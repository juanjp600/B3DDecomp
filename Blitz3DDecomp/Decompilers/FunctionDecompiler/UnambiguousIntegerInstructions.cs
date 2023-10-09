using System.Globalization;

namespace Blitz3DDecomp;

static class UnambiguousIntegerInstructions
{
    private static void Process(Function function, Function.Instruction[] section)
    {
        foreach (var instruction in section)
        {
            if (instruction.Name == "cmp" && instruction.RightArg != "0x0")
            {
                // cmp with a non-zero argument is almost definitely working with integers
                var location = instruction.LeftArg.StripDeref();
                if (!location.StartsWith("ebp")) { continue; }

                static BasicDeclaration markDeclarationAsInt(BasicDeclaration declaration)
                {
                    if (declaration.DeclType == DeclType.Unknown)
                    {
                        return declaration with { DeclType = DeclType.Int };
                    }
                    return declaration;
                }

                var offset = int.Parse(location[6..], NumberStyles.HexNumber) >> 2;
                if (location[3] == '+')
                {
                    // location is argument
                    int index = offset - 5;
                    if (index < 0 || index >= function.Arguments.Count) { continue; }
                    function.Arguments[index] = markDeclarationAsInt(function.Arguments[index]);
                }
                else if (location[3] == '-')
                {
                    // location is local variable
                    int index = offset - 1;
                    if (index < 0 || index >= function.LocalVariables.Count) { continue; }
                    function.LocalVariables[index] = markDeclarationAsInt(function.LocalVariables[index]);
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