namespace Blitz3DDecomp.HighLevel;

sealed record EndIfStatement : Statement
{
    public override string StringRepresentation
        => "EndIf";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToSubtract => 1;
}