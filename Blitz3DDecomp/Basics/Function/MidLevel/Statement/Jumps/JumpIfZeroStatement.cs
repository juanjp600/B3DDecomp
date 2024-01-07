namespace Blitz3DDecomp.MidLevel;

sealed record JumpIfZeroStatement(Expression Expression, MidLevelSection Section) : Statement;