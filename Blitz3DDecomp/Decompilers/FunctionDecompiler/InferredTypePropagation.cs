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

    private static bool SmearLocalsAndArgs(Function function, Function.Instruction[] instructions)
    {
        bool changedSomething = false;

        DeclType getTypeForLocation(string location)
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
                    var newType = getTypeForLocation(trackedLocation);
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
            changedSomething |= InferReturnTypeInSection(function, section);
            changedSomething |= SmearLocalsAndArgs(function, section);
        }

        return changedSomething;
    }
}