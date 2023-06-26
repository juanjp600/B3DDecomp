﻿using System.Globalization;
using System.Text.RegularExpressions;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class StringConstantDecompiler
{
    public static void FromDir(string inputDir, string outputDir)
    {
        var symbolDescRegex = new Regex("@([0-9A-F]+): (.+)");
        var symbolValueRegex = new Regex("    \"(.+)\" 00");
        
        inputDir = inputDir.AppendToPath("Other");
        var outputPath = outputDir.AppendToPath("Strings.bb");
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length - 2; i++)
            {
                var line = lines[i];
                var nextLine = lines[i+1];
                var symbolDescMatch = symbolDescRegex.Match(line);
                if (!symbolDescMatch.Success) { continue; }
                //var symbolAddressStr = symbolDescMatch.Groups[1].Value;
                //var symbolAddress = int.Parse(symbolAddressStr, NumberStyles.HexNumber);
                var symbolName = symbolDescMatch.Groups[2].Value;

                var symbolValueMatch = symbolValueRegex.Match(nextLine);
                if (!symbolValueMatch.Success) { continue; }
                var symbolValue = symbolValueMatch.Groups[1].Value;
                File.AppendAllText(outputPath, $"Const {symbolName}$ = \"{symbolValue}\"\n");
            }
        }
    }
}