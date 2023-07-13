using System.Diagnostics;
using Blitz3DDecomp;

internal static class Program
{
    public static void Main(string[] args)
    {
        string disasmPath = "C:/Users/juanj/Desktop/Blitz3D/ReverseEng/game_disasm/";
        disasmPath = "/Users/juanjp/Desktop/Repos/BlitzBasicGames/game_disasm/";
        string decompPath = disasmPath.Replace("_disasm", "_decomp");
        if (Directory.Exists(decompPath)) { Directory.Delete(decompPath, true); }
        Directory.CreateDirectory(decompPath);

        StringConstantDecompiler.FromDir(disasmPath, decompPath);
        TypeDecompiler.FromDir(disasmPath, decompPath);
        FunctionDecompiler.IngestFiles.FromDir(disasmPath);
        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.MarkAsFloat.Process(function);
        }
        var usedInstructions = Function.AllFunctions.SelectMany(f => f.AssemblySections.Values)
            .SelectMany(instrs => instrs).Select(instr => instr.Name).ToHashSet();
        
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

        /*foreach (var instruction in Function.AllFunctions.SelectMany(f => f.AssemblySections.Values).SelectMany(s => s))
        {
            if (instruction.Name != "call") { continue; }
            Console.WriteLine(instruction.ToString());
        }*/

        foreach (var function in Function.AllFunctions)
        {
            FunctionDecompiler.CountLocals.Process(function);
        }

        bool shouldLoop = true;
        while (shouldLoop)
        {
            shouldLoop = false;
            foreach (var function in Function.AllFunctions)
            {
                shouldLoop |= FunctionDecompiler.BbObjTypeInference.Process(function);
                shouldLoop |= FunctionDecompiler.BasicFloatPropagation.Process(function);
                shouldLoop |= ReturnTypeInference.Process(function);
            }
        }

        var sjdfsd = Function.AllFunctions
            .Where(f => f.AssemblySections.Any())
            .ToArray();
        var dingus = sjdfsd
            .Where(f =>
                f.ReturnType != DeclType.Unknown
                || f.Arguments.Any(a => a.DeclType != DeclType.Unknown)
                || f.LocalVariables.Any(v => v.DeclType != DeclType.Unknown))
            .ToArray();

        Debugger.Break();
    }
}
