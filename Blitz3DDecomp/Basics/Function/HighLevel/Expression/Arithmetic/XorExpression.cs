namespace Blitz3DDecomp.HighLevel;

sealed record XorExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Xor {Rhs.StringRepresentation})";
}