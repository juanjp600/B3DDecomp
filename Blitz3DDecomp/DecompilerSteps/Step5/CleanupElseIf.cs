using System.Diagnostics;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupElseIf
{
    public static void Process(Function function)
    {
        var sectionsByName = function.HighLevelSectionsByName;
        for (int i = 0; i < function.HighLevelStatements.Count - 1; i++)
        {
            if (function.HighLevelStatements[i] is not ElseStatement
                || function.HighLevelStatements[i + 1] is not IfStatement ifStatement) { continue; }
            int indent = 0;
            int j = i + 2;
            for (; j < function.HighLevelStatements.Count; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < 0) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent != -1
                || function.HighLevelStatements[j] is not EndIfStatement
                || function.HighLevelStatements[j + 1] is not EndIfStatement) { continue; }

            function.FindSectionForStatementIndex(j, out var endIfSection, out var indexInEndIfSection);
            endIfSection.Statements.RemoveAt(indexInEndIfSection);
            function.FindSectionForStatementIndex(i, out var elseIfSection, out var indexInElseIfSection);
            elseIfSection.Statements.RemoveAt(indexInElseIfSection);
            elseIfSection.Statements[indexInElseIfSection] = new ElseIfStatement(ifStatement.Condition);
        }
    }
}