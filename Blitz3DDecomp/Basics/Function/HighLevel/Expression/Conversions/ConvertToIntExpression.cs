namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToIntExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Int {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ConvertToIntExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}