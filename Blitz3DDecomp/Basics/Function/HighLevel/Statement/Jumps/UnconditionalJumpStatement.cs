namespace Blitz3DDecomp.HighLevel;

sealed record UnconditionalJumpStatement(MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"Goto {Section.Name}";
}