namespace Blitz3DDecomp.LowLevel;

sealed class AssemblySection
{
    public readonly Function Owner;
    public readonly string Name;

    public readonly Range InstructionRange;
    public ReadOnlySpan<Instruction> Instructions
        => Owner.Instructions.AsSpan()[InstructionRange];

    public int PreambleEndIndex = -1;

    public AssemblySection(Function owner, string name, Range instructionRange)
    {
        Owner = owner;
        Name = name;
        InstructionRange = instructionRange;
    }

    public IEnumerable<GlobalVariable> ReferencedGlobals
        => ReferencedVariables
            .OfType<GlobalVariable>();

    public IEnumerable<Variable> ReferencedVariables
        => Instructions
            .ToArray()
            .SelectMany(i => new[] { i.DestArg, i.SrcArg1, i.SrcArg2 })
            .Select(s => s.StripDeref())
            .Select(Owner.InstructionArgumentToVariable)
            .OfType<Variable>() // Removes null entries
            .Distinct();
}
