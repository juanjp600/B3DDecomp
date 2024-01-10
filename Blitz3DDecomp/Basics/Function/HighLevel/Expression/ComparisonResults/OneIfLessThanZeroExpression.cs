namespace Blitz3DDecomp.HighLevel;

sealed record OneIfLessThanZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} < 0)";
}