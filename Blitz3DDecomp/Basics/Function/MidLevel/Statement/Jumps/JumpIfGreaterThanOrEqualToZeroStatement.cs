namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfGreaterThanOrEqualToZeroStatement(Expression Expression, MidLevelSection Section) : Statement;