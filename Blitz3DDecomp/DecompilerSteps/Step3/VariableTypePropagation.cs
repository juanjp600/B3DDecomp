using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class VariableTypePropagation
{
    private static bool ProcessSection(Function.AssemblySection section)
    {
        bool somethingChanged = false;

        void processInstruction(Instruction instruction)
        {
            if (instruction.Name is not ("mov" or "lea" or "xchg")) { return; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null || srcVar is null) { return; }

            if (destVar.DeclType == DeclType.Unknown && srcVar.DeclType != DeclType.Unknown)
            {
                somethingChanged = true;
                destVar.DeclType = srcVar.DeclType;
                Logger.WriteLine($"{section.Owner}: {destVar.Name} is {destVar.DeclType} because {srcVar}");
            }
            if (srcVar.DeclType == DeclType.Unknown && destVar.DeclType != DeclType.Unknown)
            {
                somethingChanged = true;
                srcVar.DeclType = destVar.DeclType;
                Logger.WriteLine($"{section.Owner}: {srcVar.Name} is {srcVar.DeclType} because {destVar}");
            }
        }
        
        foreach (var instruction in section.Instructions)
        {
            processInstruction(instruction);
        }
        foreach (var instruction in Enumerable.Reverse(section.Instructions))
        {
            processInstruction(instruction);
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