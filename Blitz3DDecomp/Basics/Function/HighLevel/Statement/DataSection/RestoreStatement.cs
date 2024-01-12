namespace Blitz3DDecomp.HighLevel;

sealed record RestoreStatement(string Offset) : Statement
{
    public override string StringRepresentation
        => $"Restore {Offset}";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}