using System.Globalization;

namespace Blitz3DDecomp;

static class UnambiguousIntegerInstructions
{
    private static void ProcessCmp(Function function, Function.AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            var (arg0, arg1) = (instruction.LeftArg.StripDeref(), instruction.RightArg.StripDeref());
            if (arg1.StartsWith("ebp"))
            {
                (arg0, arg1) = (arg1, arg0);
            }
            if (instruction.Name == "cmp" && arg1 != "0x0")
            {
                // cmp with a non-zero argument is almost definitely working with integers
                var location = arg0;

                var variable = function.InstructionArgumentToVariable(location);
                if (variable != null && variable.DeclType == DeclType.Unknown)
                {
                    variable.DeclType = DeclType.Int;
                }
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessCmp(function, section);
        }
    }
}