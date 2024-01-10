namespace Blitz3DDecomp.HighLevel;

sealed record FieldAccessExpression(Expression Owner, CustomType.Field Field) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.StringRepresentation}\\{Field.Name}";
}