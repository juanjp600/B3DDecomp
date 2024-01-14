using System.Diagnostics.CodeAnalysis;
using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.ComparisonResults;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupBooleanExpressions
{
    private static Expression MapExpression(Expression expression)
    {
        static bool isSubtract(
            Expression compareExpr,
            [NotNullWhen(returnValue: true)] out SubtractExpression? subtractExpression)
        {
            if (compareExpr is SubtractExpression subtractExpr)
            {
                subtractExpression = subtractExpr;
                return true;
            }

            subtractExpression = null;
            return false;
        }

        SubtractExpression? innerSubtractExpression;
        switch (expression)
        {
            case OneIfZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionsEqualExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfNotZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionsNotEqualExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfLessThanZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionIsLessThanOtherExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfLessThanOrEqualToZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionIsLessThanOrEqualToOtherExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfGreaterThanZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionIsGreaterThanOtherExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfGreaterThanOrEqualToZeroExpression compareExpr when isSubtract(compareExpr.OriginalExpression, out innerSubtractExpression):
                return new OneIfExpressionIsGreaterThanOrEqualToOtherExpression(
                    innerSubtractExpression.Lhs,
                    innerSubtractExpression.Rhs);
            case OneIfZeroExpression { OriginalExpression: BooleanExpression innerBooleanExpression }:
                return innerBooleanExpression.Negated;
            case OneIfNotZeroExpression { OriginalExpression: BooleanExpression innerBooleanExpression }:
                return innerBooleanExpression;
        }

        return expression;
    }

    public static void Process(Function function)
    {
        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            function.HighLevelStatements[i] = function.HighLevelStatements[i]
                .Map(stmt => stmt, MapExpression);
        }
    }
}