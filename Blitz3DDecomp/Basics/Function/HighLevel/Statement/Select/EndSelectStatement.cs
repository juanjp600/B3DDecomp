namespace Blitz3DDecomp.HighLevel.Select;

sealed record EndSelectStatement : Statement
{
    public override string StringRepresentation
        => "End Select";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToSubtract => 2;
}