namespace Blitz3DDecomp.HighLevel;

sealed record CallExpression(Function Callee, params Expression[] Arguments) : Expression
{
    public override string StringRepresentation
        => $"{Callee.Name}({string.Join(", ", Arguments.Select(a => a.StringRepresentation))})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new CallExpression(Callee, Arguments.Select(a => a.Map(mapper)).ToArray()));
    }
}