﻿namespace Blitz3DDecomp.HighLevel;

sealed record SubtractExpression(Expression Lhs, Expression Rhs) : Expression
{
    public override string StringRepresentation
        => $"({Lhs.StringRepresentation} - {Rhs.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new SubtractExpression(Lhs.Map(mapper), Rhs.Map(mapper)));
    }
}