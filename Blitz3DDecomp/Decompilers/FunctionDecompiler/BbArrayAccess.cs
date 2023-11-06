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
                && potentialImmediateDeref.LeftArg == register
                && potentialImmediateDeref.RightArg == $"[{register}]")
            {
                Logger.WriteLine($"{function.Name}: accesses {instruction.RightArg}");
                instruction.RightArg = $"[{instruction.RightArg}]";
                potentialImmediateDeref.Name = "nop";
                section.CleanupNop();
                changedSomething = true;
            }
            else if (potentialImmediateDeref.Name == "mov"
                     && potentialImmediateDeref.LeftArg == $"[{register}]")
            {
                Logger.WriteLine(
                    $"{function.Name}: writes {potentialImmediateDeref.RightArg} into {instruction.RightArg}");
                instruction.LeftArg = $"[{instruction.RightArg}]";
                instruction.RightArg = potentialImmediateDeref.RightArg;
                potentialImmediateDeref.Name = "nop";
                section.CleanupNop();
                changedSomething = true;
            }
            else
            {
                Logger.WriteLine(
                    $"{function.Name}: copies pointer to {instruction.RightArg} into {instruction.LeftArg}");
                //if (!potentialImmediateDeref.LeftArg.Contains("esp")) { Debugger.Break(); }
            }
        }
        
        foreach (var variable in variablesOfArrayType)
        {
            var initialLocation = variable.ToInstructionArg();
            var tracker = new LocationTracker(trackDirection: 1, initialLocation: initialLocation);

            for (int i = 0; i < section.Instructions.Count - 4; i++)
            {
                var instruction = section.Instructions[i];

                if (i >= 2 && instruction.Name == "add" && instruction.RightArg == variable.ToInstructionArg())
                {
                    var register = instruction.LeftArg;
                    var prevInstruction = section.Instructions[i - 1];
                    if (prevInstruction is { Name: "shl", RightArg: "0x2" } && prevInstruction.LeftArg == register)
                    {
                        prevInstruction.Name = "nop";
                        instruction.Name = "mov";
                        instruction.LeftArg = register;
                        instruction.RightArg = $"{variable.Name}[{register}]";

                        section.CleanupNop();
                        i--;
                    }
                    else if (prevInstruction.Name == "mov"
                             && prevInstruction.LeftArg == register
                             && prevInstruction.RightArg.StartsWith("0x"))
                    {
                        var index = prevInstruction.RightArg.HexToUint32() >> 2;
                        prevInstruction.Name = "nop";
                        instruction.Name = "mov";
                        instruction.LeftArg = register;
                        instruction.RightArg = $"{variable.Name}[{index}]";

                        section.CleanupNop();
                        i--;
                    }
                    else
                    {
                        Debugger.Break();
                    }

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

                    var register = tracker.Location;
                    var arrayDerefInstruction = section.Instructions[i + 1];
                    var elementOffsetInstruction = section.Instructions[i + 2];
                    if (arrayDerefInstruction.Name == "mov"
                        && arrayDerefInstruction.LeftArg == register
                        && arrayDerefInstruction.RightArg == $"[{register}]"
                        && elementOffsetInstruction.Name == "add")
                    {
                        if (elementOffsetInstruction.LeftArg == register)
                        {
                            var arrayIndex = elementOffsetInstruction.RightArg.HexToUint32() >> 2;
                            instruction.RightArg += $"[{arrayIndex}]";
                            section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                            section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                            section.CleanupNop();
                            changedSomething = true;

                            var potentialImmediateDeref = section.Instructions[i + 1];
                            handlePotentialImmediateDeref(
                                potentialImmediateDeref,
                                register,
                                instruction);
                        }
                        else if (elementOffsetInstruction.RightArg == register)
                        {
                            var arrayIndex = $"{elementOffsetInstruction.LeftArg}>>2";
                            arrayDerefInstruction.LeftArg = elementOffsetInstruction.LeftArg;
                            arrayDerefInstruction.RightArg = $"{instruction.RightArg}[{arrayIndex}]";
                            section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                            section.CleanupNop();
                            changedSomething = true;

                            var potentialImmediateDeref = section.Instructions[i + 2];
                            handlePotentialImmediateDeref(
                                potentialImmediateDeref,
                                arrayDerefInstruction.LeftArg,
                                arrayDerefInstruction);
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                    else
                    {
                        //Debugger.Break();
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