using System.Diagnostics;
using System.Text;
using B3DDecompUtils;
using Blitz3DDecomp;

internal static class Program
{
    public static void Main(string[] args)
    {
        string disasmPath = "C:/Users/juanj/Desktop/Blitz3D/ReverseEng/game_disasm/";
        string decompPath = disasmPath.Replace("_disasm", "_decomp");
        if (Directory.Exists(decompPath)) { Directory.Delete(decompPath, true); }
        Directory.CreateDirectory(decompPath);

        Function.InitBuiltIn(
            Enum.Parse<Compiler>(
                File.ReadAllText(disasmPath + "Compiler.txt").Trim(),
                ignoreCase: true));

        StringConstantDecompiler.FromDir(disasmPath, decompPath);
        TypeDecompiler.FromDir(disasmPath, decompPath);
        LoadGlobalList.FromDir(disasmPath);
        var allGlobals = GlobalVariable.AllGlobals;
        IngestCodeFiles.FromDir(disasmPath);
        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.MarkAsFloat.Process(function);
        }
        var usedInstructions = Function.AllFunctions.SelectMany(f => f.AssemblySections.Values)
            .SelectMany(section => section.Instructions).Select(instr => instr.Name).ToHashSet();

        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.CountArguments.Process(function);
        }

        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.CountLocals.Process(function);
            VectorTypeDeduction.Process(function);
        }

        foreach (var function in Function.AllFunctions)
        {
            UnambiguousIntegerInstructions.Process(function);
        }

        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.LibCallCleanup.Process(function);
        }

        foreach (var function in Function.AllFunctions
                     // Order by total instruction count to minimize lib function signature guess errors
                     .OrderBy(f => f.TotalInstructionCount)
                     .ToList())
        {
            FunctionDecompiler.CollectCalls.Process(function);
        }

        bool shouldLoop = true;
        while (shouldLoop)
        {
            shouldLoop = false;
            foreach (var function in Function.AllFunctions)
            {
                void handleNeedForLooping(bool result, string msg)
                {
                    if (result)
                    {
                        if (!shouldLoop) { Logger.WriteLine(msg); }
                        shouldLoop = true;
                    }
                }

                handleNeedForLooping(FunctionDecompiler.BbObjTypeInference.Process(function), "BbObjTypeInference.Process returned true");
                handleNeedForLooping(FunctionDecompiler.BasicFloatPropagation.Process(function), "BasicFloatPropagation.Process returned true");
                handleNeedForLooping(InferredTypePropagation.Process(function), "InferredTypePropagation.Process returned true");
                handleNeedForLooping(BbArrayAccess.Process(function), "BbArrayAccess.Process returned true");
                handleNeedForLooping(BbObjMemberAccess.Process(function), "BbObjMemberAccess.Process returned true");
            }
        }

        var functionsWithAssemblySections = Function.AllFunctions
            .Where(f => f.AssemblySections.Any())
            .ToArray();
        var functionsWithSomeGoodBits = functionsWithAssemblySections
            .Where(f =>
                f.ReturnType != DeclType.Unknown
                || f.Parameters.Any(a => a.DeclType != DeclType.Unknown)
                || f.LocalVariables.Any(v => v.DeclType != DeclType.Unknown))
            .ToArray();
        var megaGoodFunctions = functionsWithAssemblySections
            .Where(f => f.ReturnType != DeclType.Unknown)
            .Where(f => f.Parameters.All(a => a.DeclType != DeclType.Unknown))
            .Where(f => f.LocalVariables.All(v => v.DeclType != DeclType.Unknown))
            .OrderByDescending(f => f.LocalVariables.Count + f.Parameters.Count)
            .ToArray();
        var goodFunctionsWithStringInSignature = functionsWithSomeGoodBits
            .Where(f =>
                f.ReturnType == DeclType.String
                || f.Parameters.Any(a => a.DeclType == DeclType.String)
                || f.LocalVariables.Any(v => v.DeclType == DeclType.String))
            .ToArray();

        var badFunctions = functionsWithAssemblySections
            .Where(f =>
                f.ReturnType == DeclType.Unknown
                || f.Parameters.Any(a => a.DeclType == DeclType.Unknown)
                || f.LocalVariables.Any(v => v.DeclType == DeclType.Unknown))
            .OrderByDescending(f => f.LocalVariables.Count(v => v.DeclType == DeclType.Unknown)
                                    + f.Parameters.Count(p => p.DeclType == DeclType.Unknown))
            .ToArray();

        var megaBadFunctions = functionsWithAssemblySections
            .Where(f => f.ReturnType == DeclType.Unknown)
            .Where(f => f.Parameters.All(a => a.DeclType == DeclType.Unknown))
            .Where(f => f.LocalVariables.All(v => v.DeclType == DeclType.Unknown))
            .OrderByDescending(f => f.LocalVariables.Count + f.Parameters.Count)
            .ToArray();

        var goodGlobals = GlobalVariable
            .AllGlobals
            .Where(v => v.DeclType != DeclType.Unknown)
            .OrderBy(v => v.Name)
            .ToArray();
        var badGlobals = GlobalVariable
            .AllGlobals
            .Where(v => v.DeclType == DeclType.Unknown)
            .ToArray();

        var customTypeAccessors = functionsWithAssemblySections
            .OrderByDescending(f => f.AssemblySections.Values
                .Count(s => s.Instructions
                    .Any(i =>
                        i.LeftArg.Contains('\\')
                        || i.RightArg.Contains('\\'))))
            .ToArray();

        var debugDir = $"{decompPath}DebugDir/";
        if (Directory.Exists(debugDir)) { Directory.Delete(debugDir); }
        Directory.CreateDirectory(debugDir);

        foreach (var function in functionsWithAssemblySections)
        {
            using var file = File.Create($"{debugDir}{function.Name}.txt");

            void writeLineToFile(string line)
            {
                file.Write(Encoding.UTF8.GetBytes($"{line}\n"));
            }

            writeLineToFile(function.Name + function.ReturnType.Suffix);

            if (function.Parameters.Count > 0)
            {
                writeLineToFile("  parameters:");
                foreach (var parameter in function.Parameters)
                {
                    writeLineToFile($"    {parameter} {parameter.ToInstructionArg()}");
                }
            }

            if (function.LocalVariables.Count > 0)
            {
                writeLineToFile("  locals:");
                foreach (var local in function.LocalVariables)
                {
                    writeLineToFile($"    {local} {local.ToInstructionArg()}");
                }
            }

            if (function.CompilerGeneratedTempVars.Count > 0)
            {
                writeLineToFile("  compiler-generated temps:");
                foreach (var temp in function.CompilerGeneratedTempVars)
                {
                    writeLineToFile($"    {temp.Name} {temp.ToInstructionArg()}");
                }
            }

            writeLineToFile("");
            writeLineToFile("code:");
            foreach (var section in function.AssemblySections.Values)
            {
                writeLineToFile($"  {section.Name}:");
                for (var instrIndex = 0; instrIndex < section.Instructions.Count; instrIndex++)
                {
                    var instruction = section.Instructions[instrIndex];
                    var prefix = instrIndex.ToString();
                    var maxPrefix = section.Instructions.Count.ToString();
                    while (prefix.Length < maxPrefix.Length) { prefix = " " + prefix; }
                    writeLineToFile($"    {prefix}: {instruction}");
                }
                writeLineToFile("");
            }
        }

        Logger.End();
        Debugger.Break();
    }
}
