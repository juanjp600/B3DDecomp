﻿using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class BbObjMemberAccess
{
    private static bool ProcessSectionMavless(Function function, Function.AssemblySection section)
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

            var trackedLocations = new List<int>();

            for (int i = 0; i < section.Instructions.Count; i++)
            {
                var instruction = section.Instructions[i];

                if (tracker.ProcessInstruction(instruction))
                {
                    trackedLocations.Add(i);
                }
                else if (!tracker.Location.ContainsRegister("esp"))
                {
                    continue;
                }

                if (instruction.Name == "sub" && instruction.DestArg.Contains("esp"))
                {
                    trackedLocations.Clear();
                    tracker.Location = initialLocation;
                    continue;
                }

                if (!instruction.IsJumpOrCall) { continue; }

                if (instruction.Name != "call" || !instruction.DestArg.Contains("bbObjLoad"))
                {
                    trackedLocations.Clear();
                    tracker.Location = initialLocation;
                    continue;
                }

                var fieldAccessInstructionDistance = section.Instructions
                    .Skip(i).ToList()
                    .FindIndex(instr => instr.Name == "call" && instr.DestArg.Contains("bbFieldPtrAdd"));
                var fieldAccessInstructionIndex = i + fieldAccessInstructionDistance;
                var instructionsToCleanUp = section.Instructions.Skip(i + 1).Take(fieldAccessInstructionDistance).ToArray();

                var offsetInstruction = instructionsToCleanUp.First(instr => instr.Name == "mov" && instr.SrcArg1.StartsWith("0x"));
                var fieldIndex = int.Parse(offsetInstruction.SrcArg1[2..], NumberStyles.HexNumber) >> 2;
                var customType = CustomType.GetTypeMatchingDeclType(variable.DeclType);
                var field = customType.Fields[fieldIndex];

                instruction.Name = "mov";
                instruction.DestArg = "eax";
                instruction.SrcArg1 = $"{variable.Name}\\{field.Name}";

                foreach (var instr in instructionsToCleanUp)
                {
                    instr.Name = "nop";
                }
                foreach (var index in trackedLocations)
                {
                    section.Instructions[index].Name = "nop";
                }

                section.CleanupNop();

                trackedLocations.Clear();
                tracker.Location = initialLocation;
            }
        }

        return changedSomething;
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

                if (tracker.Location.IsRegister())
                {
                    var register = tracker.Location;
                    var objDerefInstruction = section.Instructions[i + 1];
                    var memberAccessInstruction = section.Instructions[i + 2];
                    if (objDerefInstruction.Name == "mov"
                        && objDerefInstruction.DestArg == register
                        && objDerefInstruction.SrcArg1 == $"[{register}]"
                        && memberAccessInstruction.Name == "add"
                        && memberAccessInstruction.DestArg == register)
                    {
                        var fieldIndex = int.Parse(memberAccessInstruction.SrcArg1[2..], NumberStyles.HexNumber) >> 2;
                        var customType = CustomType.GetTypeMatchingDeclType(variable.DeclType);
                        if (customType is null)
                        {
                            throw new Exception($"Custom type of name {variable.DeclType.Suffix} was not loaded from symbols");
                        }

                        var field = customType.Fields[fieldIndex];
                        instruction.SrcArg1 = $"{variable.Name}\\{field.Name}";
                        section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                        section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                        if (!field.DeclType.IsArrayType)
                        {
                            var derefFieldInstruction = section.Instructions[i + 3];
                            if (derefFieldInstruction.Name == "mov"
                                && derefFieldInstruction.DestArg == register
                                && derefFieldInstruction.SrcArg1 == $"[{register}]")
                            {
                                // Read the value of the field and store in the same register
                                instruction.SrcArg1 = $"[{variable.Name}\\{field.Name}]";
                                section.Instructions[i + 3] = new Function.Instruction(name: "nop");
                                Logger.WriteLine($"{function.Name}: dereferences {variable}\\{field} into {register}");
                            }
                            else if (derefFieldInstruction.Name == "mov"
                                     && derefFieldInstruction.SrcArg1 == register)
                            {
                                // Take the pointer to the field and store it somewhere else
                                Logger.WriteLine($"{function.Name}: stores pointer to {variable}\\{field} into {register} because {derefFieldInstruction}");
                                // There's no action to be taken here, a LocationTracker should be able to handle this
                            }
                            else if (derefFieldInstruction.Name == "mov"
                                     && derefFieldInstruction.DestArg == $"[{register}]")
                            {
                                // Write a value into the field
                                instruction.DestArg = $"[{variable.Name}\\{field.Name}]";
                                instruction.SrcArg1 = derefFieldInstruction.SrcArg1;
                                section.Instructions[i + 3] = new Function.Instruction(name: "nop");
                                Logger.WriteLine($"{function.Name}: writes {derefFieldInstruction.SrcArg1} into {variable}\\{field}");
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
        if (section.Instructions.Any(i => i.DestArg.Contains("bbObjLoad")))
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