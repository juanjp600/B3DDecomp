namespace Blitz3DDecomp.MidLevel;

sealed record UnconditionalJumpStatement(MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"Goto {Section.Name}";
}