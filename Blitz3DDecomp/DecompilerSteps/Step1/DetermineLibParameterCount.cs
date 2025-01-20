using System.Collections.Immutable;
using System.Diagnostics;
using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step1;

static class DetermineLibParameterCount
{
    private readonly record struct Equation(
        Function Source,
        ImmutableArray<Function> Functions,
        ImmutableDictionary<Function, int> MaxPossibleParameters,
        int StackUsage)
    {
        public Equation RemoveCallee(Function function, int parameterCount)
        {
            var retVal = this;
            while (retVal.Functions.Contains(function))
            {
                var newStackUsage = retVal.StackUsage - parameterCount;
                retVal = new Equation(
                    Source,
                    retVal.Functions.Remove(function),
                    MaxPossibleParameters
                        .Remove(function)
                        .ToImmutableDictionary(kvp => kvp.Key, kvp => Math.Min(newStackUsage, kvp.Value)),
                    newStackUsage);
            }
            return retVal;
        }

        public override string ToString()
        {
            return string.Join(" + ", Functions
                       .GroupBy(f => f)
                       .OrderByDescending(g => g.Count())
                       .Select(g => $"({g.Key.Name} * {g.Count()})"))
                   + $" = {StackUsage}";
        }
    }

    private readonly record struct Guess(
        Function Callee,
        int ParameterCount);

    private readonly record struct Context(
        List<Equation> Equations,
        List<Guess> Guesses,
        HashSet<Function> SolvedFunctions);

