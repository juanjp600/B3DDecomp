namespace Blitz3DDecomp.HighLevel;

sealed record FirstOfTypeExpression(CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"(First {ObjectType.Name})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(this);
    }
}