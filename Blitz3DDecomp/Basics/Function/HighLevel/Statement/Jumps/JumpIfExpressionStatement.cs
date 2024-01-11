namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfExpressionStatement(Expression Expression, HighLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If {Expression.StringRepresentation} Then Goto {Section.Name}";
}