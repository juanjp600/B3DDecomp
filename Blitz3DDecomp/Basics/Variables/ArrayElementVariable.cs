namespace Blitz3DDecomp;

sealed class ArrayElementVariable : Variable
{
    public readonly Variable Owner;

    public ArrayElementVariable(Variable owner, DeclType declType) : base("ArrayElement")
    {
        Owner = owner;
        DeclType = declType;
    }

    public override string ToInstructionArg()
        => $"{Owner.Name}[i]";
}