namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfGreaterThanOrEqualToZeroStatement(Expression Expression, HighLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If ({Expression.StringRepresentation} >= 0) Then Goto {Section.Name}";
}