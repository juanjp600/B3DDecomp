namespace Blitz3DDecomp.HighLevel;

sealed record ConstructorExpression(CustomType Type) : Expression
{
    public override string StringRepresentation
        => $"(New {Type.Name})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(this);
    }

    public override IEnumerable<Expression> InnerExpressions
        => Enumerable.Empty<Expression>();
}