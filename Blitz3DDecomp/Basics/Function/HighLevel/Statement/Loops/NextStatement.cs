namespace Blitz3DDecomp.HighLevel;

sealed record NextStatement : Statement
{
    public override string StringRepresentation
        => "Next";
}