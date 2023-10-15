using System.Text.RegularExpressions;
using B3DDecompUtils;

namespace Blitz3DDecomp;

public static class IngestCodeFiles
{
    public static void FromDir(string inputDir)
    {
        var symbolDescRegex = new Regex("@([0-9A-F]+): (.+)");
        var instructionTrimRegex = new Regex("    @[0-9A-F]+:( [0-9A-F][0-9A-F])+ +(.+)");
        var instructionNameParseRegex = new Regex("(.+?) (.+)");
        var instructionArgsParseRegex = new Regex("(.+),(.+)");

        inputDir = inputDir.AppendToPath("Code");
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            string functionName = fileName is "__MAIN"
                ? "EntryPoint"
                : fileName;
            if (functionName.StartsWith("_f")) { functionName = functionName[2..]; }

            var lines = File.ReadAllLines(filePath);
            string currentSectionName = "";
            var currentSection = new Function.AssemblySection();
            var sections = new Dictionary<string, Function.AssemblySection>();

            void commitCurrentSection()
            {
                currentSection.Instructions.RemoveAll(i => i.Name == "nop");
                if (!string.IsNullOrWhiteSpace(currentSectionName))
                {
                    sections[currentSectionName] = currentSection;
                }
            }
            foreach (var line in lines)
            {
                if (instructionTrimRegex.Match(line) is { Success: true } instructionTrimMatch)
                {
                    var instructionStr = instructionTrimMatch.Groups[2].Value;
                    var parseNameMatch = instructionNameParseRegex.Match(instructionStr);
                    if (!parseNameMatch.Success)
                    {
                        currentSection.Instructions.Add(new Function.Instruction { Name = instructionStr, LeftArg = "", RightArg = "" });
                        continue;
                    }
                    var instructionName = parseNameMatch.Groups[1].Value;
                    var instructionArgsStr = parseNameMatch.Groups[2].Value;
                    var parseArgsMatch = instructionArgsParseRegex.Match(instructionArgsStr);
                    if (!parseArgsMatch.Success)
                    {
                        currentSection.Instructions.Add(new Function.Instruction { Name = instructionName, LeftArg = instructionArgsStr, RightArg = "" });
                        continue;
                    }
                    currentSection.Instructions.Add(new Function.Instruction { Name = instructionName, LeftArg = parseArgsMatch.Groups[1].Value.Trim(), RightArg = parseArgsMatch.Groups[2].Value.Trim() });
                }
                else if (symbolDescRegex.Match(line) is { Success: true } symbolDescMatch)
                {
                    commitCurrentSection();
                    currentSectionName = symbolDescMatch.Groups[2].Value;
                    currentSection = new Function.AssemblySection();
                }
            }
            commitCurrentSection();
            _ = new Function(functionName, sections);
        }

        
    }
}