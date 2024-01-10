namespace Blitz3DDecomp.HighLevel;

sealed record DimAccessExpression(DimArray Owner, params Expression[] Indices) : AccessExpression
{
    public override string StringRepresentation
        => $"{Owner.Name}({string.Join(", ", Indices.Select(i => i.StringRepresentation))})";
}