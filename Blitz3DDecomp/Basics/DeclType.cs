using System.Text.RegularExpressions;

namespace Blitz3DDecomp;

readonly record struct DeclType(string Suffix)
{
    public static readonly DeclType Unknown = new DeclType("????");
    public static readonly DeclType Int = new DeclType("%");
    public static readonly DeclType Float = new DeclType("#");
    public static readonly DeclType String = new DeclType("$");

    public static DeclType FromDesc(string descStr)
    {
        if (descStr.StartsWith("Vector_"))
        {
            return MakeVector(descStr);
        }
        if (descStr == "_builtIn__bbStrType") { return String; }
        if (descStr == "_builtIn__bbIntType") { return Int; }
        if (descStr == "_builtIn__bbFltType") { return Float; }

        if (descStr.StartsWith("_t"))
        {
            return new DeclType($".{descStr[2..]}");
        }
        return Unknown;
    }

    public static DeclType MakeVector(string descStr)
    {
        var descRegex = new Regex("Vector_[0-9]+(_.+)_sz([0-9]+)");
        var match = descRegex.Match(descStr);
        var baseDesc = FromDesc(match.Groups[1].Value);

        var sz = int.Parse(match.Groups[2].Value);

        // BlitzBasic is dumb so the following:
        //     Local myVar$[10]
        // is actually an 11 element array,
        // so we need to decrement the size here :)
        return new DeclType($"{baseDesc.Suffix}[{sz - 1}]");
    }
}