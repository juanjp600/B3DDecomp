namespace Blitz3DDecomp.MidLevel;

sealed record OrExpression(Expression Lhs, Expression Rhs) : Expression;