namespace Blitz3DDecomp.MidLevel;

sealed record ConvertToIntExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Int({OriginalExpression.StringRepresentation})";
}