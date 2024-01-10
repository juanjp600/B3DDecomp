namespace Blitz3DDecomp.HighLevel;

sealed record DeleteEachStatement(CustomType ObjectType) : Statement
{
    public override string StringRepresentation
        => $"Delete Each {ObjectType.Name}";
}