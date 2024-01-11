namespace Blitz3DDecomp.HighLevel;

sealed record OneIfLessThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} <= 0)";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfLessThanOrEqualToZeroExpression(OriginalExpression.Map(mapper)));
    }
}