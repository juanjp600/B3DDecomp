using System.Diagnostics;
using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.ComparisonResults;
using Blitz3DDecomp.HighLevel.Loops.DoWhile;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupIfs
{
    public static void Process(Function function)
    {
        var sectionsByName = function.HighLevelSectionsByName;
        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            var statement = function.HighLevelStatements[i];
            if (statement is not JumpIfExpressionStatement { Condition: BooleanExpression condition } conditionalJumpStatement) { continue; }

            var jumpStatementSection = sectionsByName[conditionalJumpStatement.SectionName];
            if (jumpStatementSection.StartIndex < i) { continue; }

            int indent = 0;
            bool couldBeValidIf = true;
            for (int j = i; j < jumpStatementSection.StartIndex; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < 0 && function.HighLevelStatements[j].IndentationToAdd > 0)
                {
                    couldBeValidIf = false;
                    break;
                }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }

            if (indent != 0)
            {
                if (indent > 0)
                {
                    for (int j = 1; j <= indent; j++)
                    {
                        if (function.HighLevelStatements[jumpStatementSection.StartIndex - j] is not RepeatStatement)
                        {
                            couldBeValidIf = false;
                            break;
                        }
                    }
                }
                else
                {
                    for (int j = 1; j <= -indent; j++)
                    {
                        if (function.HighLevelStatements[jumpStatementSection.StartIndex - j] is not EndIfStatement)
                        {
                            couldBeValidIf = false;
                            break;
                        }
                    }
                }
                if (!couldBeValidIf) { continue; }
            }

            function.HighLevelStatements[i] = new IfStatement(condition.Negated);

            if (indent == 0)
            {
                jumpStatementSection.Statements.Insert(0, new EndIfStatement());
            }
            else
            {
                int endIfStatementIndex = jumpStatementSection.StartIndex - indent;
                function.FindSectionForStatementIndex(endIfStatementIndex, out var endIfStatementOwner, out var indexInSection);
                endIfStatementOwner.Statements.Insert(indexInSection, new EndIfStatement());
            }
        }
    }
}