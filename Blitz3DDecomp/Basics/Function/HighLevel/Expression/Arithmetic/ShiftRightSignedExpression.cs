namespace Blitz3DDecomp.HighLevel;

sealed record ShiftRightSignedExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Sar {Rhs.StringRepresentation})";
}