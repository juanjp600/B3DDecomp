namespace Blitz3DDecomp.HighLevel;

sealed record FieldAccessExpression(Expression Owner, CustomType.Field Field) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.StringRepresentation}\\{Field.Name}";

    public override Expression Map(Func<Expression, Expression> mapper)
        => mapper(new FieldAccessExpression(Owner.Map(mapper), Field));

    public override IEnumerable<Expression> InnerExpressions { get; } = new[] { Owner };
}