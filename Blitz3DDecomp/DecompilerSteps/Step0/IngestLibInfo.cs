using System.Collections.Immutable;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class IngestLibInfo
{
    public static void FromDir(string disasmPath)
    {
        var inputDir = disasmPath.AppendToPath("Libs");
        if (!Directory.Exists(inputDir)) { return; }
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            var lines = File.ReadAllLines(filePath);
            var dllName = lines[0].Trim();
            var entries = new List<LibSymbols.Entry>();
            foreach (var line in lines.Skip(1))
            {
                var split = line.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var disasmName = split[0];
                var dllSymbolName = split[1];

                entries.Add(new LibSymbols.Entry(disasmName, dllSymbolName));
            }
            LibSymbols.Dlls.Add(new LibSymbols.Dll(dllName, entries.ToImmutableArray()));
        }
    }
}