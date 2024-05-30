namespace Blitz3DDecomp.HighLevel;

sealed record JumpIfExpressionStatement(Expression Condition, string SectionName, Function Function) : JumpStatement(SectionName, Function)
{
    public override string StringRepresentation
        => $"If {Condition.StringRepresentation} Then {base.StringRepresentation}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Condition };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new JumpIfExpressionStatement(Condition.Map(expressionMapper), SectionName, Function));
}