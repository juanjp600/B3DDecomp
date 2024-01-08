using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class VariableTypePropagation
{
    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;

        void exchangeTypes(Variable destVar, Variable srcVar)
        {
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

        void handleMovLeaXchg(Instruction instruction)
        {
            if (instruction.Name is not ("mov" or "lea" or "xchg")) { return; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null || srcVar is null) { return; }

            exchangeTypes(destVar, srcVar);
        }

        void handleCmp(Instruction instruction)
        {
            if (instruction.Name is not "cmp") { return; }

            var destVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null && srcVar is null) { return; }

            uint parsedUint;
            if (destVar != null && srcVar != null)
            {
                exchangeTypes(destVar, srcVar);
            }
            else if (instruction.DestArg.TryHexToUint32(out parsedUint) && parsedUint != 0 && srcVar?.DeclType == DeclType.Unknown)
            {
                srcVar.DeclType = DeclType.Int;
                Logger.WriteLine($"{section.Owner}: {srcVar.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }
            else if (instruction.SrcArg1.TryHexToUint32(out parsedUint) && parsedUint != 0 && destVar?.DeclType == DeclType.Unknown)
            {
                destVar.DeclType = DeclType.Int;
                Logger.WriteLine($"{section.Owner}: {destVar.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }
        }
        
        foreach (var instruction in section.Instructions)
        {
            handleMovLeaXchg(instruction);
            handleCmp(instruction);
        }
        foreach (var instruction in Enumerable.Reverse(section.Instructions))
        {
            handleMovLeaXchg(instruction);
            handleCmp(instruction);
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