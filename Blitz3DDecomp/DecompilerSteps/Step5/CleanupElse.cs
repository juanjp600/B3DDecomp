using System.Diagnostics;
using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.ComparisonResults;

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
            if (statement is not UnconditionalJumpStatement { PointsToUserSection: false } jumpStatement
                || nextStatement is not EndIfStatement) { continue; }

            var jumpStatementSection = sectionsByName[jumpStatement.SectionName];
            if (jumpStatementSection.StartIndex < i) { continue; }

            int indent = 0;
            for (int j = i + 1; j < jumpStatementSection.StartIndex; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < -1) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent < -1) { continue; }

            var endIfSection = jumpStatementSection;
            while (endIfSection is { IsEmpty: true, NextSection: { } nextSection })
            {
                endIfSection = nextSection;
            }
            
            if (indent > -1)
            {
                bool isValidElse = true;
                for (int j = endIfSection.StartIndex; j < function.HighLevelStatements.Count; j++)
                {
                    if (function.HighLevelStatements[j] is not EndIfStatement)
                    {
                        isValidElse = false;
                        break;
                    }
                    indent -= function.HighLevelStatements[j].IndentationToSubtract;
                    if (indent <= -1) { break; }
                    indent += function.HighLevelStatements[j].IndentationToAdd;
                }
                if (!isValidElse || indent != -1) { continue; }
            }
            endIfSection.Statements.Insert(0, new EndIfStatement());
            if (i > 0 && function.HighLevelStatements[i - 1] is IfStatement { Condition: BooleanExpression ifCondition })
            {
                function.HighLevelStatements[i - 1] = new IfStatement(ifCondition.Negated);
                function.FindSectionForStatementIndex(i + 1, out var nextStatementSection, out var indexInNextSection);
                nextStatementSection.Statements.RemoveAt(indexInNextSection);
            }
            else
            {
                function.HighLevelStatements[i + 1] = new ElseStatement();
            }

            function.FindSectionForStatementIndex(i, out var section, out int indexInSection);
            section.Statements.RemoveAt(indexInSection);
        }
    }
}