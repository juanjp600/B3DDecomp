using System.Diagnostics;

namespace Blitz3DDecomp;

static class IngestDecls
{
    public static void FromFiles(string[] declsFiles)
    {
        foreach (var declsFile in declsFiles)
        {
            var lines = File.ReadAllLines(declsFile);
            foreach (var line in lines)
            {
                var signatureString = line;
                if (line.IndexOf(":", StringComparison.Ordinal) is (var colonIndex and > 0))
                {
                    signatureString = line[..colonIndex];
                }

                signatureString = new string(signatureString.SelectMany(chr => char.IsWhiteSpace(chr) ? [] : new[]{chr}).ToArray());

                if (signatureString.IndexOf("(", StringComparison.Ordinal) is not (var parenIndex and > 0)) { continue; }

                string flipTypeAnnotationLocation(string str, string defaultType)
                {
                    if (string.IsNullOrWhiteSpace(str)) { return ""; }
                    if (str[^1] is '%' or '#' or '$' or '*')
                    {
                        return str[^1] + str[..^1];
                    }
                    return defaultType + str;
                }
                
                string[] parameters = signatureString[(parenIndex+1)..].Replace(")", "")
                    .Split(",")
                    .Select(parameter => flipTypeAnnotationLocation(parameter, "%"))
                    .ToArray();
                string nameString = flipTypeAnnotationLocation(signatureString[..parenIndex], "");

                var symbolOption = BlitzSymbol.FromString(nameString + string.Join("", parameters));
                if (!symbolOption.TryUnwrap(out var symbol)) { continue; }

                Console.WriteLine(nameString + string.Join("", parameters));

                var newFunction = new Function($"{symbol.FunctionName}__LIBS", 0) { ReturnType = symbol.ReturnType };
                newFunction.Parameters.Clear(); newFunction.Parameters.AddRange(symbol.Parameters.Select((p, i) => new Function.Parameter(p.Name, i) { DeclType = p.DeclType }));
            }
        }
    }
}