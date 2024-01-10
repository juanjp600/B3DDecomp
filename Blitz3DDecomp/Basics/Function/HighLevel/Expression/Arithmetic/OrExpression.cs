namespace Blitz3DDecomp.HighLevel;

sealed record OrExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Or {Rhs.StringRepresentation})";
}