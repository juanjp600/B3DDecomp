namespace Blitz3DDecomp.MidLevel;

sealed record UnconditionalJumpStatement(Expression Expression, MidLevelSection Section) : Statement;