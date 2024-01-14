namespace Blitz3DDecomp.HighLevel.Loops.DoWhile;

sealed record RepeatStatement : Statement
{
    public override string StringRepresentation
        => "Repeat";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(this);

    public override int IndentationToAdd => 1;
}