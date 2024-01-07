namespace Blitz3DDecomp.MidLevel;

sealed record AddExpression(Expression Lhs, Expression Rhs) : Expression;