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

        void handleChain(ref int outerLoopIndex)
        {
            try
            {
                var chainEnd = outerLoopIndex - 1;
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

                // Match each section that's jumped to
                // to the expressions representing the cases
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

                var allCaseSections = cases.Keys
                    .Select(key => sectionsByName[key])
                    .OrderBy(s => s.StartIndex)
                    .ToArray();

                var firstCaseSection = allCaseSections.First();
                var firstCaseStatementIndex = firstCaseSection.StartIndex;
                if (firstCaseStatementIndex < startSection.StartIndex) { Debugger.Break(); }

                // Generate new sections for the default case,
                // because that's first in assembly but last in high level code
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
                while (tempSections.Last().Statements.Count == 0) { tempSections.RemoveAt(tempSections.Count - 1); }

                // Every case ends with an unconditional jump to the same place
                // so we'll get it from the default case if it exists
                var statementToRemoveEverywhere = tempSections.SelectMany(s => s.Statements).Last() as UnconditionalJumpStatement;
                if (statementToRemoveEverywhere is null)
                {
                    //Debugger.Break();
                    return;
                }

                // Check that all cases actually have this same unconditional jump
                for (var j = 0; j < allCaseSections.Length; j++)
                {
                    var caseSection = allCaseSections[j];
                    var nextSection = j < allCaseSections.Length - 1 ? allCaseSections[j + 1] : sectionsByName[statementToRemoveEverywhere.SectionName];

                    int lastStatementOfCase = nextSection.StartIndex - 1;
                    function.FindSectionForStatementIndex(lastStatementOfCase, out var lastSectionOfCase, out _);
                    if (lastSectionOfCase.Statements[^1] != statementToRemoveEverywhere)
                    {
                        Debugger.Break();
                        return;
                    }
                }

                // Check that the last case actually comes before the end of the Select statement block
                var lastCaseSection = cases.Keys.Select(key => sectionsByName[key]).MaxBy(s => s.StartIndex)!;
                if (sectionsByName[statementToRemoveEverywhere.SectionName].StartIndex < lastCaseSection.StartIndex)
                {
                    Debugger.Break();
                }

                // Remove gotos
                var chainStartIndexInStartSection = chainStart - startSection.StartIndex;
                int startSectionSize = startSection.Statements.Count;
                for (int j = chainStartIndexInStartSection; j < startSectionSize; j++)
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

                for (var j = 0; j < allCaseSections.Length; j++)
                {
                    var caseSection = allCaseSections[j];
                    if (j > 0) { cleanUpGotoOfPrevSection(caseSection); }

                    //caseSection.Statements.Insert(0, new CaseStatement(cases[caseSection.Name].ToArray()));
                }

                // Remove old default case sections
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

                // Regenerate default case sections and insert End Select
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

                // Insert Case statements in a section that comes before the section pointed to by goto
                for (var j = 0; j < allCaseSections.Length; j++)
                {
                    var caseSection = allCaseSections[j];
                    var caseSectionIndex = function.HighLevelSections.IndexOf(caseSection);

                    var newSection = new HighLevelSection(function, $"_selectCase{currentSectionNumber}");
                    currentSectionNumber++;
                    function.HighLevelSections.Insert(caseSectionIndex, newSection);

                    var caseStatement = new CaseStatement(cases[caseSection.Name].ToArray());
                    newSection.Statements.Add(caseStatement);
                    if (function.HighLevelStatements[newSection.StartIndex] != caseStatement) { Debugger.Break(); }
                }

                if (function.HighLevelStatements.Count(stmt => stmt is SelectStatement)
                    != function.HighLevelStatements.Count(stmt => stmt is EndSelectStatement))
                {
                    Debugger.Break();
                }

                outerLoopIndex = chainStart;
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
            if (statement is JumpStatement { PointsToUserSection: true }) { continue; }
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

        // Push Case statements above every empty section that's next to them
        // so remaining gotos can still work.
        // Assembly sections are checked for emptiness because that's representative
        // of the real semantics of the program, some non-empty sections may decompile
        // down to no statements at all.
        for (int i = 0; i < function.HighLevelSections.Count; i++)
        {
            var section = function.HighLevelSections[i];
            if (section.Statements.Count != 1) { continue; }
            if (section.Statements[0] is not CaseStatement) { continue; }

            var reinsertIndex = i;
            while (!function.AssemblySectionsByName.TryGetValue(function.HighLevelSections[reinsertIndex - 1].Name, out var assemblySection)
                   || assemblySection.Instructions.Length == 0)
            {
                reinsertIndex--;
            }
            if (reinsertIndex < i)
            {
                function.HighLevelSections.RemoveAt(i);
                function.HighLevelSections.Insert(reinsertIndex, section);
            }
        }

        for (int i = 1; i < function.HighLevelStatements.Count; i++)
        {
            if (function.HighLevelStatements[i] is not SelectStatement selectStatement) { continue; }

            var selectInnerExpression = selectStatement.Expression;
            if (selectInnerExpression is not VariableExpression { Variable: Function.LocalVariable variable1 }) { continue; }
            if (function.HighLevelStatements[i - 1] is not AssignmentStatement { Destination: VariableExpression { Variable: Function.LocalVariable variable2 }, Source: var assignmentSource }) { continue; }
            if (variable1 != variable2) { continue; }

            function.HighLevelStatements[i] = new SelectStatement(assignmentSource);
            function.FindSectionForStatementIndex(i - 1, out var section, out var indexInSection);
            section.Statements.RemoveAt(indexInSection);
            function.LocalVariables.Remove(variable1);
            i--;
        }
    }
}