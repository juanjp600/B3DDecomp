using System.Diagnostics.CodeAnalysis;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class ConvertFunctionCallsToFinalRepresentation
{
    private static Statement MapStatement(Statement statement)
    {
        if (statement is AssignmentStatement { Destination: var destination, Source: CallExpression innerCallExpression }
            && innerCallExpression.Callee.Name is "_builtIn__bbReadInt" or "_builtIn__bbReadFloat" or "_builtIn__bbReadStr")
        {
            return new DataReadStatement(destination);
        }

        return statement;
    }

    private static Expression MapExpression(Expression expression)
    {
        static bool isCompareFunction(
            Expression innerExpression,
            [NotNullWhen(returnValue: true)]out CallExpression? callExpression)
        {
            if (innerExpression is not SubtractExpression
                {
                    Lhs: CallExpression call,
                    Rhs: ConstantExpression { Value: "0x0" }
                })
            {
                callExpression = null;
                return false;
            }

            if (call.Callee.Name is "_builtIn__bbStrCompare" or "_builtIn__bbObjCompare")
            {
                callExpression = call;
                return true;
            }

            callExpression = null;
            return false;
        }

        CallExpression? innerCallExpression;
        switch (expression)
        {
            case OneIfZeroExpression compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionsEqualExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case OneIfNotZeroExpression compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionsNotEqualExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case OneIfLessThanZeroExpression  compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionIsLessThanOtherExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case OneIfLessThanOrEqualToZeroExpression compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionIsLessThanOrEqualToOtherExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case OneIfGreaterThanZeroExpression compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionIsGreaterThanOtherExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case OneIfGreaterThanOrEqualToZeroExpression compareExpr when isCompareFunction(compareExpr.OriginalExpression, out innerCallExpression):
                return new OneIfExpressionIsGreaterThanOrEqualToOtherExpression(
                    innerCallExpression.Arguments[0],
                    innerCallExpression.Arguments[1]);
            case CallExpression callExpression:
                switch (callExpression.Callee.Name)
                {
                    case "_builtIn__bbMod" or "_builtIn__bbFMod":
                        return new ModuloExpression(callExpression.Arguments[0], callExpression.Arguments[1]);
                    case "_builtIn__bbAbs" or "_builtIn__bbFAbs":
                        return new AbsExpression(callExpression.Arguments[0]);
                    case "_builtIn__bbSgn" or "_builtIn__bbFSgn":
                        return new SignExpression(callExpression.Arguments[0]);
                    case "_builtIn__bbFPow":
                        return new ExponentiationExpression(callExpression.Arguments[0], callExpression.Arguments[1]);
                    case "_builtIn__bbStrConcat":
                        return new AddExpression(callExpression.Arguments[0], callExpression.Arguments[1]);
                    case "_builtIn__bbStrToInt":
                        return new ConvertToIntExpression(callExpression.Arguments[0]);
                    case "_builtIn__bbStrToFloat":
                        return new ConvertToFloatExpression(callExpression.Arguments[0]);
                    case "_builtIn__bbStrFromInt" or "_builtIn__bbStrFromFloat":
                        return new ConvertToStringExpression(callExpression.Arguments[0]);
                }
                break;
        }

        return expression;
    }

    private static void ProcessStatement(Function function, int i)
    {
        var statement = function.HighLevelStatements[i];
        function.HighLevelStatements[i] = statement.Map(
            MapStatement,
            MapExpression);
    }

    public static void Process(Function function)
    {
        for (var i = 0; i < function.HighLevelStatements.Count; i++)
        {
            ProcessStatement(function, i);
        }
    }
}