namespace Blitz3DDecomp.MidLevel;

sealed record CallExpression(Function Callee, params Expression[] Arguments) : Expression
{
    public override string StringRepresentation
        => $"{Callee.Name}({string.Join(", ", Arguments.Select(a => a.StringRepresentation))})";
}