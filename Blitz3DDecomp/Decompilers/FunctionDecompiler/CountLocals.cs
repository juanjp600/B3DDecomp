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
            var coreSectionInstructions =
                function.Name == "__MAIN"
                    ? function.AssemblySections.First(kvp => kvp.Key.EndsWith("_begin__MAIN")).Value.Instructions.ToArray()
                    : function.AssemblySections[function.CoreSymbolName].Instructions.Skip(5).ToArray();

            var ebpOffsetLocalRegex = new Regex("\\[ebp-0x([0-9a-f]+)\\]");
            var ebpOffsetArgRegex = new Regex("\\[ebp\\+0x([0-9a-f]+)\\]");
            var ebpOffsets = new HashSet<int>();
            foreach (var instruction in coreSectionInstructions)
            {
                if (ebpOffsetArgRegex.Match(instruction.LeftArg) is { Success: true })
                {
                    break;
                }
                if (instruction.Name == "mov" && ebpOffsetLocalRegex.Match(instruction.LeftArg) is { Success: true } ebpOffsetMatch)
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
            function.LocalVariables.AddRange(Enumerable.Range(0, ebpOffsets.Count).Select(i => new Function.LocalVariable($"local{i}") { DeclType = DeclType.Unknown }));
        }
    }
}