    private static void ProduceEquation(Context context, Function function)
    {
        var instructions = function.Instructions;

        bool firstParameterDefinitelyNotFilled = true;
        var stackFill = 0;
        var libFunctions = new List<Function>();
        var maximumPossibleParameters = new Dictionary<Function, int>();
        for (var instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            switch (instruction.Name)
            {
                case "mov":
                    if (instruction.DestArg.StartsWith("[esp", StringComparison.Ordinal)
                        || instruction.DestArg.StartsWith("dword [esp", StringComparison.Ordinal))
                    {
                        stackFill++;
                        if (instruction.DestArg.EndsWith("esp]", StringComparison.Ordinal))
                        {
                            firstParameterDefinitelyNotFilled = false;
                        }
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
                    if (!callee.Name.EndsWith("__LIBS")
                        || context.SolvedFunctions.Contains(callee))
                    {
                        stackFill -= callee.Parameters.Count;
                        Logger.WriteLine($"{function.Name} calls {callee.Name}({callee.Parameters.Count})");
                        if (stackFill < 0) { Debugger.Break(); }
                        if (stackFill == 0)
                        {
                            firstParameterDefinitelyNotFilled = true;
                        }

                        foreach (var key in maximumPossibleParameters.Keys.ToArray())
                        {
                            maximumPossibleParameters[key] = Math.Min(maximumPossibleParameters[key], stackFill);
                        }
                    }
                    else
                    {
                        if (stackFill == 0 || firstParameterDefinitelyNotFilled)
                        {
                            context.Equations.Add(new Equation(
                                Source: function,
                                Functions: new List<Function> { callee }.ToImmutableArray(),
                                MaxPossibleParameters: new[] { callee }.ToImmutableDictionary(f => f, _ => 0),
                                StackUsage: 0));
                        }
                        else
                        {
                            libFunctions.Add(callee);
                            maximumPossibleParameters[callee] =
                                Math.Min(maximumPossibleParameters.GetValueOrDefault(callee, int.MaxValue), stackFill);
                        }
                    }
                    break;
            }
        }

        if (libFunctions.Any())
        {
            context.Equations.Add(new Equation(
                Source: function,
                Functions: libFunctions.ToImmutableArray(),
                MaxPossibleParameters: maximumPossibleParameters.ToImmutableDictionary(),
                StackUsage: stackFill));
        }
        else if (stackFill != 0)
        {
            //Debugger.Break();
        }
    }

    private static void ProduceGuesses(Context context, Function function)
    {
        var instructions = function.Instructions;

        var stackFill = 0;
        var instructionIndexToStackFill = new Dictionary<int, int>();
        for (var instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            switch (instruction.Name)
            {
                case "mov":
                    if (instruction.DestArg.StartsWith("[esp", StringComparison.Ordinal)
                        || instruction.DestArg.StartsWith("dword [esp", StringComparison.Ordinal))
                    {
                        stackFill++;
                    }
                    break;
                case "call":
                    if (instruction.DestArg.EndsWith("_begin__MAIN")) { continue; }
                    var callee = Function.GetFunctionByName(instruction.DestArg);
                    if (!callee.Name.EndsWith("__LIBS")
                        || context.SolvedFunctions.Contains(callee))
                    {
                        stackFill -= callee.Parameters.Count;
                        if (stackFill < 0) { Debugger.Break(); }

                        foreach (var key in instructionIndexToStackFill.Keys.ToArray())
                        {
                            instructionIndexToStackFill[key] = Math.Min(instructionIndexToStackFill[key], stackFill);
                        }
                    }
                    else
                    {
                        instructionIndexToStackFill[instructionIndex] = stackFill;
                    }
                    break;
            }
        }

        int prevStackFill = 0;
        foreach (var instructionIndex in instructionIndexToStackFill.Keys.OrderBy(i => i))
        {
            var parameterCount = instructionIndexToStackFill[instructionIndex] - prevStackFill;
            prevStackFill = instructionIndexToStackFill[instructionIndex];

            var instruction = instructions[instructionIndex];
            var callee = Function.GetFunctionByName(instruction.DestArg);
            context.Guesses.Add(new Guess(Callee: callee, ParameterCount: parameterCount));
        }
    }

    public static void Execute()
    {
        var functionsWithAssemblySections = Function.AllFunctions.Where(f => f.AssemblySections.Length > 0).ToArray();
        var context = new Context(
            Equations: new List<Equation>(),
            SolvedFunctions: new HashSet<Function>(),
            Guesses: new List<Guess>());

        foreach (var dll in LibSymbols.Dlls)
        {
            foreach (var entry in dll.Entries)
            {
                if (Function.TryGetFunctionByName(entry.DecompName) is { } ingestedFunction)
                {
                    context.SolvedFunctions.Add(ingestedFunction);
                    Logger.WriteLine($"{entry.DecompName} was ingested from decls");
                }
                else if (entry.ParameterCount is { } parameterCount)
                {
                    var newLibFunction = new Function(entry.DecompName, parameterCount);
                    context.SolvedFunctions.Add(newLibFunction);
                    Logger.WriteLine($"Solved {entry.DecompName} from dll symbol {entry.DllSymbolName}");
                }
            }
        }


        foreach (var function in functionsWithAssemblySections)
        {
            ProduceEquation(context, function);
        }

        while (context.Equations.Count > 0)
        {
            var index = context.Equations.FindIndex(p => p.Functions.Distinct().Count() == 1);
            if (index >= 0)
            {
                var equation = context.Equations[index];
                var function = equation.Functions.First();
                var parameterCount = equation.StackUsage / equation.Functions.Length;
                if (parameterCount * equation.Functions.Length != equation.StackUsage)
                {
                    Debugger.Break();
                }
                function.Parameters.AddRange(Enumerable.Range(0, parameterCount).Select(i => new Function.Parameter(function, $"param{i}", i)));
                for (var i = 0; i < context.Equations.Count; i++)
                {
                    context.Equations[i] = context.Equations[i].RemoveCallee(function, parameterCount);
                }
                context.SolvedFunctions.Add(function);
                context.Equations.RemoveAll(eq => eq.Functions.Length == 0);

                Logger.WriteLine($"Trivially solved {function.Name}");
            }
            else
            {
                context.Guesses.Clear();
                foreach (var function in functionsWithAssemblySections)
                {
                    ProduceGuesses(context, function);
                }

                var groupedGuesses = context.Guesses
                    .GroupBy(g => g.Callee)
                    .Select(g => (
                        Key: g.Key,
                        Values: g
                            .Select(v => v.ParameterCount)
                            .GroupBy(v => v)
                            .Select(gg => (ParameterCount: gg.Key, Weight: gg.Count()))
                            .OrderByDescending(pcw => pcw.Weight)
                            .ToArray()))
                    .OrderBy(kvp => kvp.Values.Length)
                    .ThenByDescending(kvp => kvp.Values.Sum(v => v.Weight))
                    .ToArray();
                var bestGuess = groupedGuesses.First();
                var bestGuessFunction = bestGuess.Key;
                var bestGuessParameterCount = bestGuess.Values[0].ParameterCount;
                bestGuessFunction.Parameters.AddRange(Enumerable.Range(0, bestGuessParameterCount).Select(i => new Function.Parameter(bestGuessFunction, $"param{i}", i)));
                context.SolvedFunctions.Add(bestGuessFunction);

                Logger.WriteLine($"Guessed for {bestGuessFunction.Name} out of {bestGuess.Values.Length} choices");

                context.Equations.Clear();
                foreach (var function in functionsWithAssemblySections)
                {
                    ProduceEquation(context, function);
                }
            }
        }
    }
}