namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToStringExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Str {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ConvertToStringExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}