namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToIntExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Int({OriginalExpression.StringRepresentation})";
}