namespace Blitz3DDecomp.MidLevel;

sealed record ShiftRightSignedExpression(Expression Lhs, Expression Rhs) : Expression;