namespace Blitz3DDecomp.HighLevel;

sealed record SignExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Sgn {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new SignExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}