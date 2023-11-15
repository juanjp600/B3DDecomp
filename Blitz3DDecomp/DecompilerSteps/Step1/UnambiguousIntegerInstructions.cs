using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class UnambiguousIntegerInstructions
{
    private static void ProcessCmp(Function function, Function.AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            if (instruction.Name != "cmp") { continue; }

            var (arg0, arg1) = (instruction.LeftArg.StripDeref(), instruction.RightArg.StripDeref());

            var variable = function.InstructionArgumentToVariable(arg0) ?? function.InstructionArgumentToVariable(arg1);

            if (variable != null && variable.DeclType == DeclType.Unknown
                && arg0 != "0x0" && arg1 != "0x0")
            {
                variable.DeclType = DeclType.Int;
                Logger.WriteLine($"{function.Name}: {variable.Name} is int because {instruction}");
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