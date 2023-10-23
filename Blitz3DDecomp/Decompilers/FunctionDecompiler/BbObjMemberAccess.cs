using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

static class BbObjMemberAccess
{
    private static void ProcessSectionMavless(Function function, Function.AssemblySection section)
    {
        
    }

    private static void ProcessSectionVanilla(Function function, Function.AssemblySection section)
    {
        bool isVarOfCustomType(Variable variable)
            => variable.DeclType.IsCustomType;

        var variablesOfCustomType
            = function.LocalVariables.Where(isVarOfCustomType).Cast<Variable>()
                .Concat(function.Parameters.Where(isVarOfCustomType))
                .Concat(section.ReferencedGlobals.Where(isVarOfCustomType))
                .ToArray();

        foreach (var variable in variablesOfCustomType)
        {
            var initialLocation = variable.ToInstructionArg();
            var tracker = new LocationTracker(trackDirection: 1, initialLocation: initialLocation);

            for (int i = 0; i < section.Instructions.Count - 2; i++)
            {
                var instruction = section.Instructions[i];

                if (!tracker.ProcessInstruction(instruction)) { continue; }

                if (tracker.Location.IsRegister())
                {
                    var register = tracker.Location;
                    var objDerefInstruction = section.Instructions[i + 1];
                    var memberAccessInstruction = section.Instructions[i + 2];
                    if (objDerefInstruction.Name == "mov"
                        && objDerefInstruction.LeftArg == register
                        && objDerefInstruction.RightArg == $"[{register}]"
                        && memberAccessInstruction.Name == "add"
                        && memberAccessInstruction.LeftArg == register)
                    {
                        var fieldIndex = int.Parse(memberAccessInstruction.RightArg[2..], NumberStyles.HexNumber) >> 2;
                        var customType = CustomType.GetTypeMatchingDeclType(variable.DeclType);
                        var field = customType.Fields[fieldIndex];
                        Console.WriteLine($"{function.Name}: accesses {variable}\\{field}");
                        instruction.RightArg = $"{variable.Name}\\{field.Name}";
                        section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                        section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                    }
                }

                tracker.Location = initialLocation;
            }
        }
    }

    private static void ProcessSection(Function function, Function.AssemblySection section)
    {
        if (section.Instructions.Any(i => i.LeftArg.Contains("bbObjLoad")))
        {
            ProcessSectionMavless(function, section);
        }
        else
        {
            ProcessSectionVanilla(function, section);
        }
    }

    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections)
        {
            var section = kvp.Value;
            ProcessSection(function, section);
        }
    }
}