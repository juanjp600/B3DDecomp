namespace Blitz3DDecomp.MidLevel;

sealed record OrExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Or {Rhs.StringRepresentation})";
}