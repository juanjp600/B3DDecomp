using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.ComparisonResults;
using Blitz3DDecomp.HighLevel.Loops.DoWhile;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupRepeat
{
    public static void Process(Function function)
    {
        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            var statement = function.HighLevelStatements[i];
            var (sectionName, replacementStatement) = statement switch
            {
                UnconditionalJumpStatement unconditionalJumpStatement
                    => (unconditionalJumpStatement.SectionName, new ForeverStatement()),
                JumpIfExpressionStatement { Condition: BooleanExpression condition } jumpIfExpressionStatement
                    => (jumpIfExpressionStatement.SectionName, new UntilStatement(condition.Negated)),
                _
                    => ("", (Statement?)null)
            };
            if (string.IsNullOrEmpty(sectionName) || replacementStatement is null) { continue; }

            var jumpStatementSection = function.HighLevelSectionsByName[sectionName];
            if (jumpStatementSection.StartIndex > i) { continue; }

            int indent = 0;
            for (int j = jumpStatementSection.StartIndex; j < i; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent < 0) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (indent != 0) { continue; }

            function.HighLevelStatements[i] = replacementStatement;
            var repeatSection = jumpStatementSection;
            while (repeatSection is { LinkedAssemblySection.Instructions.Length: 0, NextSection: { } nextSection })
            {
                repeatSection = nextSection;
            }
            repeatSection.Statements.Insert(0, new RepeatStatement());
            i++;
        }
    }
}