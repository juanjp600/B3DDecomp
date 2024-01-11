namespace Blitz3DDecomp.HighLevel;

sealed record OneIfGreaterThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} >= 0)";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfGreaterThanOrEqualToZeroExpression(OriginalExpression.Map(mapper)));
    }
}