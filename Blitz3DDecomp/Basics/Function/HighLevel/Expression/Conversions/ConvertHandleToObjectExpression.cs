namespace Blitz3DDecomp.HighLevel;

sealed record ConvertHandleToObjectExpression(Expression HandleExpression, CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"Object.{ObjectType.Name} {HandleExpression.StringRepresentation}";
}