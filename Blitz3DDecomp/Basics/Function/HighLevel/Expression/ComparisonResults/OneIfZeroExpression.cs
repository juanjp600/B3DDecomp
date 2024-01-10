namespace Blitz3DDecomp.HighLevel;

sealed record OneIfZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} = 0)";
}