namespace Blitz3DDecomp.MidLevel;

sealed record AddExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} + {Rhs.StringRepresentation})";
}