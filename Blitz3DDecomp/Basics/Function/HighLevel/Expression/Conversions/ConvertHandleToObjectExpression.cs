namespace Blitz3DDecomp.HighLevel;

sealed record ConvertHandleToObjectExpression(Expression HandleExpression, CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"(Object.{ObjectType.Name} {HandleExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ConvertHandleToObjectExpression(HandleExpression.Map(mapper), ObjectType));
    }
}