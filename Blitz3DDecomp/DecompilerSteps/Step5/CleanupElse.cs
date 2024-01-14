using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupElse
{
    public static void Process(Function function)
    {
        var sectionsByName = function.HighLevelSectionsByName;
        for (int i = 0; i < function.HighLevelStatements.Count - 1; i++)
        {
            var statement = function.HighLevelStatements[i];
            var nextStatement = function.HighLevelStatements[i + 1];
            if (statement is not UnconditionalJumpStatement jumpStatement
                || nextStatement is not EndIfStatement) { continue; }

            var jumpStatementSection = sectionsByName[jumpStatement.SectionName];
            if (jumpStatementSection.StartIndex < i) { continue; }

            int indent = 0;
            for (int j = i; j < jumpStatementSection.StartIndex; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < -1) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent != -1) { continue; }

            jumpStatementSection.Statements.Insert(0, new EndIfStatement());
            function.HighLevelStatements[i + 1] = new ElseStatement();

            function.FindSectionForStatementIndex(i, out var section, out int indexInSection);
            section.Statements.RemoveAt(indexInSection);
        }
    }
}