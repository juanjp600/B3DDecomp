namespace Blitz3DDecomp.MidLevel;

sealed record DimAccessExpression(DimArray Owner, Expression Index) : AccessExpression;