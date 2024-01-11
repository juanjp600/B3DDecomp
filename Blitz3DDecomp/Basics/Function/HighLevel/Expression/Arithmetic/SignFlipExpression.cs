namespace Blitz3DDecomp.HighLevel;

sealed record SignFlipExpression(Expression OriginalExpression) : Expression
{
    public override string StringRepresentation
        => $"(- {OriginalExpression.StringRepresentation})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(new SignFlipExpression(OriginalExpression.Map(mapper)));
    }
}