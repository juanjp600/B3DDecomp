namespace Blitz3DDecomp.HighLevel.Loops.DoWhile;

sealed record UntilStatement(Expression Condition) : Statement
{
    public override string StringRepresentation
        => $"Until {Condition.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };
    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new UntilStatement(Condition.Map(expressionMapper)));

    public override int IndentationToSubtract => 1;
}