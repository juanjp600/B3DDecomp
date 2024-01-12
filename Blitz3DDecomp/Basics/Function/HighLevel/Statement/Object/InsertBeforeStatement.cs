namespace Blitz3DDecomp.HighLevel;

sealed record InsertBeforeStatement(Expression ObjectToInsert, Expression ObjectThatComesAfter) : Statement
{
    public override string StringRepresentation
        => $"Insert {ObjectToInsert.StringRepresentation} Before {ObjectThatComesAfter.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { ObjectToInsert, ObjectThatComesAfter };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new InsertBeforeStatement(
            ObjectToInsert.Map(expressionMapper),
            ObjectThatComesAfter.Map(expressionMapper)));
}