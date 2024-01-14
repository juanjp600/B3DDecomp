namespace Blitz3DDecomp.HighLevel;

sealed record ElseStatement : Statement
{
    public override string StringRepresentation
        => "Else";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToAdd => 1;
    public override int IndentationToSubtract => 1;
}
