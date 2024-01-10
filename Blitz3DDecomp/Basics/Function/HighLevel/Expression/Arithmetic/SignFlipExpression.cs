namespace Blitz3DDecomp.HighLevel;

sealed record SignFlipExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(- {OriginalExpression.StringRepresentation})";
}