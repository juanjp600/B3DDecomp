namespace Blitz3DDecomp.HighLevel;

sealed record DestructorStatement(Expression ObjectExpression) : Statement
{
    public override string StringRepresentation
        => $"Delete {ObjectExpression.StringRepresentation}";
}