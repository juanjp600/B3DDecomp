﻿using Blitz3DDecomp.HighLevel;

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
                case OrExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
                case XorExpression { Lhs: var tempLhs, Rhs: var tempRhs }:
                    lhs = tempLhs;
                    rhs = tempRhs;
                    break;
            }

            if (lhs is VariableExpression { Variable: Function.DecompGeneratedTempVariable }
                && lhs == rhs)
            {
                return lhs;
            }

            if (expression is ShiftRightUnsignedExpression
                {
                    Lhs: ShiftLeftExpression
                    {
                        Lhs: var unshiftedExpression,
                        Rhs: ConstantExpression { Value: "0x2" }
                    },
                    Rhs: ConstantExpression { Value: "0x2" }
                })
            {
                return unshiftedExpression;
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