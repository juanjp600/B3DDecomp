using System.Diagnostics;
using System.Linq.Expressions;
using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.Utils;
using Expression = Blitz3DDecomp.HighLevel.Expression;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class RemoveSingleUseTemps
{
    private static void ProcessStatement(Function function, int statementIndex)
    {
        var statementToRemove = function.HighLevelStatements[statementIndex];
        if (statementToRemove is not AssignmentStatement
            {
                Destination: VariableExpression { Variable: Function.DecompGeneratedTempVariable tempVariable },
                Source: var srcExpression
            })
        {
            return;
        }

        bool statementReferencesVariable(Statement statement)
        {
            var innerExpressions = statement.InnerExpressions.ToArray();
            while (innerExpressions.Any())
            {
                if (innerExpressions.Any(e =>
                        e is VariableExpression { Variable: var innerVar }
                        && innerVar == tempVariable))
                {
                    return true;
                }
                innerExpressions = innerExpressions.SelectMany(e => e.InnerExpressions).ToArray();
            }
            return false;
        }

        int secondUseIndex = -1;
        bool moreThanOneUse = false;
        int jumpIndex = -1;

        for (int i = statementIndex + 1; i < Math.Min(statementIndex + 60, function.HighLevelStatements.Count); i++)
        {
            var statement = function.HighLevelStatements[i];
            if (jumpIndex < 0 && statement is JumpStatement) { jumpIndex = i; }
            if (!statementReferencesVariable(statement)) { continue; }

            if (secondUseIndex < 0)
            {
                secondUseIndex = i;
            }
            else
            {
                moreThanOneUse = true;
                break;
            }
        }

        if (secondUseIndex > jumpIndex && jumpIndex >= 0) { return; }

        function.FindSectionForStatementIndex(statementIndex, out var section, out var indexInSection);
        if (secondUseIndex < 0)
        {
            bool statementHasSideEffects = false;
            var innerExpressions = statementToRemove.InnerExpressions.ToArray();
            while (innerExpressions.Any())
            {
                if (innerExpressions.Any(e => e is CallExpression or ConstructorExpression))
                {
                    statementHasSideEffects = true;
                    break;
                }
                innerExpressions = innerExpressions.SelectMany(e => e.InnerExpressions).ToArray();
            }

            if (!statementHasSideEffects)
            {
                Logger.WriteLine($"{function}: removing {statementToRemove.StringRepresentation}");
                section.Statements.RemoveAt(indexInSection);
            }
            else
            {
                var freestandingStatement = new FreeStandingExpressionStatement(srcExpression);
                if (srcExpression is not (CallExpression or ConstructorExpression)) { Debugger.Break(); }
                Logger.WriteLine($"{function}: rewriting {statementToRemove.StringRepresentation} as {freestandingStatement.StringRepresentation}");
                function.HighLevelStatements[statementIndex] = freestandingStatement;
            }
            return;
        }

        function.FindSectionForStatementIndex(secondUseIndex, out var secondSection, out _);
        if (section != secondSection) { return; }

        Expression expressionMapper(Expression expression)
            => expression switch
            {
                VariableExpression { Variable: var variable } when variable == tempVariable => srcExpression,
                ArrayAccessExpression { Owner: var owner, Index: var index } => new ArrayAccessExpression(owner, CleanupIndexer.Process(index)),
                _ => expression
            };

        var secondUseStatement = function.HighLevelStatements[secondUseIndex];

        Statement rewrittenStatement;
        if (secondUseStatement is AssignmentStatement { Destination: VariableExpression { Variable: var secondVariable }, Source: var srcExpression2 }
            && secondVariable == tempVariable)
        {
            rewrittenStatement = new AssignmentStatement(new VariableExpression(tempVariable), srcExpression2.Map(expressionMapper));
        }
        else
        {
            if (moreThanOneUse) { return; }

            rewrittenStatement = secondUseStatement.Map(
                statementMapper: statement => statement,
                expressionMapper: expressionMapper);
        }
        Logger.WriteLine($"{function}: rewriting {secondUseStatement.StringRepresentation} as {rewrittenStatement.StringRepresentation} and removing {statementToRemove.StringRepresentation}");
        function.HighLevelStatements[secondUseIndex] = rewrittenStatement;
        section.Statements.RemoveAt(indexInSection);
    }

    public static void Process(Function function)
    {
        for (int i = function.HighLevelStatements.Count - 1; i >= 0; i--)
        {
            ProcessStatement(function, i);
        }
    }
}