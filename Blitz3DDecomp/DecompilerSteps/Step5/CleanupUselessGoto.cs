using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupUselessGoto
{
    public static void Process(Function function)
    {
        foreach (var section in function.HighLevelSections)
        {
            var sectionStartIndex = section.StartIndex;
            if (sectionStartIndex <= 0) { continue; }
            if (function.HighLevelStatements[sectionStartIndex - 1] is UnconditionalJumpStatement { SectionName: var sectionName }
                && section.Name == sectionName)
            {
                function.FindSectionForStatementIndex(sectionStartIndex - 1, out var prevSection, out var indexInSection);
                prevSection.Statements.RemoveAt(indexInSection);
            }
        }
    }
}