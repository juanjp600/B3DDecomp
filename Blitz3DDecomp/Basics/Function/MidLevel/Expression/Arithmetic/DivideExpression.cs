namespace Blitz3DDecomp.MidLevel;

sealed record DivideExpression(Expression Lhs, Expression Rhs) : Expression;