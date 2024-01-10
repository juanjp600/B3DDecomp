namespace Blitz3DDecomp.HighLevel;

sealed record OneIfNotZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} <> 0)";
}