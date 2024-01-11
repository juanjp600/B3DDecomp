namespace Blitz3DDecomp.HighLevel;

sealed record OrExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Or {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new OrExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}