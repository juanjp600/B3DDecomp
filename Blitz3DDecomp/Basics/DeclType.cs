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
        var sz = match.Groups[2].Value;
        return new DeclType($"{baseDesc.Suffix}[{sz}]");
    }
}