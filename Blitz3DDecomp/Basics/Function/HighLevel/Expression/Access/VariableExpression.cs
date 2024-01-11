namespace Blitz3DDecomp.HighLevel;

sealed record VariableExpression(Variable Variable) : AccessExpression
{
    public override string StringRepresentation
        => Variable.Name;

    public override Expression Map(Func<Expression, Expression> mapper)
        => mapper(this);

    public override IEnumerable<Expression> InnerExpressions
        => Enumerable.Empty<Expression>();
}