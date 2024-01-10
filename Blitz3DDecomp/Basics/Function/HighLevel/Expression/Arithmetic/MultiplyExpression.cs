namespace Blitz3DDecomp.HighLevel;

sealed record MultiplyExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} * {Rhs.StringRepresentation})";
}