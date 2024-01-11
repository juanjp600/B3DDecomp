namespace Blitz3DDecomp.HighLevel;

sealed record ConvertObjectToHandleExpression(Expression ObjectExpression) : Expression
{
    public override string StringRepresentation
        => $"(Handle {ObjectExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ConvertObjectToHandleExpression(ObjectExpression.Map(mapper)));
    }
}