using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp;

static class CollectCalls
{
    private static readonly Dictionary<string, int> guesses = new Dictionary<string, int>();
    
    static void CrawlUp(AssemblySection section, int startIndex, out int espDiff, out int finalI, int dep)
    {
        finalI = 0;
        var startInstruction = section.Instructions[startIndex];

        espDiff = 0;
        if (startInstruction.DestArg.ContainsRegister())
        {
            //Debugger.Break();
            return;
        }

        var functionName = startInstruction.DestArg[1..];
        var function = Function.TryGetFunctionByName(functionName);
        if (function is { Parameters.Count: 0 })
        {
            startInstruction.CallParameterAssignmentIndices = Array.Empty<int>();
            finalI = startIndex;
            return;
        }
        var foundArgs = new Dictionary<int, int>();
        for (int i = startIndex - 1; i >= 0; i--)
        {
            var instruction = section.Instructions[i];
            if (instruction is { Name: "sub", DestArg: "esp", SrcArg1: var newEspOffsetStr })
            {
                var newEspOffset = int.Parse(newEspOffsetStr[2..], NumberStyles.HexNumber);
                espDiff -= newEspOffset;

                if (espDiff < 0 && function is null)
                {
                    finalI = i;
                    break;
                }
            }
            else if (instruction.Name == "call")
            {
                CrawlUp(section, i, out int newEspDiff, out i, dep + 1);
                function = Function.TryGetFunctionByName(functionName);
                espDiff += newEspDiff;
            }
            else if (instruction.Name == "mov" && instruction.DestArg.Contains("[esp"))
            {
                var relativeRegex = new Regex("\\[esp\\+0x([0-9a-f]+)\\]");
                var thisOffset = 0;
                if (relativeRegex.Match(instruction.DestArg) is { Success: true } relativeMatch)
                {
                    thisOffset = int.Parse(relativeMatch.Groups[1].Value, NumberStyles.HexNumber);
                }

                if (foundArgs.ContainsKey(-espDiff + thisOffset))
                {
                    Debugger.Break();
                }
                foundArgs[-espDiff + thisOffset] = i;

                if ((function != null && -espDiff + thisOffset > function.Parameters.Count * 4) || -espDiff + thisOffset < 0)
                {
                    Debugger.Break();
                }
            }

            if (function != null && foundArgs.Count >= function.Parameters.Count)
            {
                finalI = i;
                break;
            }
        }

        if (function != null)
        {
            if (foundArgs.Count != function.Parameters.Count) { Debugger.Break(); }
            espDiff += function.Parameters.Count * 4;
        }
        else
        {
            foreach (var kvp in foundArgs.OrderBy(k => k.Key))
            {
                if (foundArgs.ContainsKey(kvp.Key + 4)) continue;
                if (foundArgs.Any(kvp2 => kvp2.Key > kvp.Key))
                {
                    Debugger.Break();
                }
                break;
            }

            int foundArgCount =
                foundArgs.ContainsKey(0)
                    ? foundArgs.Count
                    : 0; // The generated assembly tends to not touch [esp] prior to a call to a function with no arguments

            espDiff += foundArgCount * 4;
            if (guesses.TryGetValue(functionName, out var prevGuess) && prevGuess != foundArgCount)
            {
                Debugger.Break();
            }
            guesses.TryAdd(functionName, foundArgCount);
            if (foundArgCount == 0)
            {
                finalI = startIndex;
                espDiff = 0;
            }

            _ = new Function(functionName, foundArgCount);
            if (foundArgCount == 0) { foundArgs.Clear(); }
        }

        startInstruction.CallParameterAssignmentIndices =
            foundArgs.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
    }

    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections)
        {
            var instructions = kvp.Value;
            for (int i = instructions.Instructions.Length - 1; i >= 0; i--)
            {
                var instruction = instructions.Instructions[i];
                if (instruction.Name != "call") { continue; }
                CrawlUp(instructions, i, out var espDiff, out i, 0);
            }
        }
    }
}