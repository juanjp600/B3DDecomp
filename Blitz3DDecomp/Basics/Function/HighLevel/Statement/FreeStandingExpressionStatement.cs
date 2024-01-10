namespace Blitz3DDecomp.HighLevel;

sealed record FreeStandingExpressionStatement(Expression Expression) : Statement
{
    public override string StringRepresentation
        => Expression.StringRepresentation;
}