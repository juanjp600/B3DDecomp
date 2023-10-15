﻿using System.Diagnostics;
using Blitz3DDecomp;

internal static class Program
{
    public static void Main(string[] args)
    {
        string disasmPath = "C:/Users/juanj/Desktop/Blitz3D/ReverseEng/game_disasm/";
        disasmPath = "C:/Users/juanj/Repos/Blitz3DDecomp/SamplePrograms/Sample1/Sample1_disasm/";
        disasmPath = "C:/Users/juanj/Desktop/Blitz3D/ReverseEng/game_disasm/";
        string decompPath = disasmPath.Replace("_disasm", "_decomp");
        if (Directory.Exists(decompPath)) { Directory.Delete(decompPath, true); }
        Directory.CreateDirectory(decompPath);

        StringConstantDecompiler.FromDir(disasmPath, decompPath);
        TypeDecompiler.FromDir(disasmPath, decompPath);
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
            FunctionDecompiler.LibCallCleanup.Process(function);
        }
        foreach (var function in Function.AllFunctions
                     // Order by total instruction count to minimize lib function signature guess errors
                     .OrderBy(f => f.TotalInstructionCount)
                     .ToList())
        {
            FunctionDecompiler.CollectCalls.Process(function);
        }

        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.CountLocals.Process(function);
            UnambiguousIntegerInstructions.Process(function);
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
                        if (!shouldLoop) { Console.WriteLine(msg); }
                        shouldLoop = true;
                    }
                }

                handleNeedForLooping(FunctionDecompiler.BbObjTypeInference.Process(function), "BbObjTypeInference.Process returned true");
                handleNeedForLooping(FunctionDecompiler.BasicFloatPropagation.Process(function), "BasicFloatPropagation.Process returned true");
                handleNeedForLooping(InferredTypePropagation.Process(function), "InferredTypePropagation.Process returned true");
            }
        }

        var sjdfsd = Function.AllFunctions
            .Where(f => f.AssemblySections.Any())
            .ToArray();
        var dingus = sjdfsd
            .Where(f =>
                f.ReturnType != DeclType.Unknown
                || f.Parameters.Any(a => a.DeclType != DeclType.Unknown)
                || f.LocalVariables.Any(v => v.DeclType != DeclType.Unknown))
            .ToArray();
        var dingus2 = dingus
            .Where(f =>
                f.ReturnType == DeclType.String
                || f.Parameters.Any(a => a.DeclType == DeclType.String)
                || f.LocalVariables.Any(v => v.DeclType == DeclType.String))
            .ToArray();

        var djdjdjd = sjdfsd
            .Where(f => f.ReturnType == DeclType.Unknown)
            .Where(f => f.Parameters.All(a => a.DeclType == DeclType.Unknown))
            .Where(f => f.LocalVariables.All(v => v.DeclType == DeclType.Unknown))
            .OrderByDescending(f => f.LocalVariables.Count + f.Parameters.Count)
            .ToArray();

        Debugger.Break();
    }
}
