namespace Blitz3DDecomp.LowLevel;

sealed class AssemblySection
{
    public readonly Function Owner;
    public readonly string Name;
    public readonly Instruction[] Instructions = Array.Empty<Instruction>();

    public AssemblySection(Function owner, string name, IReadOnlyList<Instruction> instructions)
    {
        Owner = owner;
        Name = name;
        Instructions = instructions.ToArray();
    }

    public IEnumerable<GlobalVariable> ReferencedGlobals
        => ReferencedVariables
            .OfType<GlobalVariable>();

    public IEnumerable<Variable> ReferencedVariables
        => Instructions
            .SelectMany(i => new[] { i.DestArg, i.SrcArg1, i.SrcArg2 })
            .Select(s => s.StripDeref())
            .Select(Owner.InstructionArgumentToVariable)
            .OfType<Variable>() // Removes null entries
            .Distinct();
}
