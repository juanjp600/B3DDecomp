namespace Blitz3DDecomp.HighLevel;

sealed record RestoreStatement(string Offset) : Statement
{
    public override string StringRepresentation
        => $"Restore {Offset}";
}