namespace Blitz3DDecomp.HighLevel;

sealed record ShiftLeftExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} Shl {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new ShiftLeftExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}