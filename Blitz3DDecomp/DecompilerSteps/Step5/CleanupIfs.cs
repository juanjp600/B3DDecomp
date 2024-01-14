using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.ComparisonResults;

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
            for (int j = i; j < jumpStatementSection.StartIndex; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < 0) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent != 0) { continue; }

            function.HighLevelStatements[i] = new IfStatement(condition.Negated);
            jumpStatementSection.Statements.Insert(0, new EndIfStatement());
        }
    }
}