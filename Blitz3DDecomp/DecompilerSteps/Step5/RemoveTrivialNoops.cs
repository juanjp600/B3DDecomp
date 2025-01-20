using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class RemoveTrivialNoops
{
    public static void Process(Function function)
    {
        Expression expressionMapper(Expression expression)
        {
            Expression? lhs = null;
            Expression? rhs = null;
            switch (expression)
            {
                case AndExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
                case LogicalAndExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
                case OrExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
                case LogicalOrExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
                case XorExpression { Lhs: VariableExpression { Variable: Function.DecompGeneratedTempVariable } tempLhs, Rhs: var tempRhs } when tempLhs == tempRhs:
                    return new ConstantExpression(Value: "0x0");
            }

            if (lhs is VariableExpression { Variable: Function.DecompGeneratedTempVariable }
                && lhs == rhs)
            {
                return lhs;
            }

            return expression;
        }

        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            function.HighLevelStatements[i] = function.HighLevelStatements[i]
                .Map(statement => statement, expressionMapper);

            if (function.HighLevelStatements[i] is AssignmentStatement
                {
                    Destination: VariableExpression { Variable: Function.DecompGeneratedTempVariable lhs },
                    Source: VariableExpression { Variable: var rhs }
                }
                && lhs == rhs)
            {
                function.FindSectionForStatementIndex(i, out var section, out var indexInSection);
                section.Statements.RemoveAt(indexInSection);
            }
        }
    }
}