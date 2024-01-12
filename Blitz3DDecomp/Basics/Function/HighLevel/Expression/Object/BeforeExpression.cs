namespace Blitz3DDecomp.HighLevel;

sealed record BeforeExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Before {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new BeforeExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}