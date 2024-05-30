namespace Blitz3DDecomp.HighLevel;

sealed record UnconditionalJumpStatement(string SectionName, Function Function) : JumpStatement(SectionName, Function)
{
    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}