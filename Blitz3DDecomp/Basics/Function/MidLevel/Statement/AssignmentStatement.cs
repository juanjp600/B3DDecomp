namespace Blitz3DDecomp.MidLevel;

sealed record AssignmentStatement(AccessExpression Destination, Expression Source) : Statement
{
    public override string StringRepresentation
        => $"{Destination.StringRepresentation} = {Source.StringRepresentation}";
}