namespace Blitz3DDecomp.MidLevel;

sealed record CallExpression(Function Callee, params Expression[] Arguments) : Expression;