using Blitz3DDecomp.HighLevel.ComparisonResults;

namespace Blitz3DDecomp.HighLevel;

sealed record OneIfExpressionsNotEqualExpression(Expression Lhs, Expression Rhs) : BooleanExpression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} <> {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfExpressionsNotEqualExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Lhs, Rhs };

    public override BooleanExpression Negated
        => new OneIfExpressionsEqualExpression(Lhs, Rhs);
}