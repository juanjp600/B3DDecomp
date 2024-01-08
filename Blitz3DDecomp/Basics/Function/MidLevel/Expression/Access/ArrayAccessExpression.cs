namespace Blitz3DDecomp.MidLevel;

sealed record ArrayAccessExpression(Expression Owner, Expression Index) : AccessExpression;