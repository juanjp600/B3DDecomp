namespace Blitz3DDecomp.HighLevel;

sealed record InsertBeforeStatement(Expression ObjectToInsert, Expression ObjectThatComesAfter) : Statement
{
    public override string StringRepresentation
        => $"Insert {ObjectToInsert.StringRepresentation} Before {ObjectThatComesAfter.StringRepresentation}";
}