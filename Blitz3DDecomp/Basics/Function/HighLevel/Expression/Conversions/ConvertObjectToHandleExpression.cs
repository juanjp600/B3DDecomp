namespace Blitz3DDecomp.HighLevel;

sealed record ConvertObjectToHandleExpression(Expression ObjectExpression) : Expression
{
    public override string StringRepresentation
        => $"Handle {ObjectExpression.StringRepresentation}";
}