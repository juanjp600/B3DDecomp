namespace Blitz3DDecomp.HighLevel;

sealed record AndExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} And {Rhs.StringRepresentation})";
}