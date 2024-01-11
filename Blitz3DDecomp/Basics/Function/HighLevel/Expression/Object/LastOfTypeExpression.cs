namespace Blitz3DDecomp.HighLevel;


sealed record LastOfTypeExpression(CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"(Last {ObjectType.Name})";

    public override Expression Map(Func<Expression, Expression> mapper)
    {
        return mapper(this);
    }
}