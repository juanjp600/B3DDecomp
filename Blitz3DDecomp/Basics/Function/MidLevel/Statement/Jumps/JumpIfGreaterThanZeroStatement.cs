namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfGreaterThanZeroStatement(Expression Expression, MidLevelSection Section) : Statement;