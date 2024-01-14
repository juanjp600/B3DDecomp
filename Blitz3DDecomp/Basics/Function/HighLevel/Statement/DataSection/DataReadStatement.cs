namespace Blitz3DDecomp.HighLevel;

sealed record DataReadStatement(AccessExpression Destination) : Statement
{
    public override string StringRepresentation
        => $"Read {Destination.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Destination };

    protected override Statement MapImplementation(Func<Statement, Statement> statementMapper, Func<Expression, Expression> expressionMapper)
    {
        return statementMapper(new DataReadStatement(
            Destination.Map(expressionMapper) as AccessExpression
                ?? throw new Exception("expressionMapper did not return an AccessExpression")));
    }
}