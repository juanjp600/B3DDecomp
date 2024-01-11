namespace Blitz3DDecomp.HighLevel;

sealed record OneIfZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} = 0)";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfZeroExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}