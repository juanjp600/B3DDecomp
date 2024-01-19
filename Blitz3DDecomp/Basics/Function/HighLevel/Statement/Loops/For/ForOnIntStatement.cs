namespace Blitz3DDecomp.HighLevel;

sealed record ForOnIntStatement(AccessExpression Iterator, Expression Start, Expression End, Expression Step) : Statement
{
    public override string StringRepresentation
        => $"For {Iterator.StringRepresentation} = {Start.StringRepresentation} To {End.StringRepresentation} Step {Step.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Iterator };

    protected override Statement MapImplementation(
        Func<Statement, Statement> statementMapper,
        Func<Expression, Expression> expressionMapper)
        => statementMapper(new ForOnIntStatement(
            Iterator.Map(expressionMapper) as AccessExpression ?? throw new Exception($"expressionMapper did not return an AccessExpression"),
            Start.Map(expressionMapper),
            End.Map(expressionMapper),
            Step.Map(expressionMapper)));

    public override int IndentationToAdd => 1;
}