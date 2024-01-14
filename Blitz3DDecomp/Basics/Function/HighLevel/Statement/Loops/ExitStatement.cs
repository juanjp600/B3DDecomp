namespace Blitz3DDecomp.HighLevel.Loops;

sealed record ExitStatement : Statement
{
    public override string StringRepresentation
        => "Exit";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}