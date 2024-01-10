namespace Blitz3DDecomp.HighLevel;


sealed record LastOfTypeExpression(CustomType ObjectType) : Expression
{
    public override string StringRepresentation
        => $"Last {ObjectType.Name}";
}