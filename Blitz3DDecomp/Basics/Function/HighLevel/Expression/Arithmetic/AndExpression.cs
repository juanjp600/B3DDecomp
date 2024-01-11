namespace Blitz3DDecomp.HighLevel;

sealed record AndExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} And {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new AndExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}