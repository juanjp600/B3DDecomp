namespace Blitz3DDecomp.HighLevel;

sealed record CallExpression(Function Callee, params Expression[] Arguments) : Expression
{
    public override string StringRepresentation
    {
        get
        {
            string calleeName = Callee.Name;
            if (calleeName.StartsWith("_builtIn_f", StringComparison.Ordinal))
            {
                calleeName = calleeName["_builtIn_f".Length..];
            }
            else if (calleeName.EndsWith("__LIBS", StringComparison.Ordinal))
            {
                calleeName = calleeName[..^"__LIBS".Length];
            }
            return $"{calleeName}({string.Join(", ", Arguments.Select(a => a.StringRepresentation))})";
        }
    }

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new CallExpression(Callee, Arguments.Select(a => a.Map(mapper)).ToArray()));
    }

    public override IEnumerable<Expression> InnerExpressions
        => Arguments;
}