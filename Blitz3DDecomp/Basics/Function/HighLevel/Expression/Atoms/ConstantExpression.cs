namespace Blitz3DDecomp.HighLevel;

sealed record ConstantExpression(string Value) : Expression
{
    public override string StringRepresentation
        => Value;

    public override Expression Map(Func<Expression, Expression> mapper)
        => mapper(this);

    public override IEnumerable<Expression> InnerExpressions
        => Enumerable.Empty<Expression>();
}