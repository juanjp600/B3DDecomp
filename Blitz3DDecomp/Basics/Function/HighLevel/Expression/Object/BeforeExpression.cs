namespace Blitz3DDecomp.HighLevel;

sealed record BeforeExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Before {OriginalExpression.StringRepresentation}";
}