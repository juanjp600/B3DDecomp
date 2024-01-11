namespace Blitz3DDecomp.HighLevel;

sealed class HighLevelSection
{
    public readonly string Name;
    public readonly List<Statement> Statements = new List<Statement>();

    public HighLevelSection(string name)
    {
        Name = name;
    }
}