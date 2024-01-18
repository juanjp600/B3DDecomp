namespace Blitz3DDecomp.HighLevel;

sealed record CommentStatement(string Text) : Statement
{
    public override string StringRepresentation => $";{Text}";
    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();
    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}