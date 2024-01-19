using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupForOnInt
{
    public static void Process(Function function)
    {
        var sectionsByName = function.HighLevelSectionsByName;
        for (int i = 1; i < function.HighLevelStatements.Count; i++)
        {
            if (function.HighLevelStatements[i] is not WhileStatement whileStatement) { continue; }
            if (function.HighLevelStatements[i - 1] is not AssignmentStatement { Destination: var possibleFirstAssignee, Source: var startValue }) { continue; }
            AccessExpression iterator;
            Expression endValue;
            bool stepMustBePositive;
            switch (whileStatement.Condition)
            {
                case OneIfExpressionIsGreaterThanOrEqualToOtherExpression condition:
                {
                    if (condition.Lhs is AccessExpression possibleIterator)
                    {
                        iterator = possibleIterator;
                        endValue = condition.Rhs;
                        stepMustBePositive = false;
                    }
                    else if (condition.Rhs is AccessExpression possibleIterator2)
                    {
                        iterator = possibleIterator2;
                        endValue = condition.Lhs;
                        stepMustBePositive = true;
                    }
                    else
                    {
                        continue;
                    }
                    break;
                }
                case OneIfExpressionIsLessThanOrEqualToOtherExpression condition:
                {
                    if (condition.Lhs is AccessExpression possibleIterator)
                    {
                        iterator = possibleIterator;
                        endValue = condition.Rhs;
                        stepMustBePositive = true;
                    }
                    else if (condition.Rhs is AccessExpression possibleIterator2)
                    {
                        iterator = possibleIterator2;
                        endValue = condition.Lhs;
                        stepMustBePositive = false;
                    }
                    else
                    {
                        continue;
                    }
                    break;
                }
                default:
                    continue;
            }
            if (iterator != possibleFirstAssignee) { continue; }

            int indent = 1;
            int j = i + 1;
            for (; j < function.HighLevelStatements.Count; j++)
            {
                indent -= function.HighLevelStatements[j].IndentationToSubtract;
                if (indent <= 0) { break; }
                indent += function.HighLevelStatements[j].IndentationToAdd;
            }
            if (j >= function.HighLevelStatements.Count || indent < 0) { continue; }
            if (function.HighLevelStatements[j] is not WendStatement) { continue; }

            if (function.HighLevelStatements[j - 1] is not AssignmentStatement { Destination: VariableExpression destinationExpression, Source: var source }) { continue; }
            if (destinationExpression != iterator) { continue; }

            Expression step;
            switch (source)
            {
                case AddExpression addExpression:
                    if (!stepMustBePositive) { continue; }
                    if (addExpression.Lhs == destinationExpression
                        && addExpression.Rhs is ConstantExpression possibleStep)
                    {
                        step = possibleStep;
                    }
                    else if (addExpression.Rhs == destinationExpression
                        && addExpression.Lhs is ConstantExpression possibleStep2)
                    {
                        step = possibleStep2;
                    }
                    else
                    {
                        continue;
                    }
                    break;
                case SubtractExpression subtractExpression:
                    if (stepMustBePositive) { continue; }
                    if (subtractExpression.Lhs == destinationExpression
                        && subtractExpression.Rhs is ConstantExpression possibleStep3)
                    {
                        step = new SignFlipExpression(possibleStep3);
                    }
                    else
                    {
                        continue;
                    }
                    break;
                default:
                    continue;
            }

            function.HighLevelStatements[j] = new NextStatement();
            function.FindSectionForStatementIndex(j - 1, out var section2, out var indexInSection2);
            section2.Statements.RemoveAt(indexInSection2);
            function.HighLevelStatements[i] = new ForOnIntStatement(iterator, startValue, endValue, step);
            function.FindSectionForStatementIndex(i - 1, out var section1, out var indexInSection1);
            section1.Statements.RemoveAt(indexInSection1);
        }
    }
}