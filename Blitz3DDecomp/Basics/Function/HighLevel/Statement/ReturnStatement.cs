namespace Blitz3DDecomp.HighLevel;

sealed record ReturnStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => $"Return {Expression.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Expression };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new ReturnStatement(Expression.Map(expressionMapper)));
}