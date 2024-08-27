using System.Diagnostics;
using System.Text;
using B3DDecompUtils;
using Blitz3DDecomp;
using Blitz3DDecomp.DecompilerSteps.Step1;
using Blitz3DDecomp.DecompilerSteps.Step2;
using Blitz3DDecomp.DecompilerSteps.Step3;
using Blitz3DDecomp.DecompilerSteps.Step4;
using Blitz3DDecomp.DecompilerSteps.Step5;
using Blitz3DDecomp.HighLevel;

internal static class Program
{
    const string DisasmExecutableName = "Blitz3DDisasm";
    const string DecompExecutableName = "Blitz3DDecomp";

    public static void Main(string[] args)
    {
        args = args.Select(arg => arg.CleanupPath()).ToArray();
        Console.WriteLine($"args: {string.Join(" ", args)}");

        while (args.Any(arg => !arg.IsDisasmPath() && File.GetAttributes(arg).HasFlag(FileAttributes.Directory)))
        {
            args = args.SelectMany(
                arg =>
                    !arg.IsDisasmPath() && File.GetAttributes(arg).HasFlag(FileAttributes.Directory)
                        ? Directory.GetFileSystemEntries(arg)
                        : [arg.CleanupPath()])
                .ToArray();
        }

        string[] declsPaths = args.Where(arg => arg.EndsWith(".decls", StringComparison.OrdinalIgnoreCase)).ToArray();
        string[] exePaths = args.Where(arg => arg.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).ToArray();
        string[] disasmDirPaths = args.Where(arg => arg.EndsWith("_disasm/", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (exePaths.Any())
        {
            foreach (var exePath in exePaths)
            {
                var disasmProcess = Process.Start(DisasmExecutableName, [exePath]);
                disasmProcess.WaitForExit();
                var newDisasmPath = Path.GetDirectoryName(exePath)+"/"+Path.GetFileNameWithoutExtension(exePath)+"_disasm/";

                var newDecompProcess = Process.Start(DecompExecutableName, [newDisasmPath, ..declsPaths]);
                newDecompProcess.WaitForExit();
            }
            return;
        }

        string disasmPath;
        switch (disasmDirPaths.Length)
        {
            case 0:
                Console.WriteLine("No input given, closing");
                return;
            case 1:
                disasmPath = disasmDirPaths[0].CleanupPath();
                break;
            default:
                foreach (var disasmDirPath in disasmDirPaths)
                {
                    var newDecompProcess = Process.Start(DecompExecutableName, [disasmDirPath, ..declsPaths]);
                    newDecompProcess.WaitForExit();
                }
                return;
        }

        string decompPath = disasmPath.Replace("_disasm", "_decomp");
        DirectoryUtils.RecreateDirectory(decompPath);

        CurrentCompiler.Value = Enum.Parse<Compiler>(
            File.ReadAllText(disasmPath.AppendToPath("Compiler.txt")).Trim(),
            ignoreCase: true);

        Step0(disasmPath, decompPath, declsPaths);
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

        GenerateLibDecls.ToDir(decompPath);

        //WriteDebugDirLow(referencedFunctions, decompPath);

        Step5(decompPath, referencedFunctions);

        var restoreStatements = new HashSet<RestoreStatement>();
        WriteFunctions(referencedFunctions, decompPath, restoreStatements);
        File.WriteAllLines(decompPath.AppendToPath("Globals.bb"), GlobalVariable.AllGlobals.OrderBy(g => g.Name).Select(g => $"Global {g.Name}{g.DeclType.Suffix}"));
        DecompileData.Process(disasmPath, decompPath, restoreStatements);

        var mainFileLines = new List<string>();
        mainFileLines.Add($"; {disasmPath.CleanupPath().Split('/').Last(s => !string.IsNullOrWhiteSpace(s))}");
        mainFileLines.Add($"; Decompiled on {DateTime.UtcNow}");
        mainFileLines.Add("");
        mainFileLines.Add("Include \"Globals.bb\"");
        if (File.Exists(decompPath.AppendToPath("Data.bb")))
        {
            mainFileLines.Add("Include \"Data.bb\"");
        }
        mainFileLines.Add("");
        foreach (var function in referencedFunctions)
        {
            if (!function.HighLevelStatements.Any()) { continue; }
            mainFileLines.Add($"Include \"Functions/{function.Name}.bb\"");
        }
        mainFileLines.Add("");
        foreach (var customType in CustomType.AllTypes)
        {
            mainFileLines.Add($"Include \"Types/{customType.Name}.bb\"");
        }
        mainFileLines.Add("");
        foreach (var dimArray in DimArray.AllDimArrays)
        {
            mainFileLines.Add($"Dim {dimArray.Name}{dimArray.ElementDeclType.Suffix}({string.Join(", ", Enumerable.Repeat("0", dimArray.NumDimensions))})");
        }
        mainFileLines.Add("");
        mainFileLines.Add("Const INFINITY# = (999.0) ^ (99999.0)");
        mainFileLines.Add("Const NAN# = (-1.0) ^ (0.5)");
        mainFileLines.Add("");
        mainFileLines.Add("EntryPoint()");
        File.WriteAllLines(decompPath.AppendToPath("Main.bb"), mainFileLines);

        Logger.End();
    }

    /// <summary>
    /// Disassembly ingest, trivial analysis and codegen
    /// </summary>
    private static void Step0(string disasmPath, string decompPath, string[] declsFiles)
    {
        Function.InitBuiltIn(disasmPath, CurrentCompiler.Value);

        LoadGlobalList.FromDir(disasmPath);
        LoadDimArrays.FromDir(disasmPath);
        IngestCodeFiles.FromDir(disasmPath);
        IngestLibInfo.FromDir(disasmPath);
        IngestDecls.FromFiles(declsFiles);

        IngestStringConstants.FromDir(disasmPath);
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
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            LibCallCleanup.Process(function);
        }
        DetermineLibParameterCount.Execute();
        foreach (var function in functionsWithAssemblySections)
        {
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
                somethingChanged |= HandleIntegerSub.Process(function);
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
            GuessFloatsFromStoreInstructions.Process(function);
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
                somethingChanged |= HandleIntegerSub.Process(function);
            }
            if (!somethingChanged) { break; }
        }

        GuessIntFromNothing.Execute();
    }

