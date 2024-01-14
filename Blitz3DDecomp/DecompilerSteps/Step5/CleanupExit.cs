using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.Loops;
using Blitz3DDecomp.HighLevel.Loops.DoWhile;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupExit
{
    public static void Process(Function function)
    {
        var sectionsByName = function.HighLevelSectionsByName;
        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            var statement = function.HighLevelStatements[i];
            if (statement is not UnconditionalJumpStatement jumpStatement) { continue; }

            var jumpStatementSection = sectionsByName[jumpStatement.SectionName];
            if (jumpStatementSection.StartIndex < i) { continue; }

            int exitIndex = 0;
            int innerLoops = 0;
            for (int j = i; j < jumpStatementSection.StartIndex; j++)
            {
                if (function.HighLevelStatements[j] is WhileStatement or ForEachStatement or RepeatStatement)
                {
                    innerLoops++;
                }
                else if (function.HighLevelStatements[j] is NextStatement or WendStatement or UntilStatement or ForeverStatement)
                {
                    innerLoops--;
                    if (innerLoops < 0)
                    {
                        exitIndex = j;
                        break;
                    }
                }
            }
            if (exitIndex != jumpStatementSection.StartIndex - 1) { continue; }

            function.HighLevelStatements[i] = new ExitStatement();
        }
    }
}