namespace Blitz3DDecomp.HighLevel;

sealed record OneIfGreaterThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} >= 0)";
}