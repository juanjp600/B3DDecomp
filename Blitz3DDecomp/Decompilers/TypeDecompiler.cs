using System.Text.RegularExpressions;
using B3DDecompUtils;

namespace Blitz3DDecomp;

sealed class CustomType
{
    public readonly string Name;
    public readonly List<BasicDeclaration> Fields = new List<BasicDeclaration>();

    public CustomType(string name)
    {
        Name = name;
    }
}

static class TypeDecompiler
{
    public static CustomType[] FromDir(string inputDir, string outputDir)
    {
        var retVal = new List<CustomType>();

        var symbolDescRegex = new Regex("@([0-9A-F]+): _t(.+)");
        var symbolValueRegex = new Regex("    Field (.+): (.+)");
        
        inputDir = inputDir.AppendToPath("Type");
        outputDir = outputDir.AppendToPath("Types");
        Directory.CreateDirectory(outputDir);
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            var lines = File.ReadAllLines(filePath);
            var symbolDescMatch = symbolDescRegex.Match(lines[0]);
            var typeName = symbolDescMatch.Groups[2].Value;
            var newType = new CustomType(typeName);
            foreach (var fieldLine in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var symbolValueMatch = symbolValueRegex.Match(fieldLine);
                var fieldName = $"Field{symbolValueMatch.Groups[1].Value}";
                var fieldType = DeclType.FromDesc(symbolValueMatch.Groups[2].Value);
                newType.Fields.Add(new BasicDeclaration { DeclType = fieldType, Name = fieldName });
            }
            retVal.Add(newType);

            var outputPath = outputDir.AppendToPath($"{typeName}.bb");
            File.AppendAllText(outputPath, $"Type {typeName}\n");
            File.AppendAllLines(outputPath, newType.Fields.Select(f => $"    Field {f.Name}{f.DeclType.Suffix}"));
            File.AppendAllText(outputPath, $"End Type\n");
        }

        return retVal.ToArray();
    }
}