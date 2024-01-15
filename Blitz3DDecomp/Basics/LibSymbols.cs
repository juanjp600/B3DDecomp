using System.Collections.Immutable;

namespace Blitz3DDecomp;

static class LibSymbols
{
    public readonly record struct Dll(string DllName, ImmutableArray<Entry> Entries);

    public readonly record struct Entry(string DisasmName, string DllSymbolName)
    {
        public int? ParameterCount
            => DllSymbolName.LastIndexOf('@') is >= 0 and var argCountIndex
               && int.TryParse(DllSymbolName[(argCountIndex + 1)..], out var argCount)
               && (argCount % 4 == 0)
                ? (argCount >> 2)
                : null;

        public string BlitzName => DisasmName[2..];
        public string DecompName => BlitzName + "__LIBS";
    }

    public static readonly List<Dll> Dlls = new List<Dll>();
}