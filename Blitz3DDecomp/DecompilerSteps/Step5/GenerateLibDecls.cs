using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class GenerateLibDecls
{
    public static void ToDir(string decompPath)
    {
        var outputDir = decompPath.AppendToPath("Decls");
        Directory.CreateDirectory(outputDir);

        foreach (var dll in LibSymbols.Dlls)
        {
            var outputLines = new List<string>();

            outputLines.Add($".lib \"{dll.DllName}\"");
            outputLines.Add("");

            foreach (var entry in dll.Entries)
            {
                var function = Function.GetFunctionByName(entry.DecompName);

                string extractSuffixForBlitzSignature(DeclType declType)
                {
                    if (declType.IsCustomType) { return "*"; }
                    return declType.Suffix;
                }
                var blitzSignature =
                    $"{entry.BlitzName}{extractSuffixForBlitzSignature(function.ReturnType)}({string.Join(", ", function.Parameters.Select(p => p.Name + extractSuffixForBlitzSignature(p.DeclType)))})";
                outputLines.Add($"{blitzSignature}:\"{entry.DllSymbolName}\"");
            }
            File.WriteAllLines(outputDir.AppendToPath(dll.DllName.CleanupPath().Replace("/", "_").Replace(".dll", "") + ".decls"), outputLines);
        }
    }
}