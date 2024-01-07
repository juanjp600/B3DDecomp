namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfNotZeroStatement(Expression Expression, MidLevelSection Section) : Statement;