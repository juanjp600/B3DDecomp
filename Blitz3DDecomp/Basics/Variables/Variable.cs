namespace Blitz3DDecomp;

abstract class Variable
{
    public readonly string Name;

    private DeclType declType = DeclType.Unknown;

    public DeclType DeclType
    {
        get => declType;
        set
        {
            declType = value;
            Fields.Clear();

            ArrayElement = declType.GetElementType() is { } elementType
                ? new SubVariable(this, elementType, "ArrayElement")
                : null;

            if (declType is not { IsArrayType: false, IsCustomType: true }) { return; }

            var customType = CustomType.GetTypeMatchingDeclType(declType);
            if (customType != null)
            {
                Fields.AddRange(customType.Fields.Select(f => new SubVariable(this, f)));
            }
        }
    }

    public SubVariable? ArrayElement { get; private set; } = null;
    public readonly List<SubVariable> Fields = new List<SubVariable>();

    protected Variable(string name)
    {
        Name = name;
    }

    public abstract string ToInstructionArg();

    public override string ToString()
        => $"{Name}{DeclType.Suffix}";
}
