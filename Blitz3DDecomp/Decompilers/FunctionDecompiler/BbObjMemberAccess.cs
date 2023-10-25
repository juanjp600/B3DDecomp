using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class BbObjMemberAccess
{
    private static bool ProcessSectionMavless(Function function, Function.AssemblySection section)
    {
        return false;
    }

    private static bool ProcessSectionVanilla(Function function, Function.AssemblySection section)
    {
        bool changedSomething = false;

        bool isVarOfCustomType(Variable variable)
            => variable.DeclType.IsCustomType
                && !variable.DeclType.IsArrayType;

        var variablesOfCustomType
            = section.ReferencedVariables.Where(isVarOfCustomType)
                .ToArray();

        foreach (var variable in variablesOfCustomType)
        {
            var initialLocation = variable.ToInstructionArg().StripDeref();
            var tracker = new LocationTracker(trackDirection: 1, initialLocation: initialLocation);

            for (int i = 0; i < section.Instructions.Count - 4; i++)
            {
                var instruction = section.Instructions[i];

                if (!tracker.ProcessInstruction(instruction)) { continue; }

                //if (instruction.Name != "mov") { Debugger.Break(); }
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
                        instruction.RightArg = $"{variable.Name}\\{field.Name}";
                        section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                        section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                        if (!field.DeclType.IsArrayType)
                        {
                            var derefFieldInstruction = section.Instructions[i + 3];
                            if (derefFieldInstruction.Name == "mov"
                                && derefFieldInstruction.LeftArg == register
                                && derefFieldInstruction.RightArg == $"[{register}]")
                            {
                                // Read the value of the field and store in the same register
                                instruction.RightArg = $"[{variable.Name}\\{field.Name}]";
                                section.Instructions[i + 3] = new Function.Instruction(name: "nop");
                                Logger.WriteLine($"{function.Name}: dereferences {variable}\\{field} into {register}");
                            }
                            else if (derefFieldInstruction.Name == "mov"
                                     && derefFieldInstruction.RightArg == register)
                            {
                                // Take the pointer to the field and store it somewhere else
                                Logger.WriteLine($"{function.Name}: stores pointer to {variable}\\{field} into {register} because {derefFieldInstruction}");
                                // There's no action to be taken here, a LocationTracker should be able to handle this
                            }
                            else if (derefFieldInstruction.Name == "mov"
                                     && derefFieldInstruction.LeftArg == $"[{register}]")
                            {
                                // Write a value into the field
                                instruction.LeftArg = $"[{variable.Name}\\{field.Name}]";
                                instruction.RightArg = derefFieldInstruction.RightArg;
                                section.Instructions[i + 3] = new Function.Instruction(name: "nop");
                                Logger.WriteLine($"{function.Name}: writes {derefFieldInstruction.RightArg} into {variable}\\{field}");
                            }
                            else
                            {
                                Logger.WriteLine($"{function.Name}: stores pointer to {variable}\\{field} into {register} because {derefFieldInstruction}");
                                //Debugger.Break();
                            }
                        }
                        else
                        {
                            Logger.WriteLine($"{function.Name}: stores pointer to {variable}\\{field} into {register} because array field");
                        }

                        section.CleanupNop(); changedSomething = true;
                    }
                }
                else
                {
                    Debugger.Break();
                }

                tracker.Location = initialLocation;
            }
        }

        return changedSomething;
    }

    private static bool ProcessSection(Function function, Function.AssemblySection section)
    {
        if (section.Instructions.Any(i => i.LeftArg.Contains("bbObjLoad")))
        {
            return ProcessSectionMavless(function, section);
        }
        else
        {
            return ProcessSectionVanilla(function, section);
        }
    }

    public static bool Process(Function function)
    {
        bool changedSomething = false;
        foreach (var kvp in function.AssemblySections)
        {
            var section = kvp.Value;
            changedSomething |= ProcessSection(function, section);
        }
        return changedSomething;
    }
}