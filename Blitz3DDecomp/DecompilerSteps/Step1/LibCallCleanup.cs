using System.Diagnostics;

namespace Blitz3DDecomp;

static class LibCallCleanup
{
    private static void CrawlUp(Function.AssemblySection section, int startIndex)
    {
        var startInstruction = section.Instructions[startIndex];

        var register = startInstruction.LeftArg;
        for (int i = startIndex - 1; i >= 0; i--)
        {
            var instruction = section.Instructions[i];
            if ((instruction.Name == "mov" && instruction.LeftArg == register)
                || (instruction.Name == "xchg" && (instruction.LeftArg == register || instruction.RightArg == register)))
            {
                var source = instruction.RightArg;
                if (source.ContainsRegister())
                {
                    register = source;
                }
                else
                {
                    source = source[1..^1];
                    startInstruction.LeftArg = source;
                    section.Instructions[startIndex] = startInstruction;
                    break;
                }
            }
        }
    }
    
    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections)
        {
            var section = kvp.Value;
            for (int i = 0; i < section.Instructions.Count; i++)
            {
                var instruction = section.Instructions[i];
                if (instruction.Name != "call") { continue; }
                if (!instruction.LeftArg.ContainsRegister()) { continue; }
                CrawlUp(section, i);
            }
        }
    }
}