namespace Blitz3DDecomp.HighLevel;

sealed record VariableExpression(Variable Variable) : AccessExpression
{
    public override string StringRepresentation
        => Variable.Name;
}