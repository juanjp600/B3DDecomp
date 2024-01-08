using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class BbCustomTypeFieldAccessRewrite
{
    private static bool TryRewrite(Variable? ownerVariable, Variable? outputVar, Instruction instruction, uint fieldIndex)
    {
        if (ownerVariable is null) { return false; }
        if (!ownerVariable.DeclType.IsCustomType) { return false; }
        if (ownerVariable.DeclType.IsArrayType) { return false; }
        if (outputVar is null) { return false; }

        var customType = CustomType.GetTypeMatchingDeclType(ownerVariable.DeclType);

        instruction.Name = "mov";
        instruction.DestArg = outputVar.Name;
        instruction.SrcArg1 = $"{ownerVariable.Name}\\{customType.Fields[(int)fieldIndex].Name}";
        instruction.SrcArg2 = "";

        return true;
    }

    private static bool HandleMavless(AssemblySection section, Instruction instruction)
    {
        if (instruction.Name != "call") { return false; }
        if (instruction.DestArg != "@_builtIn__bbFieldPtrAdd") { return false; }

        if (instruction.CallParameterAssignmentIndices is not { Length: 2 } assignmentIndices) { return false; }
        if (instruction.ReturnOutputVar is not { } outputVar) { return false; }

        var ownerVariable = section.Owner.InstructionArgumentToVariable(section.Instructions[assignmentIndices[0]].SrcArg1);
        uint fieldIndex = section.Instructions[assignmentIndices[1]].SrcArg1.HexToUint32() >> 2;

        return TryRewrite(ownerVariable, outputVar, instruction, fieldIndex);
    }

    private static bool HandleVanilla(AssemblySection section, Instruction instruction)
    {
        if (instruction.Name != "add") { return false; }

        var ownerVariable = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
        var outputVar = section.Owner.InstructionArgumentToVariable(instruction.DestArg);

        if (!instruction.SrcArg2.TryHexToUint32(out var fieldIndex)) { return false; }
        fieldIndex >>= 2;

        return TryRewrite(ownerVariable, outputVar, instruction, fieldIndex);
    }

    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        foreach (var instruction in section.Instructions)
        {
            somethingChanged |= HandleMavless(section, instruction);
            somethingChanged |= HandleVanilla(section, instruction);
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