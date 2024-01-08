namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfGreaterThanZeroStatement(Expression Expression, MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If ({Expression.StringRepresentation} > 0) Then Goto {Section.Name}";
}