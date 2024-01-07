using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class HandleIntegerAddAndSub
{
    private static bool ProcessSection(Function.AssemblySection section)
    {
        bool somethingChanged = false;
        foreach (var instruction in section.Instructions)
        {
            if (instruction.Name is not ("add" or "sub")) { continue; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var src1Var = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
            var src2Var = section.Owner.InstructionArgumentToVariable(instruction.SrcArg2);

            void trySetVarToInt(Variable? variable)
            {
                if (variable?.DeclType != DeclType.Unknown) { return; }
                variable.DeclType = DeclType.Int;
                Logger.WriteLine($"{section.Owner}: {variable.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }

            if (destVar?.DeclType == DeclType.Int || src1Var?.DeclType == DeclType.Int)
            {
                trySetVarToInt(destVar);
                trySetVarToInt(src1Var);
                trySetVarToInt(src2Var);
            }
        }
        return somethingChanged;
    }

    public static bool Process(Function function)
    {
        bool somethingChanged = false;
        foreach (var section in function.AssemblySections.Values)
        {
            somethingChanged |= ProcessSection(section);
        }
        return somethingChanged;
    }
}