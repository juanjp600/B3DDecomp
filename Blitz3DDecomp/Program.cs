using System.Diagnostics;
using System.Text;
using B3DDecompUtils;
using Blitz3DDecomp;
using Blitz3DDecomp.DecompilerSteps.Step1;
using Blitz3DDecomp.DecompilerSteps.Step2;
using Blitz3DDecomp.DecompilerSteps.Step3;
using Blitz3DDecomp.DecompilerSteps.Step4;
using Blitz3DDecomp.DecompilerSteps.Step5;
using Blitz3DDecomp.MidLevel;

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
        Step3();
        Step4();

        var referencedFunctions = new HashSet<Function>();
        var functionsToCheck = new HashSet<Function>();
        functionsToCheck.Add(Function.GetFunctionByName("EntryPoint"));
        while (functionsToCheck.Count > 0)
        {
            var functionsCurrentlyChecking = functionsToCheck.ToArray();
            referencedFunctions.UnionWith(functionsCurrentlyChecking);
            functionsToCheck.Clear();
            foreach (var function in functionsCurrentlyChecking)
            {
                foreach (var section in function.AssemblySections)
                {
                    foreach (var instruction in section.Instructions)
                    {
                        if (instruction.Name != "call") { continue; }

                        var callee = Function.GetFunctionByName(instruction.DestArg);
                        if (referencedFunctions.Contains(callee)) { continue; }

                        functionsToCheck.Add(callee);
                    }
                }
            }
        }

        WriteDebugDirLow(referencedFunctions, decompPath);

        Step5(referencedFunctions);

        WriteDebugDirMid(referencedFunctions, decompPath);

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
            LocationToVarRewrite.Process(function);
            DimArrayAccessRewrite.Process(function);
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
            HandleUnambiguousIntegerInstructions.Process(function);
            VectorTypeDeduction.Process(function);
        }
    }

    /// <summary>
    /// Multi-pass type deduction
    /// </summary>
    private static void Step3()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any()).ToArray();

        while (true)
        {
            bool somethingChanged = false;
            foreach (var function in functionsWithAssemblySections)
            {
                somethingChanged |= BbArrayAccessRewrite.Process(function);
                somethingChanged |= BbCustomTypeFieldAccessRewrite.Process(function);
                somethingChanged |= BbCustomTypePropagation.Process(function);
                somethingChanged |= VariableTypePropagation.Process(function);
                somethingChanged |= CalleeArgumentTypePropagation.Process(function);
                somethingChanged |= CalleeReturnTypePropagation.Process(function);
                somethingChanged |= SelfReturnTypePropagation.Process(function);
                somethingChanged |= HandleIntegerAddAndSub.Process(function);
            }
            if (!somethingChanged) { break; }
        }
    }

    /// <summary>
    /// Guesses for variables and functions where previous steps were unable to tease out the real types
    /// </summary>
    private static void Step4()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        
        foreach (var function in functionsWithAssemblySections)
        {
            GuessIntFromInstructions.Process(function);
        }
        foreach (var function in functionsWithAssemblySections)
        {
            GuessFloatsFromConstants.Process(function);
        }

        while (true)
        {
            bool somethingChanged = false;
            foreach (var function in functionsWithAssemblySections)
            {
                somethingChanged |= VariableTypePropagation.Process(function);
                somethingChanged |= CalleeArgumentTypePropagation.Process(function);
                somethingChanged |= CalleeReturnTypePropagation.Process(function);
                somethingChanged |= SelfReturnTypePropagation.Process(function);
                somethingChanged |= HandleIntegerAddAndSub.Process(function);
            }
            if (!somethingChanged) { break; }
        }

        GuessIntFromNothing.Execute();
    }

    private static void WriteDebugDirLow(IEnumerable<Function> referencedFunctions, string decompPath)
    {
        var debugDir = $"{decompPath}DebugDirLow/";
        if (Directory.Exists(debugDir)) { Directory.Delete(debugDir); }
        Directory.CreateDirectory(debugDir);
        
        var functionsWithAssemblySections = referencedFunctions.Where(f => f.AssemblySections.Count > 0).ToArray();
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
                    var numReferences = function.AssemblySections.Sum(
                        s => s.Instructions.Count(i =>
                            function.InstructionArgumentToVariable(i.DestArg) == variable
                            || function.InstructionArgumentToVariable(i.SrcArg1) == variable
                            || function.InstructionArgumentToVariable(i.SrcArg2) == variable));
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

            var referencedGlobals = function.AssemblySections.SelectMany(s => s.ReferencedGlobals).Distinct().ToArray();
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
            foreach (var section in function.AssemblySections)
            {
                writeLineToFile($"  {section.Name}:");
                for (var instrIndex = 0; instrIndex < section.Instructions.Length; instrIndex++)
                {
                    var instruction = section.Instructions[instrIndex];
                    var prefix = instrIndex.ToString();
                    var maxPrefix = section.Instructions.Length.ToString();
                    while (prefix.Length < maxPrefix.Length) { prefix = " " + prefix; }
                    writeLineToFile($"    {prefix}: {instruction}");
                }
                writeLineToFile("");
            }
        }
    }

    private static void Step5(IEnumerable<Function> referencedFunctions)
    {
        var functionsWithAssemblySections = referencedFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            MidLevelGen.Process(function);
            break;
        }
    }

    private static void WriteDebugDirMid(IEnumerable<Function> referencedFunctions, string decompPath)
    {
        var debugDir = $"{decompPath}DebugDirMid/";
        if (Directory.Exists(debugDir)) { Directory.Delete(debugDir); }
        Directory.CreateDirectory(debugDir);
        
        var functionsWithMidLevelSections = referencedFunctions.Where(f => f.MidLevelSections.Count > 0).ToArray();
        foreach (var function in functionsWithMidLevelSections)
        {
            using var file = File.Create($"{debugDir}{function.Name}.txt");
            
            string indentation = "";
            void writeLineToFile(string indent, string line)
            {
                file.Write(Encoding.UTF8.GetBytes($"{indent}{line}\n"));
            }

            writeLineToFile("", $"Function {function}");
            indentation = "    ";
            foreach (var section in function.MidLevelSections)
            {
                writeLineToFile("---- ", section.Name);
                foreach (var statement in section.Statements)
                {
                    if (statement is NextStatement) { indentation = indentation[..^4]; }
                    writeLineToFile(indentation, statement.StringRepresentation);
                    if (statement is ForEachStatement) { indentation = indentation + "    "; }
                }
            }
        }
    }
}
