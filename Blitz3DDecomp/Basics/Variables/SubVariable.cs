namespace Blitz3DDecomp;

sealed class SubVariable : Variable
{
    public readonly Variable Owner;

    public SubVariable(Variable owner, CustomType.Field field) : this(owner, field.DeclType, field.Name) { }

    public SubVariable(Variable owner, DeclType declType, string name) : base(name)
    {
        Owner = owner;
        DeclType = declType;
    }

    public override string ToInstructionArg()
        => $"{Owner.Name}\\{Name}";
}