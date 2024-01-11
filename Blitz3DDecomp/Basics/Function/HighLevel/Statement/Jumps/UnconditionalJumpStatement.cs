namespace Blitz3DDecomp.HighLevel;

sealed record UnconditionalJumpStatement(HighLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"Goto {Section.Name}";
}