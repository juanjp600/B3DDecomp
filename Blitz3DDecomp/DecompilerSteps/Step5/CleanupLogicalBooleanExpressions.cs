using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupLogicalBooleanExpressions
{
    public static void Process(Function function)
    {
        bool containsTemp(Expression expression)
            => expression is VariableExpression { Variable: Function.DecompGeneratedTempVariable }
                || expression.InnerExpressions.Any(containsTemp);

        Function.DecompGeneratedTempVariable extractTemp(Expression expression)
            => expression is VariableExpression { Variable: Function.DecompGeneratedTempVariable variable }
                ? variable
                : extractTemp(expression.InnerExpressions.Where(containsTemp).First());
        
        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            if (function.HighLevelStatements[i] is not JumpIfExpressionStatement { Condition: var condition } jumpStatement
                || !containsTemp(condition))
            {
                continue;
            }

            var varToLookFor = extractTemp(condition);

            function.FindSectionForStatementIndex(i, out var section, out var indexOfJumpStatementInSection);

            var jumpSectionIndex = function.HighLevelSections.FindIndex(s => s.Name == jumpStatement.SectionName);
            if (jumpSectionIndex <= function.HighLevelSections.IndexOf(section)) { continue; }

            var statementsThatConsumeValue = new List<AssignmentStatement>();
            for (int j = indexOfJumpStatementInSection + 1; j < section.Statements.Count; j++)
            {
                if (section.Statements[j] is not AssignmentStatement { Destination: var dest, Source: var source } assignmentStatement) { continue; }

                switch (source)
                {
                    case AndExpression { Lhs: VariableExpression { Variable: Function.DecompGeneratedTempVariable lhsVariableInAnd } }
                        when lhsVariableInAnd == varToLookFor
                             && condition is OneIfZeroExpression:
                    case OrExpression { Lhs: VariableExpression { Variable: Function.DecompGeneratedTempVariable lhsVariableInOr } }
                        when lhsVariableInOr == varToLookFor
                             && condition is OneIfNotZeroExpression:
                        statementsThatConsumeValue.Add(assignmentStatement);
                        break;
                }
            }
            if (statementsThatConsumeValue.Count == 1)
            {
                var consumerIndex = section.Statements.IndexOf(statementsThatConsumeValue[0]);
                section.Statements[consumerIndex] = new AssignmentStatement(
                    statementsThatConsumeValue[0].Destination,
                    statementsThatConsumeValue[0].Source switch
                    {
                        AndExpression { Lhs: var lhsInAnd, Rhs: var rhsInAnd } => new LogicalAndExpression(Lhs: lhsInAnd, Rhs: rhsInAnd),
                        OrExpression { Lhs: var lhsInOr, Rhs: var rhsInOr } => new LogicalOrExpression(Lhs: lhsInOr, Rhs: rhsInOr),
                        _ => throw new NotImplementedException()
                    });
                section.Statements.RemoveAt(indexOfJumpStatementInSection);

                function.RemoveUnreferencedHighLevelSections();

                i--;
            }
        }
    }
}