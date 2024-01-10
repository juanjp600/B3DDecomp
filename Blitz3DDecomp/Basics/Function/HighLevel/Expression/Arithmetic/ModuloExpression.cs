namespace Blitz3DDecomp.HighLevel;

sealed record ModuloExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Mod {Rhs.StringRepresentation})";
}