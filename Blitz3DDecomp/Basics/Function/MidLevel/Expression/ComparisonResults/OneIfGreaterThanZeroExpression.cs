namespace Blitz3DDecomp.MidLevel;

sealed record OneIfGreaterThanZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} > 0)";
}