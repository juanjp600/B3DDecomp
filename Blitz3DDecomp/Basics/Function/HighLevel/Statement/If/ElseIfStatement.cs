namespace Blitz3DDecomp.HighLevel;

sealed record ElseIfStatement(Expression Condition) : Statement
{
    public override string StringRepresentation
        => $"ElseIf {Condition.StringRepresentation} Then";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new IfStatement(Condition.Map(expressionMapper)));

    public override int IndentationToAdd => 1;
    public override int IndentationToSubtract => 1;
}