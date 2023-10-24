namespace Blitz3DDecomp;

sealed class FieldVariable : Variable
{
    public readonly Variable Owner;

    public FieldVariable(Variable owner, CustomType.Field field) : base($"{owner.Name}\\{field.Name}")
    {
        Owner = owner;
        DeclType = field.DeclType;
    }

    public override string ToInstructionArg()
        => Name;
}