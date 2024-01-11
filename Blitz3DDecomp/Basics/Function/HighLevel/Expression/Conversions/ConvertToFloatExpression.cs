namespace Blitz3DDecomp.HighLevel;

sealed record ConvertToFloatExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(Float {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ConvertToFloatExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };
}