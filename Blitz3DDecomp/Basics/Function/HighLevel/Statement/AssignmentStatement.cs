namespace Blitz3DDecomp.HighLevel;

sealed record AssignmentStatement(AccessExpression Destination, Expression Source) : Statement
{
    public override string StringRepresentation
        => $"{Destination.StringRepresentation} = {Source.StringRepresentation}";
}