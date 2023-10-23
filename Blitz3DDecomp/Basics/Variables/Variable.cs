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
            fields.Clear();

            ArrayElement = declType.GetElementType() is { } elementType
                ? new SubVariable(this, elementType, "ArrayElement")
                : null;

        }
    }

    public SubVariable? ArrayElement { get; private set; } = null;

    private readonly List<SubVariable> fields = new List<SubVariable>();

    public IReadOnlyList<SubVariable> Fields
    {
        get
        {
            if (fields.Count == 0 && declType is { IsArrayType: false, IsCustomType: true })
            {
                var customType = CustomType.GetTypeMatchingDeclType(declType);
                if (customType != null)
                {
                    fields.AddRange(customType.Fields.Select(f => new SubVariable(this, f)));
                }
            }
            return fields;
        }
    }

    protected Variable(string name)
    {
        Name = name;
    }

    public abstract string ToInstructionArg();

    public override string ToString()
        => $"{Name}{DeclType.Suffix}";
}
