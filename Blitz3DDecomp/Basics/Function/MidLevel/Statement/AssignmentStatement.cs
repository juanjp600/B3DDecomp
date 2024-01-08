namespace Blitz3DDecomp.MidLevel;

sealed record AssignmentStatement(AccessExpression Destination, Expression Source) : Statement;