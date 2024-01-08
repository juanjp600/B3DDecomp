namespace Blitz3DDecomp.MidLevel;

sealed record ShiftLeftExpression(Expression Lhs, Expression Rhs) : Expression;