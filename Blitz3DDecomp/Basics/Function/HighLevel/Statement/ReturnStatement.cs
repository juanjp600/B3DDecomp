namespace Blitz3DDecomp.HighLevel;

sealed record ReturnStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => $"Return {Expression.StringRepresentation}";
}