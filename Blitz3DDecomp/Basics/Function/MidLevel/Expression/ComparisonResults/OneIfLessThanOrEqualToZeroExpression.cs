namespace Blitz3DDecomp.MidLevel;

sealed record OneIfLessThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} <= 0)";
}