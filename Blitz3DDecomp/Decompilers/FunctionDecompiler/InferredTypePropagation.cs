using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

static class InferredTypePropagation
{
    private static bool InferReturnTypeInSection(Function function, Function.Instruction[] instructions)
    {
        if (function.ReturnType != DeclType.Unknown) { return false; }

        var trackedLocation = "";
        for (int i = instructions.Length - 1; i >= 0; i--)
        {
            var instruction = instructions[i];
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

    private static bool HandleSubCall(Function function, DeclType argType, Function.Instruction[] instructions, int assignmentLocation)
    {
        if (argType == DeclType.Unknown) { return false; }

        var trackedLocation = instructions[assignmentLocation].LeftArg.StripDeref();
        for (int i = assignmentLocation; i >= 0; i--)
        {
            var instruction = instructions[i];
            if (instruction.Name is "mov" or "lea" or "xchg" &&
                instruction.LeftArg.StripDeref() == trackedLocation)
            {
                trackedLocation = instruction.RightArg.StripDeref();

                if (trackedLocation.StartsWith("ebp") && GetTypeForLocation(function, trackedLocation) == DeclType.Unknown)
                {
                    if (trackedLocation.StartsWith("ebp-0x"))
                    {
                        // This is a local
                        int varIndex = (int.Parse(trackedLocation[6..], NumberStyles.HexNumber) - 0x4) >> 2;
                        if (varIndex >= 0
                            && varIndex < function.LocalVariables.Count)
                        {
                            function.LocalVariables[varIndex] = function.LocalVariables[varIndex] with { DeclType = argType };
                        }
                    }
                    else if (trackedLocation.StartsWith("ebp+0x"))
                    {
                        // This is an argument
                        int paramIndex = (int.Parse(trackedLocation[6..], NumberStyles.HexNumber) - 0x14) >> 2;
                        if (paramIndex >= 0
                            && paramIndex < function.Arguments.Count)
                        {
                            function.Arguments[paramIndex] = function.Arguments[paramIndex] with { DeclType = argType };
                        }
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private static bool HandleSubCalls(Function function, Function.Instruction[] instructions)
    {
        for (int i = instructions.Length - 1; i >= 0; i--)
        {
            if (instructions[i].Name != "call") { continue; }
            var assignmentLocations = instructions[i].CallParameterAssignmentIndices ?? Array.Empty<int>();

            for (var argIndex = 0; argIndex < assignmentLocations.Length; argIndex++)
            {
                var callee = Function.AllFunctions.Find(f => f.Name == instructions[i].LeftArg[1..] || f.Name == instructions[i].LeftArg[3..]);
                var assignmentLocation = assignmentLocations[argIndex];
                HandleSubCall(function, callee.Arguments[argIndex].DeclType, instructions, assignmentLocation);
            }
        }

        return false;
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
                && paramIndex < function.Arguments.Count)
            {
                return function.Arguments[paramIndex].DeclType;
            }
        }

        return DeclType.Unknown;
    }

    private static bool SmearLocalsAndArgs(Function function, Function.Instruction[] instructions)
    {
        bool changedSomething = false;

        void smear(ref BasicDeclaration declaration, string initialLocation, string declarationDesc, int smearDir)
        {
            var trackedLocation = initialLocation;
            for (int i = smearDir > 0 ? 0 : instructions.Length - 1;
                 i >= 0 && i < instructions.Length;
                 i += smearDir)
            {
                var instruction = instructions[i];

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
                        declaration = declaration with { DeclType = newType };
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
            smear(ref variable, $"ebp-0x{(localIndex * 4) + 0x4:x1}", $"local {localIndex}", smearDir: -1);
            smear(ref variable, $"ebp-0x{(localIndex * 4) + 0x4:x1}", $"local {localIndex}", smearDir: 1);
            function.LocalVariables[localIndex] = variable;
        }
        
        for (int argIndex = 0; argIndex < function.Arguments.Count; argIndex++)
        {
            if (function.Arguments[argIndex].DeclType != DeclType.Unknown) { continue; }

            var variable = function.Arguments[argIndex];
            smear(ref variable, $"ebp+0x{(argIndex * 4) + 0x14:x1}", $"arg {argIndex}", smearDir: 1);
            smear(ref variable, $"ebp+0x{(argIndex * 4) + 0x14:x1}", $"arg {argIndex}", smearDir: -1);
            function.Arguments[argIndex] = variable;
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