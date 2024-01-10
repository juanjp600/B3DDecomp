namespace Blitz3DDecomp.HighLevel;

sealed record ConstructorExpression(CustomType Type) : Expression
{
    public override string StringRepresentation
        => $"New {Type.Name}";
}