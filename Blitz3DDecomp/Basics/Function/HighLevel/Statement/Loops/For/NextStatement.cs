namespace Blitz3DDecomp.HighLevel;

sealed record NextStatement : Statement
{
    public override string StringRepresentation
        => "Next";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToSubtract => 1;
}