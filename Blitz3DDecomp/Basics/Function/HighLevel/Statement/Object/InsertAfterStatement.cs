namespace Blitz3DDecomp.HighLevel;

sealed record InsertAfterStatement(Expression ObjectToInsert, Expression ObjectThatComesBefore) : Statement
{
    public override string StringRepresentation
        => $"Insert {ObjectToInsert.StringRepresentation} After {ObjectThatComesBefore.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { ObjectToInsert, ObjectThatComesBefore };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new InsertAfterStatement(
            ObjectToInsert.Map(expressionMapper),
            ObjectThatComesBefore.Map(expressionMapper)));
}