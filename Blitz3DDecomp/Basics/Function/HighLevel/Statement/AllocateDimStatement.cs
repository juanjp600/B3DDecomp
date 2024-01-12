namespace Blitz3DDecomp.HighLevel;

sealed record AllocateDimStatement(DimArray Dim, params Expression[] Dimensions) : Statement
{
    public override string StringRepresentation
        => $"Dim {Dim.Name}{Dim.ElementDeclType.Suffix}({string.Join(", ", Dimensions.Select(d => d.StringRepresentation))})";

    public override IEnumerable<Expression> InnerExpressions => Dimensions;

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(new AllocateDimStatement(Dim, Dimensions.Select(expressionMapper).ToArray()));
}