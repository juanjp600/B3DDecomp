namespace Blitz3DDecomp.HighLevel;

sealed record OneIfExpressionsEqualExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} = {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OneIfExpressionsEqualExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}