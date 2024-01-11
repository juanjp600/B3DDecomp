namespace Blitz3DDecomp.HighLevel;

sealed record AfterExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(After {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(this);
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}