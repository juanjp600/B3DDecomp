namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfLessThanOrEqualToZeroStatement(Expression Expression, MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If ({Expression.StringRepresentation} <= 0) Then Goto {Section.Name}";
}