namespace Blitz3DDecomp.MidLevel;

sealed record AndExpression(Expression Lhs, Expression Rhs) : Expression;