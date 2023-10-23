﻿using System.Text.RegularExpressions;
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
            var newFunction = new Function(functionName, new Dictionary<string, Function.AssemblySection>());
            Function.AssemblySection? currentSection = null;

            void commitCurrentSection()
            {
                if (currentSection is null) { return; }

                currentSection.Instructions.RemoveAll(i => i.Name == "nop");
                if (!string.IsNullOrWhiteSpace(currentSectionName))
                {
                    newFunction.AssemblySections[currentSectionName] = currentSection;
                }
            }
            foreach (var line in lines)
            {
                if (currentSection is not null && instructionTrimRegex.Match(line) is { Success: true } instructionTrimMatch)
                {
                    var instructionStr = instructionTrimMatch.Groups[2].Value;
                    var parseNameMatch = instructionNameParseRegex.Match(instructionStr);
                    if (!parseNameMatch.Success)
                    {
                        currentSection.Instructions.Add(new Function.Instruction(name: instructionStr));
                        continue;
                    }
                    var instructionName = parseNameMatch.Groups[1].Value;
                    var instructionArgsStr = parseNameMatch.Groups[2].Value;
                    var parseArgsMatch = instructionArgsParseRegex.Match(instructionArgsStr);
                    if (!parseArgsMatch.Success)
                    {
                        currentSection.Instructions.Add(new Function.Instruction(name: instructionName, leftArg: instructionArgsStr));
                        continue;
                    }
                    currentSection.Instructions.Add(new Function.Instruction(name: instructionName, leftArg: parseArgsMatch.Groups[1].Value.Trim(), rightArg: parseArgsMatch.Groups[2].Value.Trim()));
                }
                else if (symbolDescRegex.Match(line) is { Success: true } symbolDescMatch)
                {
                    commitCurrentSection();
                    currentSectionName = symbolDescMatch.Groups[2].Value;
                    currentSection = new Function.AssemblySection(newFunction, currentSectionName);
                }
            }
            commitCurrentSection();
        }

        
    }
}