using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step2;

static class HandleUnambiguousIntegerInstructions
{
    private static void ProcessSection(AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            if (instruction.Name is not ("shl" or "shr" or "sar")) { continue; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);

            void trySetVarToInt(Variable? variable)
            {
                if (variable?.DeclType != DeclType.Unknown) { return; }
                variable.DeclType = DeclType.Int;
                Logger.WriteLine($"{section.Owner}: {variable.Name} is {DeclType.Int} because {instruction}");
            }

            trySetVarToInt(destVar);
            trySetVarToInt(srcVar);
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