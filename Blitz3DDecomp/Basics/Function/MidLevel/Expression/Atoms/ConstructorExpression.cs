namespace Blitz3DDecomp.MidLevel;

sealed record ConstructorExpression(CustomType Type) : Expression
{
    public override string StringRepresentation
        => $"New {Type.Name}";
}