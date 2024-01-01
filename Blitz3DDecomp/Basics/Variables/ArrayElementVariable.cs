namespace Blitz3DDecomp;

sealed class ArrayElementVariable : Variable
{
    public readonly Variable Owner;

    public ArrayElementVariable(Variable owner, string index) : base($"{owner.Name}[{index}]")
    {
        Owner = owner;
    }

    public override DeclType DeclType
    {
        get => Owner.DeclType.GetElementType()
               ?? throw new Exception($"Invalid array element variable from owner {Owner}");
        set => throw new InvalidOperationException("Can't set the type of an array element variable");
    }

    public override string ToInstructionArg()
        => Name;
}