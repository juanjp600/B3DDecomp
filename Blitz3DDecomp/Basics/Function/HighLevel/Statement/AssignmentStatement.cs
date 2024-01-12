namespace Blitz3DDecomp.HighLevel;

sealed record AssignmentStatement(AccessExpression Destination, Expression Source) : Statement
{
    public override string StringRepresentation
        => $"{Destination.StringRepresentation} = {Source.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Destination, Source };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new AssignmentStatement(
            Destination.Map(expressionMapper) as AccessExpression ?? throw new Exception("expressionMapper did not return an AccessExpression"),
            Source.Map(expressionMapper)));
}