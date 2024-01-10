﻿namespace Blitz3DDecomp.HighLevel;

sealed record AfterExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"After {OriginalExpression.StringRepresentation}";
}