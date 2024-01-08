namespace Blitz3DDecomp.MidLevel;

sealed record DivideExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} / {Rhs.StringRepresentation})";
}