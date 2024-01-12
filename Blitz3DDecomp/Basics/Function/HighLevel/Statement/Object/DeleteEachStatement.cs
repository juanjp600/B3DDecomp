namespace Blitz3DDecomp.HighLevel;

sealed record DeleteEachStatement(CustomType ObjectType) : Statement
{
    public override string StringRepresentation
        => $"Delete Each {ObjectType.Name}";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}