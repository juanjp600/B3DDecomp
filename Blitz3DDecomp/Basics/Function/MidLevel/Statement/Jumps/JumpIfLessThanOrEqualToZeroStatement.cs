namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfLessThanOrEqualToZeroStatement(Expression Expression, MidLevelSection Section) : Statement;