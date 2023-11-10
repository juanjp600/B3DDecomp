namespace Blitz3DDecomp;

sealed class CustomType
{
    public sealed record Field(
        CustomType Owner,
        string Name,
        DeclType DeclType)
    {
        public override string ToString()
            => $"{Name}{DeclType.Suffix}";
    }

    public static readonly List<CustomType> AllTypes = new List<CustomType>();

    public static CustomType GetTypeWithName(string name)
    {
        if (name[0] =='.') { name = name[1..]; }
        return AllTypes.FirstOrDefault(t => t.Name == name)
            ?? throw new Exception($"Custom type of name {name} was not loaded from symbols");
    }

    public static CustomType GetTypeMatchingDeclType(DeclType declType)
        => GetTypeWithName(declType.Suffix);

    public readonly string Name;
    public readonly List<Field> Fields = new List<Field>();

    public CustomType(string name)
    {
        Name = name;
        AllTypes.Add(this);
    }
}
