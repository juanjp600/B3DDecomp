namespace Blitz3DDecomp.HighLevel;

sealed record ShiftRightUnsignedExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Shr {Rhs.StringRepresentation})";
}