using System.Diagnostics;
using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step1;

static class DetermineLibParameterCountOld
{
    private readonly record struct Guess(
        int ParameterCount,
        float Certainty);
    private readonly record struct Context(
        Dictionary<Function, List<Guess>> GuessedParameterCounts,
        HashSet<Function> SolvedFunctions);
    private static void TrackStackPointer(Context context, Function function)
    {
        var instructions = function.Instructions;

        var hasLocalsOrTemps = instructions.Any(instr =>
            instr.DestArg.Contains("[ebp-", StringComparison.Ordinal)
            || instr.SrcArg1.Contains("[ebp-", StringComparison.Ordinal)
            || instr.SrcArg2.Contains("[ebp-", StringComparison.Ordinal));
        var stackPointer = 0;
        var stackFill = 0;
        var instructionIndexToStackPointer = new Dictionary<int, int>();
        var instructionIndexToStackFill = new Dictionary<int, int>();
        for (var instructionIndex = 1; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            switch (instruction.Name)
            {
                case "sub":
                    if (instruction.DestArg == "esp")
                    {
                        if (instructions[instructionIndex - 1] is not { Name: "mov", DestArg: "ebp", SrcArg1: "esp" }
                            || !hasLocalsOrTemps)
                        {
                            var newSlotCount = (int)instruction.SrcArg1.HexToUint32() >> 2;
                            stackPointer += newSlotCount;
                        }
                    }
                    break;
                case "mov":
                    if (instruction.DestArg.StartsWith("[esp", StringComparison.Ordinal)
                        || instruction.DestArg.StartsWith("dword [esp+"))
                    {
                        stackFill++;
                    }
                    break;
                case "call":
                    if (instruction.DestArg.EndsWith("_begin__MAIN")) { continue; }
                    var callee = Function.TryGetFunctionByName(instruction.DestArg);
                    if (callee is null)
                    {
                        if (instruction.DestArg.EndsWith("__LIBS"))
                        {
                            callee = new Function(instruction.DestArg[3..]);
                            Logger.WriteLine($"New lib function created: {callee.Name}");
                        }
                        else
                        {
                            throw new Exception($"Unrecognized function: {instruction.DestArg}");
                        }
                    }
                    if (callee.Name.EndsWith("__LIBS") && !context.SolvedFunctions.Contains(callee))
                    {
                        instructionIndexToStackPointer[instructionIndex] = stackPointer;
                        instructionIndexToStackFill[instructionIndex] = stackFill;
                        stackFill = 0;
                    }
                    else
                    {
                        for (int j = 0; j < callee.Parameters.Count; j++)
                        {
                            var keysToDecrement = instructionIndexToStackPointer
                                .Where(kvp => kvp.Value >= stackPointer)
                                .Select(kvp => kvp.Key)
                                .ToArray();
                            foreach (var key in keysToDecrement)
                            {
                                instructionIndexToStackPointer[key]--;
                            }
                            stackPointer--;
                        }
                        if (stackPointer < 0) { Debugger.Break(); }
                        stackFill = Math.Max(0, stackFill - callee.Parameters.Count);
                    }
                    break;
            }
        }

        void addGuess(KeyValuePair<int, int> kvp, float certainty, int subtract)
        {
            var instruction = instructions[kvp.Key];
            var callee = Function.GetFunctionByName(instruction.DestArg);
            var parameterCount = kvp.Value - subtract;
            if (!context.GuessedParameterCounts.TryGetValue(callee, out var counts))
            {
                counts = new List<Guess>();
                context.GuessedParameterCounts[callee] = counts;
            }
            counts.Add(new Guess(ParameterCount: parameterCount, Certainty: certainty));
        }

        if (instructionIndexToStackPointer.Count == 0)
        {
            if (stackPointer != 0)
            {
                Debugger.Break();
            }
        }
        if (instructionIndexToStackPointer.Count == 1)
        {
            var kvp = instructionIndexToStackPointer.First();
            addGuess(kvp, certainty: 100000f, subtract: 0);
        }
        else
        {
            int prevCount = 0;
            foreach (var kvp in instructionIndexToStackPointer)
            {
                addGuess(kvp, certainty: 2f, subtract: prevCount);
                prevCount = kvp.Value;
            }
        }
        foreach (var kvp in instructionIndexToStackFill)
        {
            addGuess(kvp, certainty: 1f, subtract: 0);
        }
    }

    public static void Execute()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Length > 0).ToArray();
        var context = new Context(
            GuessedParameterCounts: new Dictionary<Function, List<Guess>>(),
            SolvedFunctions: new HashSet<Function>());
        while (true)
        {
            foreach (var function in functionsWithAssemblySections)
            {
                TrackStackPointer(context, function);
            }

            if (context.GuessedParameterCounts.Count == 0) { break; }

            var mostCertainFunction = context.GuessedParameterCounts.First().Key;
            var parameterCount = 0;
            var certainty = 0f;
            var sumOfAllGuesses = 0f;
            var countToCertainty = new Dictionary<int, float>();
            foreach (var kvp in context.GuessedParameterCounts)
            {
                var function = kvp.Key;
                var guesses = kvp.Value;
                var currentCountToCertainty = new Dictionary<int, float>();
                foreach (var guess in guesses)
                {
                    currentCountToCertainty[guess.ParameterCount] =
                        currentCountToCertainty.GetValueOrDefault(guess.ParameterCount, 0f) + guess.Certainty;
                }

                var mostCertainCountKvp = currentCountToCertainty.MaxBy(kvp => kvp.Value);

                var currentSumOfAllGuesses = guesses.Sum(guess => guess.Certainty);
                if (certainty < mostCertainCountKvp.Value
                    || (certainty == mostCertainCountKvp.Value && parameterCount < mostCertainCountKvp.Key))
                {
                    mostCertainFunction = function;
                    parameterCount = mostCertainCountKvp.Key;
                    certainty = mostCertainCountKvp.Value;
                    sumOfAllGuesses = currentSumOfAllGuesses;
                    countToCertainty = currentCountToCertainty;
                }
            }
            Logger.WriteLine($"Most certain function is {mostCertainFunction.Name} with {parameterCount} parameters (certainty {certainty} / {sumOfAllGuesses})");
            mostCertainFunction.Parameters.AddRange(Enumerable.Range(0, parameterCount).Select(i => new Function.Parameter($"param{i}", i)));
            context.GuessedParameterCounts.Clear();
            context.SolvedFunctions.Add(mostCertainFunction);
        }
    }
}