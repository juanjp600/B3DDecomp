namespace Blitz3DDecomp.HighLevel;

sealed record ModuloExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Mod {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ModuloExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}