namespace Blitz3DDecomp.HighLevel;

sealed record WendStatement : Statement
{
    public override string StringRepresentation
        => "Wend";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToSubtract => 1;
}