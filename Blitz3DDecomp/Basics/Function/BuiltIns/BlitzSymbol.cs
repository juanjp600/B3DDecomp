using System.Collections.Immutable;
using System.Diagnostics;
using B3DDecompUtils.Primitives;

namespace Blitz3DDecomp;

readonly record struct BlitzSymbol(
    string FunctionName,
    ImmutableArray<BlitzSymbol.Parameter> Parameters,
    DeclType ReturnType)
{
    public readonly record struct Parameter(string Name, DeclType DeclType);

    public static Option<BlitzSymbol> FromString(string str)
    {
        static DeclType ripTypeFromStr(ref string str, DeclType defaultType)
        {
            str = str.Replace(" ", "");
            if (str[0] == '#')
            {
                str = str[1..];
                return DeclType.Float;
            }
            if (str[0] == '%')
            {
                str = str[1..];
                return DeclType.Int;
            }
            if (str[0] == '$')
            {
                str = str[1..];
                return DeclType.String;
            }
            if (str[0] == '*')
            {
                str = str[1..];
                return DeclType.Pointer;
            }
            return defaultType;
        }

        DeclType returnType = ripTypeFromStr(ref str, DeclType.Unknown);

        str = str
            .Replace("%", " %")
            .Replace("#", " #")
            .Replace("$", " $")
            .Replace("*", " *");
        var split = str.Split(" ");
        var parameters = new List<Parameter>();
        var funcName = split[0].ToLowerInvariant();
        split = split.Skip(1).ToArray();
        for (var argIndex = 0; argIndex < split.Length; argIndex++)
        {
            var argName = split[argIndex];
            var argType = ripTypeFromStr(ref argName, DeclType.Int);
            parameters.Add(new Parameter(argName, argType));
        }

        if (funcName.Length == 0 || !char.IsLetter(funcName[0])) { return Option.None; }

        return Option.Some(new BlitzSymbol(
            FunctionName: funcName,
            Parameters: parameters.ToImmutableArray(),
            ReturnType: returnType));
    }

    public static readonly Dictionary<string, BlitzSymbol> SymbolsDeclaredByExecutable = new Dictionary<string, BlitzSymbol>();

    public static void Init(string symbolsPath)
    {
        var symbolStrings = File.ReadAllLines(symbolsPath);
        foreach (var s in symbolStrings)
        {
            if (FromString(s).TryUnwrap(out var symbol))
            {
                SymbolsDeclaredByExecutable[symbol.FunctionName] = symbol;
            }
        }
    }
}