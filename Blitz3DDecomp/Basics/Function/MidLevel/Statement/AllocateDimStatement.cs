namespace Blitz3DDecomp.MidLevel;

sealed record AllocateDimStatement(DimArray Dim, params Expression[] Dimensions) : Statement;