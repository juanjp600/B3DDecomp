﻿namespace Blitz3DDecomp.HighLevel;

sealed record OneIfLessThanOrEqualToZeroExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"({OriginalExpression.StringRepresentation} <= 0)";
}