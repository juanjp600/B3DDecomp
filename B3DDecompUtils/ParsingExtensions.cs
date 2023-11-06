using System.Globalization;

namespace B3DDecompUtils;

public static class ParsingExtensions
{
    public static UInt32 HexToUint32(this string s)
    {
        if (s.StartsWith("0x")) { s = s[2..]; }
        return UInt32.Parse(s, NumberStyles.HexNumber);
    }
}