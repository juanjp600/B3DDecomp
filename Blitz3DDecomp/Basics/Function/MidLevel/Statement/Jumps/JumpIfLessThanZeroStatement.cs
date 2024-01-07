namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfLessThanZeroStatement(Expression Expression, MidLevelSection Section) : Statement;