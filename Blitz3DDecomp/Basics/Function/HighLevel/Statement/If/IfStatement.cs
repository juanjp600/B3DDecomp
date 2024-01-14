namespace Blitz3DDecomp.HighLevel;

sealed record IfStatement(Expression Condition) : Statement
{
    public override string StringRepresentation
        => $"If {Condition.StringRepresentation} Then";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new IfStatement(Condition.Map(expressionMapper)));

    public override int IndentationToAdd => 1;
}