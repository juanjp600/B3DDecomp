using System.Diagnostics;
using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step3_Obsolete;

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
            Instruction potentialImmediateDeref,
            string register,
            Instruction instruction)
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
            var restartTracker = new LocationTracker(trackDirection: 1, initialLocation: initialLocation);
            var reverseTracker = new LocationTracker(trackDirection: -1, initialLocation: "????");
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
                    reverseTracker.Location = tracker.Location;
                    restartTracker.Location = initialLocation;
                    if (restartTracker.ProcessInstruction(instruction))
                    {
                        tracker.Location = restartTracker.Location;
                    }
                    else if (!tracker.ProcessInstruction(instruction))
                    {
                        if (reverseTracker.ProcessInstruction(instruction))
                        {
                            tracker.Location = initialLocation;
                            continue;
                        }
                    }

                    if (!tracker.Location.IsRegister()) { continue; }
                    if (instruction.Name != "mov") { continue; }

                    for (int j = i + 1; j < section.Instructions.Count && j < i + 4; j++)
                    {
                        var register = tracker.Location;
                        var arrayDerefInstruction = section.Instructions[j];
                        if (arrayDerefInstruction.Name == "mov"
                            && arrayDerefInstruction.DestArg == register
                            && arrayDerefInstruction.SrcArg1 == $"[{register}]")
                        {
                            for (int k = j + 1; k < section.Instructions.Count && k < j + 4; k++)
                            {
                                var elementOffsetInstruction = section.Instructions[k];
                                if (elementOffsetInstruction.Name != "add") { continue; }
        
                                if (elementOffsetInstruction.DestArg == register)
                                {
                                    var arrayIndex = elementOffsetInstruction.SrcArg1.HexToUint32() >> 2;
                                    instruction.SrcArg1 = $"{variable.Name}[{arrayIndex}]";
                                    arrayDerefInstruction.Name = "nop";
                                    elementOffsetInstruction.Name = "nop";
                                    var potentialImmediateDeref = section.Instructions[k + 1];
                                    section.CleanupNop();
                                    changedSomething = true;

                                    handlePotentialImmediateDeref(
                                        potentialImmediateDeref,
                                        register,
                                        instruction);
                                    break;
                                }
                                else if (elementOffsetInstruction.SrcArg1 == register)
                                {
                                    if (section.Name == "_17335_ffillroom"
                                        && variable.Name.Contains("arg0\\Field29[1]", StringComparison.Ordinal))
                                    {
                                        Debugger.Break();
                                    }
                                    
                                    var arrayIndex = $"{elementOffsetInstruction.DestArg}>>2";
                                    arrayDerefInstruction.DestArg = elementOffsetInstruction.DestArg;
                                    arrayDerefInstruction.SrcArg1 = $"{variable.Name}[{arrayIndex}]";
                                    elementOffsetInstruction.Name = "nop";
                                    var potentialImmediateDeref = section.Instructions[k + 1];
                                    section.CleanupNop();
                                    changedSomething = true;

                                    handlePotentialImmediateDeref(
                                        potentialImmediateDeref,
                                        arrayDerefInstruction.DestArg,
                                        arrayDerefInstruction);

                                    break;
                                }
                            }
                            break;
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