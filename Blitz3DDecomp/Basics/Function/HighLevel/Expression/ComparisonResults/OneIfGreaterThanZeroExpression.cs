namespace Blitz3DDecomp.HighLevel;

sealed record OneIfGreaterThanZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} > 0)";
}