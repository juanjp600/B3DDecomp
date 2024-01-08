namespace Blitz3DDecomp.MidLevel;

sealed record ShiftRightUnsignedExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Shr {Rhs.StringRepresentation})";
}