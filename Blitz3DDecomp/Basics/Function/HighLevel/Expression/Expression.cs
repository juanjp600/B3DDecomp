namespace Blitz3DDecomp.HighLevel;

abstract record Expression
{
    public abstract string StringRepresentation { get; }
    public abstract Expression Map(Func<Expression, Expression> mapper);
    public abstract IEnumerable<Expression> InnerExpressions { get; }
}