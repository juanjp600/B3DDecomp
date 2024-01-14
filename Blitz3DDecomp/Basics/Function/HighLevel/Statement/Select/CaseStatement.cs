namespace Blitz3DDecomp.HighLevel.Select;

sealed record CaseStatement(Expression[] Expressions) : Statement
{
    public override string StringRepresentation
        => $"Case {string.Join(",",Expressions.Select(expr => expr.StringRepresentation))}";

    public override IEnumerable<Expression> InnerExpressions => Expressions;

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new CaseStatement(Expressions.Select(expr => expr.Map(expressionMapper)).ToArray()));

    public override int IndentationToAdd => 1;
    public override int IndentationToSubtract => 1;
}