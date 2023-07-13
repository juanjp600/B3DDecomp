using System.Globalization;

namespace Blitz3DDecomp;

static class ReturnTypeInference
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

    public static bool Process(Function function)
    {
        bool changedSomething = false;

        foreach (var section in function.AssemblySections.Values)
        {
            changedSomething |= InferReturnTypeInSection(function, section);
        }

        return changedSomething;
    }
}