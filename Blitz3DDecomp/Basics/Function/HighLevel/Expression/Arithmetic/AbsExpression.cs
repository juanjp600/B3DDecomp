namespace Blitz3DDecomp.HighLevel;

sealed record AbsExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Abs {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new AbsExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}