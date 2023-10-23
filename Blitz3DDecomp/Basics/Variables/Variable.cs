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
                ? new ArrayElementVariable(this, elementType)
                : null;

        }
    }

    public ArrayElementVariable? ArrayElement { get; private set; } = null;

    private readonly List<FieldVariable> fields = new List<FieldVariable>();
    public IReadOnlyList<FieldVariable> Fields
    {
        get
        {
            if (fields.Count == 0 && declType is { IsArrayType: false, IsCustomType: true })
            {
                var customType = CustomType.GetTypeMatchingDeclType(declType);
                if (customType != null)
                {
                    fields.AddRange(customType.Fields.Select(f => new FieldVariable(this, f)));
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
