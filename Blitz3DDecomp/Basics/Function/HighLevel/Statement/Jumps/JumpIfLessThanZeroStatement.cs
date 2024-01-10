namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfLessThanZeroStatement(Expression Expression, MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If ({Expression.StringRepresentation} < 0) Then Goto {Section.Name}";
}