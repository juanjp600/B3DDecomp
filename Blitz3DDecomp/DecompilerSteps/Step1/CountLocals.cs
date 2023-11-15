using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Blitz3DDecomp;

static class CountLocals
{
    public static void Process(Function function)
    {
        if (!function.AssemblySections.Any()) { return; }

        var coreSectionInstructions =
            function.CoreSymbolName == "__MAIN"
                ? function.AssemblySections.First(kvp => kvp.Key.EndsWith("_begin__MAIN")).Value.Instructions.ToArray()
                : function.AssemblySections[function.CoreSymbolName].Instructions.Skip(5).ToArray();

        var initializedRegisters = new HashSet<string>();
        var ebpOffsetLocalRegex = new Regex("\\[ebp-0x([0-9a-f]+)\\]");
        var ebpOffsets = new HashSet<int>();
        for (var i = 0; i < coreSectionInstructions.Length; i++)
        {
            var instruction = coreSectionInstructions[i];
            if (instruction.IsJumpOrCall)
            {
                if (instruction.Name == "call")
                {
                    initializedRegisters.Add("eax");
                }

                if (!instruction.LeftArg.Contains("_builtIn", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            if (instruction.Name == "mov" && instruction.LeftArg.IsRegister() && instruction.RightArg == "0x0")
            {
                initializedRegisters.Add(instruction.LeftArg);
                continue;
            }

            if (instruction.Name == "mov"
                && ebpOffsetLocalRegex.Match(instruction.LeftArg) is { Success: true } ebpOffsetMatch)
            {
                if (instruction.RightArg.IsRegister()
                    && !initializedRegisters.Contains(instruction.RightArg))
                {
                    break;
                }

                if (!ebpOffsets.Add(int.Parse(ebpOffsetMatch.Groups[1].Value, NumberStyles.HexNumber)))
                {
                    break;
                }

                continue;
            }

            if (instruction.LeftArg.ContainsRegister() && instruction.LeftArg.Contains("ebp"))
            {
                break;
            }

            if (instruction.RightArg.ContainsRegister() && instruction.RightArg.Contains("ebp"))
            {
                bool isParamForLocalInitializer = false;
                if (instruction.RightArg.Contains("[ebp+"))
                {
                    for (int j = i + 1; j < coreSectionInstructions.Length; j++)
                    {
                        var instruction2 = coreSectionInstructions[j];
                        if (instruction2.Name != "call") { continue; }

                        isParamForLocalInitializer = instruction2.LeftArg.Contains("_builtIn");
                        break;
                    }
                }
                if (!isParamForLocalInitializer) { break; }
            }

            if (instruction.LeftArg.Contains("@_v") || instruction.RightArg.Contains("@_v"))
            {
                break;
            }

            if (instruction.LeftArg.Contains("@_f") || instruction.RightArg.Contains("@_f"))
            {
                break;
            }
        }

        for (int i = 4; i <= ebpOffsets.Count * 4; i += 4)
        {
            if (!ebpOffsets.Contains(i))
            {
                Debugger.Break();
            }
        }
        function.LocalVariables.AddRange(Enumerable.Range(0, ebpOffsets.Count)
            .Select(i => new Function.LocalVariable($"local{i}", i) { DeclType = DeclType.Unknown }));
        
        var lastLocalIndex = function.AssemblySections.Values
            .SelectMany(s => s.Instructions)
            .SelectMany(i => new[] { i.LeftArg, i.RightArg })
            .Select(a => a.StripDeref())
            .Where(a => a.StartsWith("ebp-0x", StringComparison.Ordinal))
            .Distinct()
            .Select(a => int.Parse(a[6..], NumberStyles.HexNumber) >> 2)
            .Append(0)
            .Max();
        function.CompilerGeneratedTempVars.AddRange(Enumerable.Range(ebpOffsets.Count, lastLocalIndex - ebpOffsets.Count)
            .Select(i => new Function.LocalVariable($"temp{i}", i) { DeclType = DeclType.Unknown }));
    }
}