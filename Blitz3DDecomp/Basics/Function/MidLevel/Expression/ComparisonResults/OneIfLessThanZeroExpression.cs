namespace Blitz3DDecomp.MidLevel;

sealed record OneIfLessThanZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} < 0)";
}