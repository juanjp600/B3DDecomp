namespace Blitz3DDecomp.MidLevel;

sealed record SubtractExpression(Expression Lhs, Expression Rhs) : Expression;