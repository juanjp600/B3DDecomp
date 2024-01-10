namespace Blitz3DDecomp.HighLevel;

sealed record ArrayAccessExpression(Expression Owner, Expression Index) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.StringRepresentation}[{Index.StringRepresentation}]";
}