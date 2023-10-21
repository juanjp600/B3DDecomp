using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

static class InferredTypePropagation
{
    private static bool InferReturnTypeInSection(Function function, Function.AssemblySection section)
    {
        if (function.ReturnType != DeclType.Unknown) { return false; }

        var locationTracker = new LocationTracker(trackDirection: -1, initialLocation: "");
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
                        Console.WriteLine($"{function.Name} returns {function.ReturnType} because {instruction}");
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
                Console.WriteLine($"{function.Name} returns {function.ReturnType} because {variable.Name}");
                return true;
            }
        }

        return false;
    }

    private static bool HandleSubCall(Function function, Function callee, int argIndex, Function.AssemblySection section, int assignmentLocation)
    {
        var argType = callee.Parameters[argIndex].DeclType;

        var locationTracker = new LocationTracker(trackDirection: -1, section.Instructions[assignmentLocation].LeftArg.StripDeref());
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
                    Console.WriteLine($"{function.Name}'s {variable.Name} is {callee.Parameters[argIndex].DeclType} because {callee.Name}'s arg {argIndex}");

                    variable.DeclType = callee.Parameters[argIndex].DeclType;
                    return true;
                }
                else if (argType == DeclType.Unknown
                         && !callee.Name.StartsWith("_builtIn"))
                {
                    Console.WriteLine($"{callee.Name}'s arg {argIndex} is {variable.DeclType} because {function.Name}'s {variable.Name}");
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
                var callee = Function.AllFunctions.Find(f => f.Name == section.Instructions[i].LeftArg[1..] || f.Name == section.Instructions[i].LeftArg[3..])
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

        void smear(Variable declaration, string initialLocation, string declarationDesc, int smearDir)
        {
            var locationTracker = new LocationTracker(trackDirection: smearDir, initialLocation: initialLocation);
            for (int i = smearDir > 0 ? 0 : section.Instructions.Count - 1;
                 i >= 0 && i < section.Instructions.Count;
                 i += smearDir)
            {
                var instruction = section.Instructions[i];

                if (instruction.IsJumpOrCall)
                {
                    locationTracker.Location = initialLocation;
                }

                var (destArg, srcArg) = (instruction.LeftArg.StripDeref(), instruction.RightArg.StripDeref());
                if (smearDir < 0)
                {
                    (destArg, srcArg) = (srcArg, destArg);
                }

                if (!locationTracker.ProcessInstruction(instruction)) { continue; }

                var variable = function.InstructionArgumentToVariable(locationTracker.Location);
                if (variable != null && variable.DeclType != DeclType.Unknown)
                {
                    declaration.DeclType = variable.DeclType;
                    Console.WriteLine($"{function.Name}: {declarationDesc} is {variable.DeclType} because {variable.Name}");
                    changedSomething = true;
                    break;
                }
            }
        }

        for (int localIndex = 0; localIndex < function.LocalVariables.Count; localIndex++)
        {
            if (function.LocalVariables[localIndex].DeclType != DeclType.Unknown) { continue; }

            var variable = function.LocalVariables[localIndex];
            smear(variable, $"ebp-0x{(localIndex * 4) + 0x4:x1}", $"local {localIndex}", smearDir: -1);
            smear(variable, $"ebp-0x{(localIndex * 4) + 0x4:x1}", $"local {localIndex}", smearDir: 1);
            function.LocalVariables[localIndex] = variable;
        }
        
        for (int argIndex = 0; argIndex < function.Parameters.Count; argIndex++)
        {
            if (function.Parameters[argIndex].DeclType != DeclType.Unknown) { continue; }

            var variable = function.Parameters[argIndex];
            smear(variable, $"ebp+0x{(argIndex * 4) + 0x14:x1}", $"arg {argIndex}", smearDir: 1);
            smear(variable, $"ebp+0x{(argIndex * 4) + 0x14:x1}", $"arg {argIndex}", smearDir: -1);
            function.Parameters[argIndex] = variable;
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