using System.Text.RegularExpressions;
using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp;

public static class IngestCodeFiles
{
    public readonly record struct TempSection(string Name, int StartIndex);

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
            var sections = new List<TempSection>();
            var instructions = new List<Instruction>();

            foreach (var line in lines)
            {
                if (line.Length == 0) { continue; }
                if (line[0] == '@')
                {
                    sections.Add(new TempSection(line[11..].Trim(), instructions.Count));
                }
                else
                {
                    int i = 14;
                    for (; i < line.Length; i+=3)
                    {
                        if (line[i + 0] != ' '
                            || !line[i + 1].IsHexDigit()
                            || !line[i + 2].IsHexDigit())
                        {
                            break;
                        }
                    }

                    for (; i < line.Length; i++)
                    {
                        if (line[i] != ' ') { break; }
                    }

                    int instructionStart = i;
                    for (; i < line.Length; i++)
                    {
                        if (line[i] == ' ') { break; }
                    }
                    string instructionName = line[instructionStart..i];

                    i++;
                    int destArgStart = i;
                    for (; i < line.Length; i++)
                    {
                        if (line[i] == ',') { break; }
                    }
                    string destArg = destArgStart < line.Length ? line[destArgStart..Math.Min(i, line.Length)].Trim() : "";

                    i++;
                    int srcArg1Start = i;
                    for (; i < line.Length; i++)
                    {
                        if (line[i] == ',') { break; }
                    }
                    string srcArg1 = srcArg1Start < line.Length ? line[srcArg1Start..Math.Min(i, line.Length)].Trim() : "";

                    i++;
                    string srcArg2 = i < line.Length ? line[i..].Trim() : "";

                    if (instructionName == "nop") { continue; }
                    instructions.Add(new Instruction(instructionName, destArg, srcArg1, srcArg2));
                }
            }

            var newFunction = new Function(functionName, instructions, sections);
        }

        
    }
}