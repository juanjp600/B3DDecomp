namespace Blitz3DDecomp.HighLevel.Select;

sealed record SelectStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => $"Select {Expression.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Expression };

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
        => statementMapper(new SelectStatement(Expression.Map(expressionMapper)));

    public override int IndentationToAdd => 2;
}