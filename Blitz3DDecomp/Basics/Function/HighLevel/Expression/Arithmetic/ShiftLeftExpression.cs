namespace Blitz3DDecomp.HighLevel;

sealed record ShiftLeftExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Shl {Rhs.StringRepresentation})";
}