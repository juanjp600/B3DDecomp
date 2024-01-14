namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfExpressionStatement(Expression Condition, string SectionName) : Statement
{
    public override string StringRepresentation
        => $"If {Condition.StringRepresentation} Then Goto section{SectionName}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new JumpIfExpressionStatement(Condition.Map(expressionMapper), SectionName));
}