namespace Blitz3DDecomp;

sealed class FieldVariable : Variable
{
    public readonly Variable Owner;
    public readonly CustomType.Field TypeField;

    public FieldVariable(Variable owner, CustomType.Field field) : base($"{owner.Name}\\{field.Name}")
    {
        Owner = owner;
        TypeField = field;
    }

    public override DeclType DeclType
    {
        get => TypeField.DeclType;
        set => throw new InvalidOperationException("Cannot set field decltype!");
    }

    public override string ToInstructionArg()
        => Name;
}