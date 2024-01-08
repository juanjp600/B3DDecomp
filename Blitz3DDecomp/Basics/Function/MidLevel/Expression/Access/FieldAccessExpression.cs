namespace Blitz3DDecomp.MidLevel;

sealed record FieldAccessExpression(Expression Owner, CustomType.Field Field) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.StringRepresentation}\\{Field.Name}";
}