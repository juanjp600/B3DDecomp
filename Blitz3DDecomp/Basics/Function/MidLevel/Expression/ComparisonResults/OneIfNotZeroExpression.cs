namespace Blitz3DDecomp.MidLevel;

sealed record OneIfNotZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} <> 0)";
}