using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class CountLocals
    {
        public static void Process(Function function)
        {
            if (!function.AssemblySections.Any()) { return; }
            var coreSection =
                function.Name == "__MAIN"
                    ? function.AssemblySections.First(kvp => kvp.Key.EndsWith("_begin__MAIN")).Value
                    : function.AssemblySections[function.CoreSymbolName].Skip(5).ToArray();

            var ebpOffsetRegex = new Regex("\\[ebp-0x([0-9a-f]+)\\]");
            var ebpOffsets = new HashSet<int>();
            foreach (var instruction in coreSection)
            {
                if (instruction.Name == "mov" && ebpOffsetRegex.Match(instruction.LeftArg) is { Success: true } ebpOffsetMatch)
                {
                    if (!ebpOffsets.Add(int.Parse(ebpOffsetMatch.Groups[1].Value, NumberStyles.HexNumber)))
                    {
                        break;
                    }
                    continue;
                }

                if (instruction.LeftArg.ContainsRegister() && instruction.LeftArg.Contains("ebp")) { break; }
                if (instruction.RightArg.ContainsRegister() && instruction.RightArg.Contains("ebp")) { break; }
                if (instruction.LeftArg.Contains("@_v") || instruction.RightArg.Contains("@_v")) { break; }
                if (instruction.LeftArg.Contains("@_f") || instruction.RightArg.Contains("@_f")) { break; }
            }

            for (int i = 4; i <= ebpOffsets.Count * 4; i += 4)
            {
                if (!ebpOffsets.Contains(i))
                {
                    Debugger.Break();
                }
            }
            function.LocalVariables.AddRange(Enumerable.Range(0, ebpOffsets.Count).Select(i => new BasicDeclaration { DeclType = DeclType.Unknown, Name = $"local{i}"}));
        }
    }
}