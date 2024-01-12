namespace Blitz3DDecomp.HighLevel;

sealed record DestructorStatement(Expression ObjectExpression) : Statement
{
    public override string StringRepresentation
        => $"Delete {ObjectExpression.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { ObjectExpression };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new DestructorStatement(ObjectExpression.Map(expressionMapper)));
}