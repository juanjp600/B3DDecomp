namespace Blitz3DDecomp.HighLevel;

sealed record ForEachStatement(AccessExpression Iterator, CustomType Type) : Statement
{
    public override string StringRepresentation
        => $"For {Iterator.StringRepresentation} = Each {Type.Name}";
}