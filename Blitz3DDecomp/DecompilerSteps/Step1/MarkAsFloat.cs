using System.Diagnostics;

namespace Blitz3DDecomp;

static class MarkAsFloat
{
    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections.ToList())
        {
            var section = kvp.Value;
            for (int i = 0; i < section.Instructions.Count - 2; i++)
            {
                var instruction = section.Instructions[i];
                var nextInstruction = section.Instructions[i + 1];
                var instructionAfterNext = section.Instructions[i + 2];
                if (instruction.Name == "push" && instructionAfterNext.Name == "pop"
                    && instruction.LeftArg == instructionAfterNext.LeftArg)
                {
                    if (nextInstruction.Name is not ("fistp" or "fild" or "fstp" or "fld")) { Debugger.Break(); }

                    instruction.Name = nextInstruction.Name + "_markAsFloat";

                    section.Instructions.RemoveAt(i + 2);
                    section.Instructions.RemoveAt(i + 1);
                    section.Instructions[i] = instruction;
                    function.AssemblySections[kvp.Key] = section;
                }
            }
        }
    }
}