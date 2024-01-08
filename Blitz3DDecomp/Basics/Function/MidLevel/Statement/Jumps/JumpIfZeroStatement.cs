namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfZeroStatement(Expression Expression, MidLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If ({Expression.StringRepresentation} = 0) Then Goto {Section.Name}";
}