namespace Blitz3DDecomp.MidLevel;

sealed record ConvertToFloatExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Float({OriginalExpression.StringRepresentation})";
}