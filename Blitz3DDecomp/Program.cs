using System.Diagnostics;
using System.Text;
using B3DDecompUtils;
using Blitz3DDecomp;
using Blitz3DDecomp.DecompilerSteps.Step1;
using Blitz3DDecomp.DecompilerSteps.Step2;
using Blitz3DDecomp.DecompilerSteps.Step3_Obsolete;

internal static class Program
{
    public static void Main(string[] args)
    {
        string disasmPath = "C:/Users/juanj/Desktop/Blitz3d/ReverseEng/game_disasm/";
        string decompPath = disasmPath.Replace("_disasm", "_decomp");
        if (Directory.Exists(decompPath)) { Directory.Delete(decompPath, true); }
        Directory.CreateDirectory(decompPath);
        
        Step0(disasmPath, decompPath);
        Step1();
        Step2();

        WriteDebugDir(decompPath);

        Logger.End();
    }

    /// <summary>
    /// Disassembly ingest, trivial analysis and codegen
    /// </summary>
    private static void Step0(string disasmPath, string decompPath)
    {
        Function.InitBuiltIn(
            Enum.Parse<Compiler>(
                File.ReadAllText(disasmPath + "Compiler.txt").Trim(),
                ignoreCase: true));

        LoadGlobalList.FromDir(disasmPath);
        LoadDimArrays.FromDir(disasmPath);
        IngestCodeFiles.FromDir(disasmPath);

        StringConstantDecompiler.FromDir(disasmPath, decompPath);
        TypeDecompiler.FromDir(disasmPath, decompPath);

        foreach (var function in Function.AllFunctions.Where(f => f.AssemblySections.Any()))
        {
            CountArguments.Process(function);
            CountLocals.Process(function);
        }
    }

    /// <summary>
    /// Basic rewrites to facilitate type deduction
    /// </summary>
    private static void Step1()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any())
            // Order by total instruction count to minimize lib function signature guess errors
            .OrderBy(f => f.TotalInstructionCount)
            .ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            LibCallCleanup.Process(function);
            CollectCalls.Process(function);
            DimArrayAccessRewrite.Process(function);
            LocationToVarRewrite.Process(function);
        }
    }

    /// <summary>
    /// Basic type deduction
    /// </summary>
    private static void Step2()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            HandleFloatInstructions.Process(function);
            VectorTypeDeduction.Process(function);
        }
    }

    private static void WriteDebugDir(string decompPath)
    {
        var debugDir = $"{decompPath}DebugDir/";
        if (Directory.Exists(debugDir)) { Directory.Delete(debugDir); }
        Directory.CreateDirectory(debugDir);
        
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            using var file = File.Create($"{debugDir}{function.Name}.txt");

            void writeLineToFile(string line)
            {
                file.Write(Encoding.UTF8.GetBytes($"{line}\n"));
            }

            writeLineToFile(function.Name + function.ReturnType.Suffix);

            string varToStr(Variable variable)
            {
                var instructionArg = variable.ToInstructionArg();
                var retVal = $"    {variable} {instructionArg}";
                if (variable.DeclType == DeclType.Unknown)
                {
                    var numReferences = function.AssemblySections.Values.Sum(
                        s => s.Instructions.Count(i =>
                            i.DestArg.Contains(instructionArg)
                            || i.SrcArg1.Contains(instructionArg)
                            || i.SrcArg2.Contains(instructionArg)));
                    retVal += $" ({numReferences} references)";
                }
                return retVal;
            }
            if (function.Parameters.Count > 0)
            {
                writeLineToFile($"  {function.Parameters.Count} parameters:");
                foreach (var parameter in function.Parameters)
                {
                    writeLineToFile(varToStr(parameter));
                }
            }

            if (function.LocalVariables.Count > 0)
            {
                writeLineToFile($"  {function.LocalVariables.Count} locals:");
                foreach (var local in function.LocalVariables)
                {
                    writeLineToFile(varToStr(local));
                }
            }

            if (function.CompilerGeneratedTempVars.Count > 0)
            {
                writeLineToFile($"  {function.CompilerGeneratedTempVars.Count} compiler-generated temps:");
                foreach (var temp in function.CompilerGeneratedTempVars)
                {
                    writeLineToFile($"    {temp.Name} {temp.ToInstructionArg()}");
                }
            }

            var referencedGlobals = function.AssemblySections.Values.SelectMany(s => s.ReferencedGlobals).Distinct().ToArray();
            if (referencedGlobals.Length > 0)
            {
                writeLineToFile($"  {referencedGlobals.Length} referenced globals:");
                foreach (var global in referencedGlobals)
                {
                    writeLineToFile(varToStr(global));
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
    }
}
