using System.Text.RegularExpressions;
using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp;

public static class IngestCodeFiles
{
    public static void FromDir(string inputDir)
    {
        var symbolDescRegex = new Regex("@([0-9A-F]+): (.+)");
        var instructionTrimRegex = new Regex("    @[0-9A-F]+:( [0-9A-F][0-9A-F])+ +(.+)");
        var instructionNameParseRegex = new Regex("(.+?) (.+)");
        var instructionOneSrcArgsParseRegex = new Regex("(.+),(.+)");
        var instructionTwoSrcArgsParseRegex = new Regex("(.+),(.+),(.+)");

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
            var newFunction = new Function(functionName, new Dictionary<string, AssemblySection>());
            List<Instruction>? currentInstructions = null;

            void commitCurrentSection()
            {
                if (currentInstructions is null) { return; }

                currentInstructions.RemoveAll(i => i.Name == "nop");
                if (!string.IsNullOrWhiteSpace(currentSectionName))
                {
                    newFunction.AssemblySections[currentSectionName] = new AssemblySection(newFunction, currentSectionName, currentInstructions);
                }
            }
            foreach (var line in lines)
            {
                if (currentInstructions is not null && instructionTrimRegex.Match(line) is { Success: true } instructionTrimMatch)
                {
                    var instructionStr = instructionTrimMatch.Groups[2].Value;
                    var parseNameMatch = instructionNameParseRegex.Match(instructionStr);
                    if (!parseNameMatch.Success)
                    {
                        currentInstructions.Add(new Instruction(name: instructionStr));
                        continue;
                    }
                    var instructionName = parseNameMatch.Groups[1].Value;
                    var instructionArgsStr = parseNameMatch.Groups[2].Value;

                    var parseTwoSrcArgsMatch = instructionTwoSrcArgsParseRegex.Match(instructionArgsStr);
                    if (parseTwoSrcArgsMatch.Success)
                    {
                        currentInstructions.Add(new Instruction(
                            name: instructionName,
                            destArg: parseTwoSrcArgsMatch.Groups[1].Value.Trim(),
                            srcArg1: parseTwoSrcArgsMatch.Groups[2].Value.Trim(),
                            srcArg2: parseTwoSrcArgsMatch.Groups[3].Value.Trim()));
                        continue;
                    }

                    var parseOneSrcArgsMatch = instructionOneSrcArgsParseRegex.Match(instructionArgsStr);
                    if (parseOneSrcArgsMatch.Success)
                    {
                        currentInstructions.Add(new Instruction(
                            name: instructionName,
                            destArg: parseOneSrcArgsMatch.Groups[1].Value.Trim(),
                            srcArg1: parseOneSrcArgsMatch.Groups[2].Value.Trim()));
                        continue;
                    }

                    currentInstructions.Add(new Instruction(name: instructionName, destArg: instructionArgsStr));
                }
                else if (symbolDescRegex.Match(line) is { Success: true } symbolDescMatch)
                {
                    commitCurrentSection();
                    currentSectionName = symbolDescMatch.Groups[2].Value;
                    currentInstructions = new List<Instruction>();
                }
            }
            commitCurrentSection();
        }

        
    }
}