﻿using System.Globalization;

namespace Blitz3DDecomp;

static class CountArguments
{
    public static void Process(Function function)
    {
        if (!function.AssemblySections.Any()) { return; }
        var leaveSection = function.AssemblySections.Last(kvp => kvp.Key.Contains($"_leave{function.CoreSymbolName}")).Value;
        var retInstruction = leaveSection.Instructions[^1];
        var retValueStr = retInstruction.DestArg[2..];
        var retValue = int.Parse(retValueStr, NumberStyles.HexNumber);
        function.Parameters.AddRange(Enumerable.Range(0, retValue / 4).Select(i => new Function.Parameter($"arg{i}", i) { DeclType = DeclType.Unknown }));
    }
}