namespace Blitz3DDecomp.HighLevel.ComparisonResults;

abstract record BooleanExpression : Expression
{
    public abstract BooleanExpression Negated { get; }
}