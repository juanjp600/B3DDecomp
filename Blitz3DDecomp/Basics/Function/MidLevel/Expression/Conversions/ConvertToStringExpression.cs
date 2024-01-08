namespace Blitz3DDecomp.MidLevel;

sealed record ConvertToStringExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Str({OriginalExpression.StringRepresentation})";
}