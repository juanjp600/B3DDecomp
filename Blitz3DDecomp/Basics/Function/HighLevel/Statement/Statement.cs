namespace Blitz3DDecomp.HighLevel;

abstract record Statement
{
    public abstract string StringRepresentation { get; }
    public abstract IEnumerable<Expression> InnerExpressions { get; }

    protected abstract Statement MapImplementation(
        Func<Statement, Statement> statementMapper,
        Func<Expression, Expression> expressionMapper);

    public Statement Map(
        Func<Statement, Statement> statementMapper,
        Func<Expression, Expression>? expressionMapper = null)
    {
        expressionMapper ??= expr => expr;
        return MapImplementation(statementMapper, expressionMapper);
    }
}