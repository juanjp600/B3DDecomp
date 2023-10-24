using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

static class BbArrayAccess
{
    private static void ProcessSection(Function function, Function.AssemblySection section)
    {
        bool isVarOfArrayType(Variable variable)
            => variable.DeclType.IsArrayType;

        var variablesOfArrayType
            = section.ReferencedVariables.Where(isVarOfArrayType)
                .ToArray();

        foreach (var variable in variablesOfArrayType)
        {
            var initialLocation = variable.ToInstructionArg();
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
                        && objDerefInstruction.LeftArg == register
                        && objDerefInstruction.RightArg == $"[{register}]"
                        && memberAccessInstruction.Name == "add")
                    {
                        if (memberAccessInstruction.LeftArg == register)
                        {
                            var arrayIndex = int.Parse(memberAccessInstruction.RightArg[2..], NumberStyles.HexNumber) >> 2;
                            instruction.RightArg += $"[{arrayIndex}]";
                            section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                            section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                            section.CleanupNop();

                            var potentialImmediateDeref = section.Instructions[i + 1];
                            if (potentialImmediateDeref.Name == "mov"
                                && potentialImmediateDeref.LeftArg == register
                                && potentialImmediateDeref.RightArg == $"[{register}]")
                            {
                                Console.WriteLine($"{function.Name}: accesses {instruction.RightArg}");
                                instruction.RightArg = $"[{instruction.RightArg}]";
                                section.Instructions[i + 1] = new Function.Instruction(name: "nop");
                                section.CleanupNop();
                            }
                            else
                            {
                                Console.WriteLine($"{function.Name}: copies pointer to {instruction.RightArg} into {instruction.LeftArg}");
                                //Debugger.Break();
                            }
                        }
                        else if (memberAccessInstruction.RightArg == register)
                        {
                            var arrayIndex = $"{memberAccessInstruction.LeftArg}>>2";
                            objDerefInstruction.LeftArg = memberAccessInstruction.LeftArg;
                            objDerefInstruction.RightArg = $"{instruction.RightArg}[{arrayIndex}]";
                            section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                            section.CleanupNop();

                            var potentialImmediateDeref = section.Instructions[i + 2];
                            if (potentialImmediateDeref.Name == "mov"
                                && potentialImmediateDeref.LeftArg == objDerefInstruction.LeftArg
                                && potentialImmediateDeref.RightArg == $"[{objDerefInstruction.LeftArg}]")
                            {
                                Console.WriteLine($"{function.Name}: accesses {objDerefInstruction.RightArg}");
                                objDerefInstruction.RightArg = $"[{objDerefInstruction.RightArg}]";
                                section.Instructions[i + 2] = new Function.Instruction(name: "nop");
                                section.CleanupNop();
                            }
                            else
                            {
                                Console.WriteLine($"{function.Name}: copies pointer to {objDerefInstruction.RightArg} into {objDerefInstruction.LeftArg}");
                                //Debugger.Break();
                            }

                        }
                        else
                        {
                            Debugger.Break();
                        }

                        section.CleanupNop();
                    }
                    else
                    {
                        //Debugger.Break();
                    }
                }

                tracker.Location = initialLocation;
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections)
        {
            if (kvp.Key.Contains("_leave")) { continue; }
            var section = kvp.Value;
            ProcessSection(function, section);
        }
    }
}