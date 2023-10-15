namespace Blitz3DDecomp;

abstract class Variable
{
    public readonly string Name;
    public DeclType DeclType = DeclType.Unknown;

    protected Variable(string name)
    {
        Name = name;
    }

    public override string ToString()
        => $"{Name}{DeclType.Suffix}";
}
