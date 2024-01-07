namespace Blitz3DDecomp.MidLevel;

sealed record AssignmentStatement(Variable Destination, Expression Source) : Statement;