using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Blitz3DDecomp;

static class CountLocals
{
    public static void Process(Function function)
    {
        if (!function.AssemblySections.Any()) { return; }

        var coreSection =
            function.CoreSymbolName == "__MAIN"
                ? function.AssemblySectionsByName.First(kvp => kvp.Key.EndsWith("_begin__MAIN")).Value
                : function.AssemblySectionsByName[function.CoreSymbolName];
        int startIndex = function.CoreSymbolName == "__MAIN" ? 0 : 5;

        var initializedRegisters = new HashSet<string>();
        var ebpOffsetLocalRegex = new Regex("\\[ebp-0x([0-9a-f]+)\\]");
        var ebpOffsets = new HashSet<int>();
        for (var i = startIndex; i < coreSection.Instructions.Length; i++)
        {
            bool isCallAllowedInPreamble(string instructionArg)
            {
                return instructionArg.EndsWith("_builtIn__bbVecAlloc", StringComparison.OrdinalIgnoreCase)
                       || instructionArg.EndsWith("_builtIn__bbStrRetain", StringComparison.OrdinalIgnoreCase);
            }

            var instruction = coreSection.Instructions[i];
            if (instruction.IsJumpOrCall)
            {
                if (instruction.Name == "call")
                {
                    initializedRegisters.Add("eax");
                }

                if (!isCallAllowedInPreamble(instruction.DestArg))
                {
                    break;
                }
            }

            if (instruction.Name == "mov" && instruction.DestArg.IsRegister() && instruction.SrcArg1 == "0x0")
            {
                initializedRegisters.Add(instruction.DestArg);
                continue;
            }

            if (instruction.Name == "mov"
                && ebpOffsetLocalRegex.Match(instruction.DestArg) is { Success: true } ebpOffsetMatch)
            {
                if (instruction.SrcArg1.IsRegister() && !initializedRegisters.Contains(instruction.SrcArg1)) { break; }

                if (!ebpOffsets.Add(int.Parse(ebpOffsetMatch.Groups[1].Value, NumberStyles.HexNumber))) { break; }

                coreSection.PreambleEndIndex = i;

                continue;
            }

            if (instruction.DestArg.ContainsRegister() && instruction.DestArg.Contains("ebp")) { break; }

            if (instruction.SrcArg1.ContainsRegister() && instruction.SrcArg1.Contains("ebp"))
            {
                bool isParamForLocalInitializer = false;
                if (instruction.SrcArg1.Contains("[ebp+"))
                {
                    for (int j = i + 1; j < coreSection.Instructions.Length; j++)
                    {
                        var instruction2 = coreSection.Instructions[j];
                        if (instruction2.Name != "call") { continue; }

                        isParamForLocalInitializer = isCallAllowedInPreamble(instruction2.DestArg);
                        break;
                    }
                }
                if (!isParamForLocalInitializer) { break; }
            }

            if (instruction.DestArg.Contains("@_v") || instruction.SrcArg1.Contains("@_v")){ break; }

            if (instruction.DestArg.Contains("@_f") || instruction.SrcArg1.Contains("@_f")){ break; }
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
        
        var lastLocalIndex = function.AssemblySections
            .SelectMany(s => s.Instructions.ToArray())
            .SelectMany(i => new[] { i.DestArg, i.SrcArg1, i.SrcArg2 })
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