namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToFloatExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"Float({OriginalExpression.StringRepresentation})";
}