namespace Blitz3DDecomp.HighLevel;

sealed record LogicalOrExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Lor {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new LogicalOrExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Lhs, Rhs };
}