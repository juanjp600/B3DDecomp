using System.Globalization;

namespace Blitz3DDecomp;

static class UnambiguousIntegerInstructions
{
    private static void Process(Function function, Function.AssemblySection section)
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
                if (!location.StartsWith("ebp")) { continue; }

                static void markDeclarationAsInt(Variable declaration)
                {
                    if (declaration.DeclType == DeclType.Unknown)
                    {
                        declaration.DeclType = DeclType.Int;
                    }
                }

                var offset = int.Parse(location[6..], NumberStyles.HexNumber) >> 2;
                if (location[3] == '+')
                {
                    // location is argument
                    int index = offset - 5;
                    if (index < 0 || index >= function.Parameters.Count) { continue; }
                    markDeclarationAsInt(function.Parameters[index]);
                }
                else if (location[3] == '-')
                {
                    // location is local variable
                    int index = offset - 1;
                    if (index < 0 || index >= function.LocalVariables.Count) { continue; }
                    markDeclarationAsInt(function.LocalVariables[index]);
                }
            }
        }
    }
    
    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            Process(function, section);
        }
    }
}