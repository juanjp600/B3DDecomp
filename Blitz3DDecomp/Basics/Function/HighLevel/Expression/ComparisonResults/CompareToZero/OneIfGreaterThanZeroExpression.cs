using Blitz3DDecomp.HighLevel.ComparisonResults;

namespace Blitz3DDecomp.HighLevel;

sealed record OneIfGreaterThanZeroExpression(Expression OriginalExpression) : BooleanExpression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} > 0)";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfGreaterThanZeroExpression(OriginalExpression.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { OriginalExpression };

    public override BooleanExpression Negated
        => new OneIfLessThanOrEqualToZeroExpression(OriginalExpression);
}