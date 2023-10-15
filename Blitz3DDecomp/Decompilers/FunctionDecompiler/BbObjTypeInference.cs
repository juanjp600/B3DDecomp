using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class BbObjTypeInference
    {
        private static void InferTypesForCall(Function function, Function.AssemblySection section, int callLocation)
        {
            var callInstruction = section.Instructions[callLocation];
            if (!string.IsNullOrEmpty(callInstruction.BbObjType)) { return; }
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return; }
            var calleeName = callInstruction.LeftArg[1..];
            var callee = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName || f.Name == calleeName[2..]);
            for (int i = 0; i < callee.Parameters.Count; i++)
            {
                var assignmentLocation = callParameterAssignmentIndices[i];
                var assignmentInstruction = section.Instructions[assignmentLocation];
                if (assignmentInstruction.RightArg.StartsWith("@_t"))
                {
                    callInstruction.BbObjType = assignmentInstruction.RightArg[1..];
                    break;
                }
                var trackedLocation = assignmentInstruction.RightArg.StripDeref();
                for (int j = assignmentLocation - 1; j >= 0; j--)
                {
                    var instruction = section.Instructions[j];
                    if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                        if (instruction.RightArg.StartsWith("@_t"))
                        {
                            callInstruction.BbObjType = instruction.RightArg[1..];
                            break;
                        }
                    }

                    if (instruction.Name is
                        "call" or "jmp" or "je" or "jz"
                        or "jne" or "jnz" or "jg" or "jge"
                        or "jl" or "jle")
                    {
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(callInstruction.BbObjType))
                {
                    break;
                }
            }

            if (!string.IsNullOrEmpty(callInstruction.BbObjType))
            {
                if (!calleeName.StartsWith("_builtIn__bbObj"))
                {
                    Debugger.Break();
                }
                Console.WriteLine($"{function.Name}: {calleeName} -> {callInstruction.BbObjType}");
            }
        }

        private static bool InferTypesForLocals(Function function, Function.AssemblySection section)
        {
            bool changedSomething = false;
            for (int i = 0; i < function.LocalVariables.Count; i++)
            {
                if (function.LocalVariables[i].DeclType != DeclType.Unknown) { continue; }

                DeclType? typeAtTop = null;
                var trackedLocation = $"ebp-0x{(i * 4) + 0x4:x1}";
                for (int j = section.Instructions.Count - 1; j >= 0; j--)
                {
                    var instruction = section.Instructions[j];
                    if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                    }

                    if (changedSomething) { break; }

                    if (instruction.Name is "call")
                    {
                        bool instrIsConstructor = false;
                        string? bbObjType = instruction.BbObjType;

                        if (trackedLocation == "eax"
                            && (instruction.LeftArg.Contains("bbObjNew") || instruction.LeftArg.Contains("bbObjFirst") || instruction.LeftArg.Contains("bbObjLast")))
                        {
                            instrIsConstructor = true;
                        }
                        else if (instruction.LeftArg.Contains("bbObjEachFirst") || instruction.LeftArg.Contains("bbObjStore"))
                        {
                            if (string.IsNullOrEmpty(bbObjType))
                            {
                                var secondParamAssignmentLocation = instruction.CallParameterAssignmentIndices[1];
                                var tl3 = section.Instructions[secondParamAssignmentLocation].LeftArg.StripDeref();
                                for (int k = secondParamAssignmentLocation; k >= 0; k--)
                                {
                                    var instruction2 = section.Instructions[k];

                                    if (instruction2.Name is
                                        "call" or "jmp" or "je" or "jz"
                                        or "jne" or "jnz" or "jg"
                                        or "jge" or "jl" or "jle")
                                    {
                                        if (!string.IsNullOrEmpty(instruction2.BbObjType))
                                        {
                                            if (tl3 == "eax")
                                            {
                                                bbObjType = instruction2.BbObjType;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    if (instruction2.Name is "mov" or "lea" or "xchg" &&
                                        instruction2.LeftArg.StripDeref() == tl3)
                                    {
                                        tl3 = instruction2.RightArg.StripDeref();
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(bbObjType))
                            {
                                var firstParamAssignmentLocation = instruction.CallParameterAssignmentIndices[0];
                                var tl2 = section.Instructions[firstParamAssignmentLocation].LeftArg.StripDeref();
                                for (int k = firstParamAssignmentLocation; k >= 0; k--)
                                {
                                    var instruction2 = section.Instructions[k];

                                    if (instruction2.Name is
                                        "call" or "jmp" or "je" or "jz"
                                        or "jne" or "jnz" or "jg"
                                        or "jge" or "jl" or "jle")
                                    {
                                        break;
                                    }

                                    if (instruction2.Name is "mov" or "lea" or "xchg" &&
                                        instruction2.LeftArg.StripDeref() == tl2)
                                    {
                                        tl2 = instruction2.RightArg.StripDeref();
                                    }

                                    if (tl2.Contains("ebp-"))
                                    {
                                        if (tl2 == trackedLocation)
                                        {
                                            instrIsConstructor = true;
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(bbObjType) && instrIsConstructor)
                        {
                            function.LocalVariables[i].DeclType = new DeclType("."+bbObjType[2..]);
                            Console.WriteLine($"{function.Name}: local {i} is {function.LocalVariables[i].DeclType} because {instruction.LeftArg}");
                            changedSomething = true;
                            break;
                        }
                    }

                    if (instruction.Name is
                        "call" or "jmp" or "je" or "jz"
                        or "jne" or "jnz" or "jg"
                        or "jge" or "jl" or "jle")
                    {
                        trackedLocation = $"ebp-0x{(i * 4) + 0x4:x1}";
                    }
                }
            }

            return changedSomething;
        }

        public static bool Process(Function function)
        {
            bool changedSomething = false;

            while (true)
            {
                bool changedSomethingNow = false;
                foreach (var section in function.AssemblySections.Values)
                {
                    for (int i = 0; i < section.Instructions.Count; i++)
                    {
                        if (section.Instructions[i].Name != "call") { continue; }
                        InferTypesForCall(function, section, i);
                    }
                    changedSomethingNow |= InferTypesForLocals(function, section);
                }
                if (!changedSomethingNow) { break; }

                changedSomething = true;
            }
            return changedSomething;
        }
    }
}