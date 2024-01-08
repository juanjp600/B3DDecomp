namespace Blitz3DDecomp.MidLevel;

sealed record FreeStandingExpressionStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => Expression.StringRepresentation;
}