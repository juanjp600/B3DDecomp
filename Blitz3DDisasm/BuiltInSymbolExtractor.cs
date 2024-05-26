using System.Collections.Immutable;
using System.Text;
using AsmResolver;
using AsmResolver.PE.File;

namespace Blitz3DDecomp;

static class BuiltInSymbolExtractor
{
    public static ImmutableArray<string> FromFile(string exePath)
    {
        var peFile = PEFile.FromFile(exePath);

        var result = new List<string>();
        var stringBuilder = new StringBuilder();
        foreach (var section in peFile.Sections)
        {
            if (section.Name is not (".data" or ".rdata")) { continue; }
            var data = (section.Contents as VirtualSegment)?.PhysicalContents?.WriteIntoArray();
            if (data is null)
            {
                return ImmutableArray<string>.Empty;
            }

            for (int i = 0; i < data.Length; i++)
            {
                byte asByte = data[i];
                if (asByte == 0)
                {
                    if (stringBuilder.Length >= 2)
                    {
                        var builtString = stringBuilder.ToString();
                        if (builtString.Contains('%')
                            || builtString.Contains('$')
                            || builtString.Contains('#')
                            || builtString.Contains('*'))
                        {
                            result.Add(builtString);
                        }
                    }
                    stringBuilder.Clear();
                    continue;
                }

                char asChar = (char)asByte;
                if ((char.IsLetterOrDigit(asChar)
                     || asChar is ('_' or '%' or '$' or '#' or '*' or '=' or '.' or '"'))
                    && asByte <= (byte)'z')
                {
                    stringBuilder.Append(asChar);
                }
                else
                {
                    stringBuilder.Clear();
                }
            }
        }
        return result.ToImmutableArray();
    }
}