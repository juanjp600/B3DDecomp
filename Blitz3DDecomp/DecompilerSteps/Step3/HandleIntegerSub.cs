using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class HandleIntegerSub
{
    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        foreach (var instruction in section.Instructions)
        {
            if (instruction.Name is not "sub") { continue; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);

            void trySetVarToInt(Variable? variable)
            {
                if (variable?.DeclType != DeclType.Unknown) { return; }
                variable.DeclType = DeclType.Int;
                Logger.WriteLine($"{section.Owner}: {variable.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }

            if (destVar?.DeclType == DeclType.Int
                || srcVar?.DeclType == DeclType.Int)
            {
                trySetVarToInt(destVar);
                trySetVarToInt(srcVar);
            }
        }
        return somethingChanged;
    }

    public static bool Process(Function function)
    {
        bool somethingChanged = false;
        foreach (var section in function.AssemblySections)
        {
            somethingChanged |= ProcessSection(section);
        }
        return somethingChanged;
    }
}