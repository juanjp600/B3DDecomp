using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step4;

static class GuessFloatsFromStoreInstructions
{
    private static void ProcessSection(AssemblySection section)
    {
        for (var i = 0; i < section.Instructions.Length - 1; i++)
        {
            var instruction = section.Instructions[i];
            var nextInstruction = section.Instructions[i + 1];
            if (instruction.Name == "push" && nextInstruction.Name is "fstp" or "fistp")
            {
                var pushVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
                if (pushVar?.DeclType == DeclType.Unknown)
                {
                    pushVar.DeclType = DeclType.Float;
                    Logger.WriteLine($"{section.Owner}: {pushVar.Name} is probably {DeclType.Float} because {instruction}");
                }
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections)
        {
            ProcessSection(section);
        }
    }
}