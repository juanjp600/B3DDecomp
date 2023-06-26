using System.Globalization;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class CountArguments
    {
        public static void Process(Function function)
        {
            if (!function.AssemblySections.Any()) { return; }
            var leaveSection = function.AssemblySections.Last(kvp => kvp.Key.Contains($"_leave{function.CoreSymbolName}")).Value;
            var retInstruction = leaveSection[^1];
            var retValueStr = retInstruction.LeftArg[2..];
            var retValue = int.Parse(retValueStr, NumberStyles.HexNumber);
            function.Arguments.AddRange(Enumerable.Range(0, retValue / 4).Select(i => new BasicDeclaration { DeclType = DeclType.Unknown, Name = $"arg{i}" }));
        }
    }
}