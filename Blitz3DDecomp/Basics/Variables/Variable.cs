using System.Collections.Immutable;
using B3DDecompUtils;

namespace Blitz3DDecomp;

abstract class Variable
{
    public readonly string Name;

    public abstract DeclType DeclType { get; set; }

    public DebugTrace Trace = default;

    public ArrayElementVariable? GetArrayElement(string index)
        => DeclType.GetElementType() is not null
            ? new ArrayElementVariable(this, index)
            : null;

    protected readonly List<FieldVariable> fields = new List<FieldVariable>();
    public IReadOnlyList<FieldVariable> Fields
    {
        get
        {
            if (fields.Count == 0 && DeclType is { IsArrayType: false, IsCustomType: true })
            {
                var customType = CustomType.GetTypeMatchingDeclType(DeclType);
                fields.AddRange(customType.Fields.Select(f => new FieldVariable(this, f)));
            }
            return fields;
        }
    }

    public bool CanBeSourceOfPropagation()
    {
        if (DeclType == DeclType.Unknown) { return false; }

        if (this is Function.Parameter { Owner: var owner }
            && DeclType == DeclType.Pointer
            && owner.Name.EndsWith("__LIBS", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    protected Variable(string name)
    {
        Name = name;
    }

    public abstract string ToInstructionArg();

    public override string ToString()
        => $"{Name}{DeclType.Suffix}";

    public override bool Equals(object? obj)
    {
        return obj is Variable otherVariable && otherVariable == this;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, DeclType);
    }

    public static bool operator ==(Variable? a, Variable? b)
    {
        if (a is null) { return b is null; }
        if (b is null) { return false; }
        return a.Name == b.Name && a.DeclType == b.DeclType;
    }

    public static bool operator !=(Variable? a, Variable? b)
    {
        return !(a == b);
    }
}
