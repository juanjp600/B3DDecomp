namespace Blitz3DDecomp.HighLevel;

sealed record ArrayAccessExpression(Expression Owner, Expression Index) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.StringRepresentation}[{Index.StringRepresentation}]";

    public override Expression Map(Func<Expression, Expression> mapper)
        => mapper(new ArrayAccessExpression(Owner.Map(mapper), Index.Map(mapper)));
}