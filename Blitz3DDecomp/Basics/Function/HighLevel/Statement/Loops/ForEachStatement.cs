namespace Blitz3DDecomp.HighLevel;

sealed record ForEachStatement(AccessExpression Iterator, CustomType Type) : Statement
{
    public override string StringRepresentation
        => $"For {Iterator.StringRepresentation} = Each {Type.Name}";

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Iterator };

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new ForEachStatement(
            Iterator.Map(expressionMapper) as AccessExpression ?? throw new Exception($"expressionMapper did not return an AccessExpression"),
            Type));
}