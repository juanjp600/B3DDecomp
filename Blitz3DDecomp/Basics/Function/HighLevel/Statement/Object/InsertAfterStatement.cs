namespace Blitz3DDecomp.HighLevel;

sealed record InsertAfterStatement(Expression ObjectToInsert, Expression ObjectThatComesBefore) : Statement
{
    public override string StringRepresentation
        => $"Insert {ObjectToInsert.StringRepresentation} After {ObjectThatComesBefore.StringRepresentation}";
}