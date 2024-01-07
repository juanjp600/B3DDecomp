using System.Diagnostics;
using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step4;

static class GuessIntFromInstructions
{
    private static void GuessForVariable(Function function, Variable? variable, string reason)
    {
        if (variable?.DeclType != DeclType.Unknown) { return; }

        variable.DeclType = DeclType.Int;
        Logger.WriteLine($"{function}: {variable.Name} is probably {variable.DeclType} because {reason}");
    }

    private static void ProcessSection(AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            switch (instruction.Name)
            {
                case "add" or "cmp" or "xor" or "and" or "or":
                    var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
                    var srcVar1 = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
                    var srcVar2 = section.Owner.InstructionArgumentToVariable(instruction.SrcArg2);

                    GuessForVariable(section.Owner, destVar, instruction.ToString());
                    GuessForVariable(section.Owner, srcVar1, instruction.ToString());
                    GuessForVariable(section.Owner, srcVar2, instruction.ToString());
                    break;
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(section);
        }
    }
}