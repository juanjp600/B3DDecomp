namespace Blitz3DDecomp.HighLevel;

sealed record WhileStatement(Expression Condition) : Statement
{
    public override string StringRepresentation
        => $"While {Condition.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new WhileStatement(Condition.Map(expressionMapper)));

    public override int IndentationToAdd => 1;
}