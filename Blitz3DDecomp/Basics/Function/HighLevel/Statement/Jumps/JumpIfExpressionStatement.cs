namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfExpressionStatement(Expression Expression, HighLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"If {Expression.StringRepresentation} Then Goto {Section.Name}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Expression };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new JumpIfExpressionStatement(Expression.Map(expressionMapper), Section));
}