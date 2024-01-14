namespace Blitz3DDecomp.HighLevel;

sealed record ExponentiationExpression(Expression Base, Expression Exponent) : Expression
{
    public override string StringRepresentation
        => $"({Base.StringRepresentation} ^ {Exponent.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ExponentiationExpression(Base.Map(mapper), Exponent.Map(mapper)));
    }

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Base, Exponent };
}