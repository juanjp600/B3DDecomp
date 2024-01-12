namespace Blitz3DDecomp.HighLevel;

sealed record FreeStandingExpressionStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => Expression.StringRepresentation;

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Expression };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new FreeStandingExpressionStatement(Expression.Map(expressionMapper)));
}