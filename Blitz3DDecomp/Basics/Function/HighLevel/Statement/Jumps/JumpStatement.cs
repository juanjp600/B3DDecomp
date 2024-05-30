namespace Blitz3DDecomp.HighLevel;

abstract record JumpStatement(string SectionName, Function Function) : Statement
{
    public bool PointsToUserSection => SectionName.StartsWith("_l_", StringComparison.Ordinal);

    public override string StringRepresentation => $"Goto {HighLevelSection.CleanupSectionName(SectionName, Function)}";
}