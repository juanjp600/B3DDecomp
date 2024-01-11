using System.Diagnostics;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp;

static class LibCallCleanup
{
    private static void CrawlUp(Function function, int startIndex)
    {
        var startInstruction = function.Instructions[startIndex];

        var register = startInstruction.DestArg;
        for (int i = startIndex - 1; i >= 0; i--)
        {
            var instruction = function.Instructions[i];
            if ((instruction.Name == "mov" && instruction.DestArg == register)
                || (instruction.Name == "xchg" && (instruction.DestArg == register || instruction.SrcArg1 == register)))
            {
                var source = instruction.SrcArg1;
                if (source.ContainsRegister())
                {
                    register = source;
                }
                else
                {
                    source = source[1..^1];
                    startInstruction.DestArg = source;
                    break;
                }
            }
        }
    }
    
    public static void Process(Function function)
    {
        for (int i = 0; i < function.Instructions.Length; i++)
        {
            var instruction = function.Instructions[i];
            if (instruction.Name != "call") { continue; }
            if (!instruction.DestArg.ContainsRegister()) { continue; }
            CrawlUp(function, i);
        }
    }
}