namespace Blitz3DDecomp.MidLevel;

sealed record XorExpression(Expression Lhs, Expression Rhs) : Expression;