namespace Blitz3DDecomp.MidLevel;

sealed record OneIfGreaterThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} >= 0)";
}