using System.Diagnostics;
using Blitz3DDecomp.HighLevel;
using Blitz3DDecomp.HighLevel.Select;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class CleanupSelect
{
    private readonly record struct TempSection(
        string Name,
        List<Statement> Statements);

    public static void Process(Function function)
    {
        int chainStart = -1;
        bool isSelectExpressionRhs = false;

        int currentSectionNumber = 0;

        Expression getSelectExpression(OneIfExpressionsEqualExpression condition)
            => !isSelectExpressionRhs
                ? condition.Lhs
                : condition.Rhs;
        Expression getCaseExpression(OneIfExpressionsEqualExpression condition)
            => !isSelectExpressionRhs
                ? condition.Rhs
                : condition.Lhs;

        void handleChain(ref int i)
        {
            try
            {
                var chainEnd = i - 1;
                if (chainStart < 0 || chainEnd < 0) { return; }
                if (chainStart == chainEnd) { return; }
                function.FindSectionForStatementIndex(chainStart, out var startSection, out var indexInStartSection);
                function.FindSectionForStatementIndex(chainEnd, out var endSection, out _);
                if (startSection != endSection) { return; }

                var chain = function.HighLevelStatements.Skip(chainStart).Take(chainEnd - chainStart + 1).ToArray();
                if (chain[0] is not JumpIfExpressionStatement { Condition: OneIfExpressionsEqualExpression firstCondition })
                {
                    return;
                }
                var selectInnerExpression = getSelectExpression(firstCondition);

                var cases = new Dictionary<string, List<Expression>>();
                foreach (var selectCase in chain)
                {
                    if (selectCase is not JumpIfExpressionStatement { Condition: OneIfExpressionsEqualExpression condition, SectionName: var sectionName })
                    {
                        return;
                    }
                    if (!cases.ContainsKey(sectionName))
                    {
                        cases.Add(sectionName, new List<Expression>());
                    }
                    cases[sectionName].Add(getCaseExpression(condition));
                }

                var sectionsByName = function.HighLevelSectionsByName;

                var firstCaseSection = cases.Keys.Select(key => sectionsByName[key]).MinBy(s => s.StartIndex)!;
                var firstCaseStatementIndex = firstCaseSection.StartIndex;
                if (firstCaseStatementIndex < startSection.StartIndex) { Debugger.Break(); }

                var tempSections = new List<TempSection>
                {
                    new TempSection($"_endSelect{currentSectionNumber}", new List<Statement>())
                };
                currentSectionNumber++;
                for (int j = function.HighLevelSections.IndexOf(startSection) + 1;
                     j < function.HighLevelSections.IndexOf(firstCaseSection);
                     j++)
                {
                    tempSections.Add(new TempSection(function.HighLevelSections[j].Name, new List<Statement>()));
                }

                for (int j = chainEnd + 1; j < firstCaseStatementIndex; j++)
                {
                    function.FindSectionForStatementIndex(j, out var currentSection, out _);
                    var tempSection = currentSection == startSection
                        ? tempSections[0]
                        : tempSections.First(s => s.Name == currentSection.Name);
                    tempSection.Statements.Add(function.HighLevelStatements[j]);
                }

                var statementToRemoveEverywhere = tempSections.Last().Statements.Last() as UnconditionalJumpStatement;
                if (statementToRemoveEverywhere is null)
                {
                    Debugger.Break();
                    return;
                }

                var lastCaseSection = cases.Keys.Select(key => sectionsByName[key]).MaxBy(s => s.StartIndex)!;
                if (sectionsByName[statementToRemoveEverywhere.SectionName].StartIndex < lastCaseSection.StartIndex)
                {
                    Debugger.Break();
                }

                var chainStartIndexInStartSection = chainStart - startSection.StartIndex;
                int startSectionSize = startSection.Statements.Count;
                for (int j = chainStartIndexInStartSection; j < startSectionSize - 1; j++)
                {
                    var stmt = startSection.Statements[chainStartIndexInStartSection];
                    startSection.Statements.RemoveAt(chainStartIndexInStartSection);
                }

                void cleanUpGotoOfPrevSection(HighLevelSection section)
                {
                    var sectionIndex = function.HighLevelSections.IndexOf(section);
                    var prevIndex = sectionIndex - 1;
                    while (function.HighLevelSections[prevIndex].Statements.Count == 0) { prevIndex--; }
                    var sectionToClean = function.HighLevelSections[prevIndex];
                    if (sectionToClean.Statements[^1] != statementToRemoveEverywhere) { Debugger.Break(); }
                    sectionToClean.Statements.RemoveAt(sectionToClean.Statements.Count - 1);
                }

                foreach (var caseSection in cases.Keys.Select(key => sectionsByName[key]))
                {
                    cleanUpGotoOfPrevSection(caseSection);

                    caseSection.Statements.Insert(0, new CaseStatement(cases[caseSection.Name].ToArray()));
                }

                foreach (var tempSection in tempSections.Skip(1).Reverse())
                {
                    var oldSection = sectionsByName[tempSection.Name];
                    var oldSectionIndex = function.HighLevelSections.IndexOf(oldSection);
                    while (oldSection.Statements.Count > 0)
                    {
                        oldSection.Statements.RemoveAt(0);
                    }
                    function.HighLevelSections.RemoveAt(oldSectionIndex);
                }

                var indexToInsertDefault = function.HighLevelSections.IndexOf(sectionsByName[statementToRemoveEverywhere.SectionName]);
                foreach (var tempSection in tempSections)
                {
                    var newSection = new HighLevelSection(function, tempSection.Name);
                    function.HighLevelSections.Insert(indexToInsertDefault, newSection);
                    indexToInsertDefault++;

                    if (tempSection == tempSections.First())
                    {
                        cleanUpGotoOfPrevSection(newSection);
                    }

                    if (tempSection == tempSections.First()
                        && tempSections.Any(s => s.Statements.Any(stmt => stmt != statementToRemoveEverywhere)))
                    {
                        newSection.Statements.Add(new DefaultStatement());
                    }
                    foreach (var statement in tempSection.Statements)
                    {
                        newSection.Statements.Add(statement);
                    }
                    if (tempSection == tempSections.Last())
                    {
                        if (newSection.Statements[^1] != statementToRemoveEverywhere) { Debugger.Break(); }
                        newSection.Statements[^1] = new EndSelectStatement();
                    }
                }

                startSection.Statements.Add(new SelectStatement(selectInnerExpression));

                i = chainStart;
            }
            finally
            {
                isSelectExpressionRhs = false;
                chainStart = -1;
            }
        }

        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            var statement = function.HighLevelStatements[i];
            if (statement is not JumpIfExpressionStatement { Condition: OneIfExpressionsEqualExpression currentCondition })
            {
                handleChain(ref i);
                continue;
            }

            if (chainStart < 0)
            {
                chainStart = i;
            }
            else
            {
                if (function.HighLevelStatements[chainStart] is not JumpIfExpressionStatement { Condition: OneIfExpressionsEqualExpression chainStartCondition })
                {
                    chainStart = -1;
                    continue;
                }

                if (!isSelectExpressionRhs
                    && currentCondition.Lhs != chainStartCondition.Lhs
                    && currentCondition.Rhs == chainStartCondition.Rhs
                    && currentCondition.Rhs is AccessExpression)
                {
                    isSelectExpressionRhs = true;
                }
                
                if (getSelectExpression(currentCondition) != getSelectExpression(chainStartCondition))
                {
                    handleChain(ref i);
                    continue;
                }
            }
        }
    }
}