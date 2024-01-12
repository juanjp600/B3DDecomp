namespace Blitz3DDecomp.HighLevel;

sealed record UnconditionalJumpStatement(HighLevelSection Section) : Statement
{
    public override string StringRepresentation
        => $"Goto {Section.Name}";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}