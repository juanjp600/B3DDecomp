namespace Blitz3DDecomp;

sealed class ArrayElementVariable : Variable
{
    public readonly Variable Owner;

    public ArrayElementVariable(Variable owner, DeclType declType, string index) : base($"{owner.Name}[{index}]")
    {
        Owner = owner;
        DeclType = declType;
    }

    public override string ToInstructionArg()
        => Name;
}