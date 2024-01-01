using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class BbArrayAccess
{
    private static bool ProcessSection(Function function, Function.AssemblySection section)
    {
        bool changedSomething = false;

        bool isVarOfArrayType(Variable variable)
            => variable.DeclType.IsArrayType;

        var variablesOfArrayType
            = section.ReferencedVariables.Where(isVarOfArrayType)
                .ToArray();

        void handlePotentialImmediateDeref(
            Function.Instruction potentialImmediateDeref,
            string register,
            Function.Instruction instruction)
        {
            if (potentialImmediateDeref.Name == "mov"
                && potentialImmediateDeref.DestArg == register
                && potentialImmediateDeref.SrcArg1 == $"[{register}]")
            {
                Logger.WriteLine($"{function.Name}: accesses {instruction.SrcArg1}");
                instruction.SrcArg1 = $"[{instruction.SrcArg1}]";
                potentialImmediateDeref.Name = "nop";
                section.CleanupNop();
                changedSomething = true;
            }
            else if (potentialImmediateDeref.Name == "mov"
                     && potentialImmediateDeref.DestArg == $"[{register}]")
            {
                Logger.WriteLine(
                    $"{function.Name}: writes {potentialImmediateDeref.SrcArg1} into {instruction.SrcArg1}");
                instruction.DestArg = $"[{instruction.SrcArg1}]";
                instruction.SrcArg1 = potentialImmediateDeref.SrcArg1;
                potentialImmediateDeref.Name = "nop";
                section.CleanupNop();
                changedSomething = true;
            }
            else
            {
                Logger.WriteLine(
                    $"{function.Name}: copies pointer to {instruction.SrcArg1} into {instruction.DestArg}");
                //if (!potentialImmediateDeref.LeftArg.Contains("esp")) { Debugger.Break(); }
            }
        }
        
        foreach (var variable in variablesOfArrayType)
        {
            var initialLocation = variable.ToInstructionArg();
            var tracker = new LocationTracker(trackDirection: 1, initialLocation: initialLocation);

            for (int i = 0; i < section.Instructions.Count; i++)
            {
                var instruction = section.Instructions[i];

                if (i >= 2 && instruction.Name == "add" && instruction.SrcArg1 == variable.ToInstructionArg())
                {
                    var register = instruction.DestArg;
                    var prevInstruction = section.Instructions[i - 1];
                    if (prevInstruction is { Name: "shl", SrcArg1: "0x2" } && prevInstruction.DestArg == register)
                    {
                        prevInstruction.Name = "nop";
                        instruction.Name = "mov";
                        instruction.DestArg = register;
                        instruction.SrcArg1 = $"{variable.Name}[{register}]";

                        section.CleanupNop();
                        i--;
                        changedSomething = true;
                    }
                    else if (prevInstruction.Name == "mov"
                             && prevInstruction.DestArg == register
                             && prevInstruction.SrcArg1.StartsWith("0x"))
                    {
                        var index = prevInstruction.SrcArg1.HexToUint32() >> 2;
                        prevInstruction.Name = "nop";
                        instruction.Name = "mov";
                        instruction.DestArg = register;
                        instruction.SrcArg1 = $"{variable.Name}[{index}]";

                        section.CleanupNop();
                        i--;
                        changedSomething = true;
                    }
                    else
                    {
                        Debugger.Break();
                    }

                    if (i+1 >= section.Instructions.Count) { continue; }
                    var potentialImmediateDeref = section.Instructions[i + 1];
                    handlePotentialImmediateDeref(
                        potentialImmediateDeref,
                        register,
                        instruction);
                }
                else
                {
                    tracker.Location = initialLocation;
                    if (!tracker.ProcessInstruction(instruction)) { continue; }

                    if (instruction.Name != "mov") { continue; }

                    if (!tracker.Location.IsRegister())
                    {
                        Debugger.Break();
                    }

                    if (i+2 >= section.Instructions.Count) { continue; }

                    var register = tracker.Location;
                    var arrayDerefInstruction = section.Instructions[i + 1];
                    if (arrayDerefInstruction.Name == "mov"
                        && arrayDerefInstruction.DestArg == register
                        && arrayDerefInstruction.SrcArg1 == $"[{register}]")
                    {
                        for (int j = i + 2; j < section.Instructions.Count && j < i + 40; j++)
                        {
                            var elementOffsetInstruction = section.Instructions[j];
                            if (elementOffsetInstruction.Name != "add") { continue; }
    
                            if (elementOffsetInstruction.DestArg == register)
                            {
                                var arrayIndex = elementOffsetInstruction.SrcArg1.HexToUint32() >> 2;
                                instruction.SrcArg1 += $"[{arrayIndex}]";
                                arrayDerefInstruction.Name = "nop";
                                elementOffsetInstruction.Name = "nop";
                                section.CleanupNop();
                                changedSomething = true;

                                var potentialImmediateDeref = section.Instructions[j - 1];
                                handlePotentialImmediateDeref(
                                    potentialImmediateDeref,
                                    register,
                                    instruction);
                                break;
                            }
                            else if (elementOffsetInstruction.SrcArg1 == register)
                            {
                                var arrayIndex = $"{elementOffsetInstruction.DestArg}>>2";
                                arrayDerefInstruction.DestArg = elementOffsetInstruction.DestArg;
                                arrayDerefInstruction.SrcArg1 = $"{instruction.SrcArg1}[{arrayIndex}]";
                                elementOffsetInstruction.Name = "nop";
                                section.CleanupNop();
                                changedSomething = true;

                                var potentialImmediateDeref = section.Instructions[j - 1];
                                handlePotentialImmediateDeref(
                                    potentialImmediateDeref,
                                    arrayDerefInstruction.DestArg,
                                    arrayDerefInstruction);

                                break;
                            }
                        }
                    }
                }
            }
        }

        return changedSomething;
    }

    public static bool Process(Function function)
    {
        bool changedSomething = false;
        foreach (var kvp in function.AssemblySections)
        {
            if (kvp.Key.Contains("_leave")) { continue; }
            var section = kvp.Value;
            changedSomething |= ProcessSection(function, section);
        }
        return changedSomething;
    }
}