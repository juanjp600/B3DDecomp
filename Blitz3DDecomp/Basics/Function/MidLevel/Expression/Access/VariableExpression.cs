namespace Blitz3DDecomp.MidLevel;

sealed record VariableExpression(Variable Variable) : AccessExpression
{
    public override string StringRepresentation
        => Variable.Name;
}