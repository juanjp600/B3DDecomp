using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class LibCallCleanup
    {
        private static void CrawlUp(Function.Instruction[] instructions, int startIndex)
        {
            var startInstruction = instructions[startIndex];

            var register = startInstruction.LeftArg;
            for (int i = startIndex - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
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
                        instructions[startIndex] = startInstruction;
                        break;
                    }
                }
            }
        }
        
        public static void Process(Function function)
        {
            foreach (var kvp in function.AssemblySections)
            {
                var instructions = kvp.Value;
                for (int i = 0; i < instructions.Length; i++)
                {
                    var instruction = instructions[i];
                    if (instruction.Name != "call") { continue; }
                    if (!instruction.LeftArg.ContainsRegister()) { continue; }
                    CrawlUp(instructions, i);
                }
            }
        }
    }
}