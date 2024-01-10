namespace Blitz3DDecomp.HighLevel;

sealed record ConstantExpression(string Value) : Expression
{
    public override string StringRepresentation
        => Value;
}