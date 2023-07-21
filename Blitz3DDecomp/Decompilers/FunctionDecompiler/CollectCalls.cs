using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class CollectCalls
    {
        private static readonly Dictionary<string, int> guesses = new Dictionary<string, int>();
        
        static void CrawlUp(Function.Instruction[] instructions, int startIndex, out int espDiff, out int finalI, int dep)
        {
            finalI = 0;
            var startInstruction = instructions[startIndex];

            if (startInstruction.LeftArg.ContainsRegister())
            {
                Debugger.Break();
            }

            var functionName = startInstruction.LeftArg[1..];
            var function = Function.AllFunctions.FirstOrDefault(f => f.Name == functionName || f.Name == functionName[2..]);
            espDiff = 0;
            if (function is { Arguments.Count: 0 })
            {
                finalI = startIndex;
                return;
            }
            var foundArgs = new Dictionary<int, int>();
            for (int i = startIndex - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
                if (instruction is { Name: "sub", LeftArg: "esp", RightArg: var newEspOffsetStr })
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
                    CrawlUp(instructions, i, out int newEspDiff, out i, dep + 1);
                    espDiff += newEspDiff;
                }
                else if (instruction.Name == "mov" && instruction.LeftArg.Contains("[esp"))
                {
                    var relativeRegex = new Regex("\\[esp\\+0x([0-9a-f]+)\\]");
                    var thisOffset = 0;
                    if (relativeRegex.Match(instruction.LeftArg) is { Success: true } relativeMatch)
                    {
                        thisOffset = int.Parse(relativeMatch.Groups[1].Value, NumberStyles.HexNumber);
                    }

                    if (foundArgs.ContainsKey(-espDiff + thisOffset))
                    {
                        Debugger.Break();
                    }
                    foundArgs[-espDiff + thisOffset] = i;

                    if ((function != null && -espDiff + thisOffset > function.Arguments.Count * 4) || -espDiff + thisOffset < 0)
                    {
                        Debugger.Break();
                    }
                }

                if (function != null && foundArgs.Count >= function.Arguments.Count)
                {
                    finalI = i;
                    break;
                }
            }

            if (function != null)
            {
                if (foundArgs.Count != function.Arguments.Count) { Debugger.Break(); }
                espDiff += function.Arguments.Count * 4;
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

                Function.AllFunctions.Add(new Function(functionName, foundArgCount));
            }

            startInstruction.CallParameterAssignmentIndices =
                foundArgs.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
            instructions[startIndex] = startInstruction;
        }

        public static void Process(Function function)
        {
            foreach (var kvp in function.AssemblySections)
            {
                var instructions = kvp.Value;
                for (int i = instructions.Length - 1; i >= 0; i--)
                {
                    var instruction = instructions[i];
                    if (instruction.Name != "call") { continue; }
                    CrawlUp(instructions, i, out var espDiff, out i, 0);
                }
            }
        }
    }
}