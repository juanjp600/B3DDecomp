using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

static class InferredTypePropagation
{
    private static bool InferReturnTypeInSection(Function function, Function.AssemblySection section)
    {
        if (function.ReturnType != DeclType.Unknown) { return false; }

        var trackedLocation = "";
        for (int i = section.Instructions.Count - 1; i >= 0; i--)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name is "jmp" && instruction.LeftArg.Contains("_leave"))
            {
                trackedLocation = "eax";
            }
            else if (instruction.Name is
                "call" or "jmp" or "je" or "jz"
                or "jne" or "jnz" or "jg"
                or "jge" or "jl" or "jle")
            {
                if (instruction.Name is "call" && trackedLocation == "eax")
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
                trackedLocation = "";
            }

            if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
            {
                trackedLocation = instruction.RightArg.StripDeref();
            }

            if (trackedLocation.Contains("ebp-0x"))
            {
                int varIndex = (int.Parse(trackedLocation[6..], NumberStyles.HexNumber) >> 2) - 1;
                if (varIndex >= 0
                    && varIndex < function.LocalVariables.Count
                    && function.LocalVariables[varIndex].DeclType != DeclType.Unknown)
                {
                    function.ReturnType = function.LocalVariables[varIndex].DeclType;
                    Console.WriteLine($"{function.Name} returns {function.ReturnType} because local {varIndex}");
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HandleSubCall(Function function, Function callee, int argIndex, Function.AssemblySection section, int assignmentLocation)
    {
        var argType = callee.Parameters[argIndex].DeclType;

        var trackedLocation = section.Instructions[assignmentLocation].LeftArg.StripDeref();
        for (int i = assignmentLocation; i >= 0; i--)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name is "mov" or "lea" or "xchg" &&
                instruction.LeftArg.StripDeref() == trackedLocation)
            {
                trackedLocation = instruction.RightArg.StripDeref();

                if (trackedLocation.StartsWith("ebp"))
                {
                    var typeForLocation = GetTypeForLocation(function, trackedLocation);
                    if (typeForLocation != DeclType.Unknown && argType != DeclType.Unknown)
                    {
                        // Nothing needs to change here because the types of
                        // both the given argument and its source are known
                        return false;
                    }

                    if (typeForLocation == DeclType.Unknown && argType == DeclType.Unknown)
                    {
                        // Nothing can change here because both types are unknown
                        return false;
                    }

                    bool propagateType(string sourceDesc, Action propagateFromArgToSource)
                    {
                        if (typeForLocation == DeclType.Unknown)
                        {
                            Console.WriteLine($"{sourceDesc} is {callee.Parameters[argIndex].DeclType} because {callee.Name}'s arg {argIndex}");
                            propagateFromArgToSource();
                            return true;
                        }
                        else if (argType == DeclType.Unknown
                                 && !callee.Name.StartsWith("_builtIn"))
                        {
                            Console.WriteLine($"{callee.Name}'s arg {argIndex} is {typeForLocation} because {sourceDesc}");
                            callee.Parameters[argIndex].DeclType = typeForLocation;
                            return true;
                        }
                        return false;
                    }
                    
                    if (trackedLocation.StartsWith("ebp-0x"))
                    {
                        // This is a local
                        int varIndex = (int.Parse(trackedLocation[6..], NumberStyles.HexNumber) - 0x4) >> 2;
                        if (varIndex >= 0
                            && varIndex < function.LocalVariables.Count)
                        {
                            return propagateType(
                                sourceDesc: $"{function.Name}'s local {varIndex}",
                                propagateFromArgToSource: () => function.LocalVariables[varIndex] .DeclType = argType);
                        }
                    }
                    else if (trackedLocation.StartsWith("ebp+0x"))
                    {
                        // This is an argument
                        int paramIndex = (int.Parse(trackedLocation[6..], NumberStyles.HexNumber) - 0x14) >> 2;
                        if (paramIndex >= 0
                            && paramIndex < function.Parameters.Count)
                        {
                            return propagateType(
                                sourceDesc: $"{function.Name}'s arg {paramIndex}",
                                propagateFromArgToSource: () => function.Parameters[paramIndex].DeclType = argType);
                        }
                    }

                    return false;
                }
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

    private static DeclType GetTypeForLocation(Function function, string location)
    {
        if (location.StartsWith("ebp-0x"))
        {
            // This is a local
            int varIndex = (int.Parse(location[6..], NumberStyles.HexNumber) - 0x4) >> 2;
            if (varIndex >= 0
                && varIndex < function.LocalVariables.Count)
            {
                return function.LocalVariables[varIndex].DeclType;
            }
        }

        if (location.StartsWith("ebp+0x"))
        {
            // This is an argument
            int paramIndex = (int.Parse(location[6..], NumberStyles.HexNumber) - 0x14) >> 2;
            if (paramIndex >= 0
                && paramIndex < function.Parameters.Count)
            {
                return function.Parameters[paramIndex].DeclType;
            }
        }

        return DeclType.Unknown;
    }

    private static bool SmearLocalsAndArgs(Function function, Function.AssemblySection section)
    {
        bool changedSomething = false;

        void smear(Variable declaration, string initialLocation, string declarationDesc, int smearDir)
        {
            var trackedLocation = initialLocation;
            for (int i = smearDir > 0 ? 0 : section.Instructions.Count - 1;
                 i >= 0 && i < section.Instructions.Count;
                 i += smearDir)
            {
                var instruction = section.Instructions[i];

                if (instruction.Name is
                    "call" or "jmp" or "je" or "jz"
                    or "jne" or "jnz" or "jg"
                    or "jge" or "jl" or "jle")
                {
                    trackedLocation = initialLocation;
                }

                var (destArg, srcArg) = (instruction.LeftArg.StripDeref(), instruction.RightArg.StripDeref());
                if (smearDir < 0)
                {
                    (destArg, srcArg) = (srcArg, destArg);
                }

                if (instruction.Name is "mov" or "lea" or "xchg" && srcArg == trackedLocation)
                {
                    trackedLocation = destArg;
                    var newType = GetTypeForLocation(function, trackedLocation);
                    if (newType != DeclType.Unknown)
                    {
                        declaration.DeclType = newType;
                        Console.WriteLine($"{function.Name}: {declarationDesc} is {newType} because {trackedLocation}");
                        changedSomething = true;
                        break;
                    }
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