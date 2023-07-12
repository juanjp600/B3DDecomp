using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class MarkAsFloat
    {
        public static void Process(Function function)
        {
            foreach (var kvp in function.AssemblySections.ToList())
            {
                var section = kvp.Value;
                for (int i = 0; i < section.Length - 2; i++)
                {
                    var instruction = section[i];
                    var nextInstruction = section[i + 1];
                    var instructionAfterNext = section[i + 2];
                    if (instruction.Name == "push" && instructionAfterNext.Name == "pop"
                        && instruction.LeftArg == instructionAfterNext.LeftArg)
                    {
                        if (nextInstruction.Name is not ("fistp" or "fild" or "fstp" or "fld")) { Debugger.Break(); }

                        instruction.Name = nextInstruction.Name + "_markAsFloat";

                        section = section[..(i + 1)].Concat(section[(i + 3)..]).ToArray();
                        section[i] = instruction;
                        function.AssemblySections[kvp.Key] = section;
                    }
                }
            }
        }
    }
}