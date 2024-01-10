namespace Blitz3DDecomp.HighLevel;

sealed record FirstOfTypeExpression(CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"First {ObjectType.Name}";
}