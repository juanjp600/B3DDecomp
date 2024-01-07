namespace Blitz3DDecomp.MidLevel;

sealed record MultiplyExpression(Expression Lhs, Expression Rhs) : Expression;