using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class DecompileData
{
    public static void Process(string inputDir, string outputDir, HashSet<RestoreStatement> restoreStatements)
    {
        var inputPath = inputDir.AppendToPath("Data").AppendToPath("__DATA.txt");
        var outputPath = outputDir.AppendToPath("Data.bb");
        if (!File.Exists(inputPath)) { return; }

        var inputLines = File
            .ReadAllLines(inputPath)
            .Skip(1)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();
        int currOffset = 0;
        var outputLines = new List<string>();
        foreach (var line in inputLines)
        {
            if (restoreStatements.Any(stmt => stmt.Offset == $"DATA_{currOffset:X8}"))
            {
                if (outputLines.Count > 0)
                {
                    outputLines.Add("");
                }

                outputLines.Add($".DATA_{currOffset:X8}");
            }

            var value = line[4..];
            var type = line[..4] switch
            {
                "STR:" => DeclType.String,
                "FLT:" => DeclType.Float,
                "INT:" => DeclType.Int,
                _ => throw new ArgumentOutOfRangeException()
            };
            outputLines.Add($"Data {ConvertConstantsToFinalRepresentation.ConvertConstant(new ConstantExpression(value), type).StringRepresentation}");
            currOffset += 8;
        }

        if (outputLines.Count > 0)
        {
            File.WriteAllLines(outputPath, outputLines);
        }
    }
}