namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToStringExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Str({OriginalExpression.StringRepresentation})";
}