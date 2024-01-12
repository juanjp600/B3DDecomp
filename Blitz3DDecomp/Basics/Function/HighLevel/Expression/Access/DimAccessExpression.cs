namespace Blitz3DDecomp.HighLevel;

sealed record DimAccessExpression(DimArray Owner, params Expression[] Indices) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.Name}({string.Join(", ", Indices.Select(i => i.StringRepresentation))})";

    public override Expression Map(Func<Expression, Expression> mapper)
        => mapper(new DimAccessExpression(Owner, Indices.Select(i => i.Map(mapper)).ToArray()));

    public override IEnumerable<Expression> InnerExpressions
        => Indices;
}