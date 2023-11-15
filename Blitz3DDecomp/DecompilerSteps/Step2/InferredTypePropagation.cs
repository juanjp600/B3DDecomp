using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class InferredTypePropagation
{
    private static bool InferReturnTypeInSection(Function function, Function.AssemblySection section)
    {
        if (function.ReturnType != DeclType.Unknown) { return false; }

        var locationTracker = new LocationTracker(trackDirection: -1, initialLocation: "", preserveDeref: true);
        for (int i = section.Instructions.Count - 1; i >= 0; i--)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name is "jmp" && instruction.LeftArg.Contains("_leave"))
            {
                locationTracker.Location = "eax";
            }
            else if (instruction.IsJumpOrCall)
            {
                if (instruction.Name is "call" && locationTracker.Location == "eax")
                {
                    var calleeName = instruction.LeftArg[1..];
                    var calleeFunction = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName || f.Name == calleeName[2..]);
                    if (calleeFunction != null && calleeFunction.ReturnType != DeclType.Unknown)
                    {
                        function.ReturnType = calleeFunction.ReturnType;
                        Logger.WriteLine($"{function.Name} returns {function.ReturnType} because {instruction}");
                        return true;
                    }
                }
                locationTracker.Location = "";
            }

            locationTracker.ProcessInstruction(instruction);

            var variable = function.InstructionArgumentToVariable(locationTracker.Location);

            if (variable != null && variable.DeclType != DeclType.Unknown)
            {
                function.ReturnType = variable.DeclType;
                Logger.WriteLine($"{function.Name} returns {function.ReturnType} because {variable.Name}");
                return true;
            }
        }

        return false;
    }

    private static bool HandleSubCall(Function function, Function callee, int argIndex, Function.AssemblySection section, int assignmentLocation)
    {
        var argType = callee.Parameters[argIndex].DeclType;

        var locationTracker = new LocationTracker(trackDirection: -1, section.Instructions[assignmentLocation].LeftArg, preserveDeref: true);
        for (int i = assignmentLocation; i >= 0; i--)
        {
            var instruction = section.Instructions[i];

            if (instruction.IsJumpOrCall)
            {
                return false;
            }

            if (!locationTracker.ProcessInstruction(instruction)) { continue; }

            var variable = function.InstructionArgumentToVariable(locationTracker.Location);
            if (variable != null)
            {
                if (variable.DeclType != DeclType.Unknown && argType != DeclType.Unknown)
                {
                    // Nothing needs to change here because the types of
                    // both the given argument and its source are known
                    return false;
                }

                if (variable.DeclType == DeclType.Unknown && argType == DeclType.Unknown)
                {
                    // Nothing can change here because both types are unknown
                    return false;
                }

                if (variable.DeclType == DeclType.Unknown)
                {
                    if (variable.Name.Contains("Field")) { Debugger.Break(); }
                    Logger.WriteLine($"{function.Name}'s {variable.Name} is {callee.Parameters[argIndex].DeclType} because {callee.Name}'s arg {argIndex}. {section.Name}:{i}");

                    variable.DeclType = callee.Parameters[argIndex].DeclType;
                    return true;
                }
                else if (argType == DeclType.Unknown
                         && !callee.Name.StartsWith("_builtIn"))
                {
                    Logger.WriteLine($"{callee.Name}'s arg {argIndex} is {variable.DeclType} because {function.Name}'s {variable.Name}. {section.Name}:{i}");
                    callee.Parameters[argIndex].DeclType = variable.DeclType;
                    return true;
                }

                return false;
            }
        }

        return false;
    }

    private static bool HandleSubCalls(Function function, Function.AssemblySection section)
    {
        bool changesMade = false;
        for (int i = section.Instructions.Count - 1; i >= 0; i--)
        {
            if (section.Instructions[i].Name != "call") { continue; }
            var assignmentLocations = section.Instructions[i].CallParameterAssignmentIndices ?? Array.Empty<int>();

            for (var argIndex = 0; argIndex < assignmentLocations.Length; argIndex++)
            {
                var callee = Function.GetFunctionByName(section.Instructions[i].LeftArg)
                    ?? throw new Exception($"Function {section.Instructions[i].LeftArg} not found");
                var assignmentLocation = assignmentLocations[argIndex];
                changesMade |= HandleSubCall(function, callee, argIndex, section, assignmentLocation);
            }
        }

        return changesMade;
    }

    private static bool SmearLocalsAndArgs(Function function, Function.AssemblySection section)
    {
        bool changedSomething = false;

        void smear(Variable declaration, string initialLocation, int smearDir)
        {
            var locationTracker = new LocationTracker(trackDirection: smearDir, initialLocation: initialLocation, preserveDeref: true);
            for (int i = smearDir > 0 ? 0 : section.Instructions.Count - 1;
                 i >= 0 && i < section.Instructions.Count;
                 i += smearDir)
            {
                var instruction = section.Instructions[i];

                if (instruction.IsJumpOrCall)
                {
                    if (smearDir < 0
                        && declaration.DeclType != DeclType.Unknown
                        && instruction.Name == "call"
                        && locationTracker.Location == "eax")
                    {
                        var callee = Function.GetFunctionByName(section.Instructions[i].LeftArg)
                            ?? throw new Exception($"Function {section.Instructions[i].LeftArg} not found");
                        if (callee.AssemblySections.Any() && callee.ReturnType == DeclType.Unknown)
                        {
                            Logger.WriteLine($"{function.Name}: {callee.Name}'s return type is {declaration.DeclType} because {declaration.Name}");
                            callee.ReturnType = declaration.DeclType;
                        }
                    }
                    
                    locationTracker.Location = initialLocation;
                }

                if (!locationTracker.ProcessInstruction(instruction)) { continue; }
                if (declaration.DeclType != DeclType.Unknown) { continue; }

                var variable = function.InstructionArgumentToVariable(locationTracker.Location);
                if (variable != null && variable.DeclType != DeclType.Unknown)
                {
                    declaration.DeclType = variable.DeclType;
                    Logger.WriteLine($"{function.Name}: {declaration.Name} is {variable.DeclType} because {variable.Name}");
                    changedSomething = true;
                    break;
                }
            }
        }

        void smearBothWays(Variable variable, string initialLocation)
        {
            smear(variable, initialLocation, smearDir: -1);
            smear(variable, initialLocation, smearDir: 1);
        }

        foreach (var variable in section.ReferencedVariables)
        {
            smearBothWays(variable, variable.ToInstructionArg());
        }

        return changedSomething;
    }

    public static bool Process(Function function)
    {
        bool changedSomething = false;

        foreach (var section in function.AssemblySections.Values)
        {
            changedSomething |= HandleSubCalls(function, section);
            changedSomething |= InferReturnTypeInSection(function, section);
            changedSomething |= SmearLocalsAndArgs(function, section);
        }

        return changedSomething;
    }
}