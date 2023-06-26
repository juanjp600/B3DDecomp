namespace Blitz3DDecomp;

struct BasicDeclaration
{
    public string Name;
    public DeclType DeclType;

    public override string ToString()
        => $"{Name}{DeclType.Suffix}";
}