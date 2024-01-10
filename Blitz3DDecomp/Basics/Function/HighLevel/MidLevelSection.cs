namespace Blitz3DDecomp.HighLevel;

sealed class MidLevelSection
{
    public readonly string Name;
    public readonly List<Statement> Statements = new List<Statement>();

    public MidLevelSection(string name)
    {
        Name = name;
    }
}