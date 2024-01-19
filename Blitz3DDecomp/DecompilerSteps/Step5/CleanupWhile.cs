using System.Diagnostics;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupWhile
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
            while (jumpStatementSection.Statements.Count == 0)
            {
                if (jumpStatementSection.NextSection is not { } nextSection) { break; }
                jumpStatementSection = nextSection;
            }
            if (jumpStatementSection.Statements.Count < 1) { continue; }

            if (jumpStatementSection.Statements[0] is not JumpIfExpressionStatement conditionalJumpStatement) { continue; }
            var conditionalJumpStatementSection = sectionsByName[conditionalJumpStatement.SectionName];
            if (conditionalJumpStatementSection.StartIndex != i + 1) { continue; }

            int indent = 0;
            for (int j = i; j < jumpStatementSection.StartIndex; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < 0) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent != 0) { continue; }

            jumpStatementSection.Statements[0] = new WendStatement();
            function.HighLevelStatements[i] = new WhileStatement(conditionalJumpStatement.Condition);
        }
    }
}