    private static void WriteDebugDirLow(IEnumerable<Function> referencedFunctions, string decompPath)
    {
        var debugDir = $"{decompPath}DebugDirLow/";
        DirectoryUtils.RecreateDirectory(debugDir);
        
        var functionsWithAssemblySections = referencedFunctions.Where(f => f.AssemblySections.Length > 0).ToArray();
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
                    var numReferences = function.Instructions.Count(i =>
                        function.InstructionArgumentToVariable(i.DestArg) == variable
                        || function.InstructionArgumentToVariable(i.SrcArg1) == variable
                        || function.InstructionArgumentToVariable(i.SrcArg2) == variable);
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

    private static void Step5(string decompPath, IEnumerable<Function> referencedFunctions)
    {
        var functionsWithAssemblySections = referencedFunctions.Where(f => f.AssemblySections.Any()).ToArray();
        foreach (var function in functionsWithAssemblySections)
        {
            BasicLowToHighLevelConversion.Process(function);
            RemoveTrivialNoops.Process(function);
            RemoveSingleUseTemps.Process(function);
            FixDimIndexer.Process(function);
            ConvertFunctionCallsToFinalRepresentation.Process(function);
            CleanupBooleanExpressions.Process(function);
            ConvertConstantsToFinalRepresentation.Process(function);
            CleanupSelect.Process(function);
            CleanupUselessGoto.Process(function);
            CleanupWhile.Process(function);
            CleanupForOnInt.Process(function);
            CleanupRepeat.Process(function);
            CleanupIfs.Process(function);
            CleanupElse.Process(function);
            CleanupExit.Process(function);
            CleanupUselessGoto.Process(function);
            CleanupElseIf.Process(function);
        }
    }

    private static void WriteFunctions(IEnumerable<Function> referencedFunctions, string decompPath, HashSet<RestoreStatement> restoreStatements)
    {
        var outputDir = decompPath.AppendToPath("Functions");
        DirectoryUtils.RecreateDirectory(outputDir);

        var functionsWithHighLevelSections = referencedFunctions.Where(f => f.HighLevelSections.Count > 0).ToArray();
        foreach (var function in functionsWithHighLevelSections)
        {
            var referencedSections = function
                .HighLevelStatements
                .SelectMany(stmt =>
                    stmt switch
                    {
                        UnconditionalJumpStatement unconditionalJumpStatement => new[] { unconditionalJumpStatement.SectionName },
                        JumpIfExpressionStatement jumpIfExpressionStatement => new[] { jumpIfExpressionStatement.SectionName },
                        _ => Array.Empty<string>()
                    })
                .ToHashSet();

            using var file = File.Create(outputDir.AppendToPath($"{function.Name}.bb"));

            writeLineToFile("", $"Function {function}");
            var indentation = "    ";
            void writeLineToFile(string indent, string line)
            {
                file.Write(Encoding.UTF8.GetBytes($"{indent}{line}\n"));
            }
            foreach (var local in function.LocalVariables)
            {
                writeLineToFile(indentation, $"Local {local.Name}{local.DeclType.Suffix}");
            }
            foreach (var section in function.HighLevelSections)
            {
                if (referencedSections.Contains(section.Name))
                {
                    writeLineToFile("", "." + HighLevelSection.CleanupSectionName(section.Name, function));
                }
                foreach (var statement in section.Statements)
                {
                    for (int i=0;i<statement.IndentationToSubtract;i++) { indentation = indentation[..^4]; }
                    writeLineToFile(indentation, statement.StringRepresentation);
                    if (statement is RestoreStatement restoreStatement) { restoreStatements.Add(restoreStatement); }
                    for (int i=0;i<statement.IndentationToAdd;i++) { indentation = indentation + "    "; }
                }
            }
            writeLineToFile("", "End Function");
        }
    }
}
