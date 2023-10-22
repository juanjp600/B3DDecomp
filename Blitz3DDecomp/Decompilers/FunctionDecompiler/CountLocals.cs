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
            var lastLocalIndex = function.AssemblySections.Values
                .SelectMany(s => s.Instructions)
                .SelectMany(i => new[] { i.LeftArg, i.RightArg })
                .Select(a => a.StripDeref())
                .Where(a => a.StartsWith("ebp-0x", StringComparison.Ordinal))
                .Distinct()
                .Select(a => int.Parse(a[6..], NumberStyles.HexNumber) >> 2)
                .Append(0)
                .Max();
            function.LocalVariables.AddRange(
                Enumerable.Range(0, lastLocalIndex)
                .Select(i => new Function.LocalVariable($"local{i}")));
        }
    }
}