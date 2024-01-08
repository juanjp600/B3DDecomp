namespace Blitz3DDecomp.MidLevel;

sealed record ReturnStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => $"Return {Expression.StringRepresentation}";
}