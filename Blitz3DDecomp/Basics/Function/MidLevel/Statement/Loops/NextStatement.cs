namespace Blitz3DDecomp.MidLevel;

sealed record NextStatement : Statement
{
    public override string StringRepresentation
        => "Next";
}