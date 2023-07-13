using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class BasicFloatPropagation
    {
        private static bool CheckInstructionForMarkAsFloat(Function function, ref BasicDeclaration declaration, string declarationDesc, Function.Instruction instruction, ref DeclType? typeAtTop)
        {
            bool changedSomething = false;
            if (instruction.Name.Contains("_markAsFloat"))
            {
                if (typeAtTop is null)
                {
                    if (instruction.Name.StartsWith("fistp"))
                    {
                        // Convert from float to int and pop -> argument is getting an int
                        declaration = declaration with { DeclType = DeclType.Int };
                        Console.WriteLine($"{function.Name}: {declarationDesc} is int");
                    }
                    else
                    {
                        if (function.Name.Contains("bbStrLoad") || function.Name.Contains("bbFieldPtrAdd"))
                        {
                            Debugger.Break();
                        }
                        declaration = declaration with { DeclType = DeclType.Float };
                        Console.WriteLine($"{function.Name}: {declarationDesc} is float");
                    }
                    changedSomething = true;
                }

                if (instruction.Name.StartsWith("fild"))
                {
                    // Load an int, convert to float and push float to stack -> type at top is an int
                    typeAtTop = DeclType.Int;
                }
                else
                {
                    typeAtTop = DeclType.Float;
                }
            }
            return changedSomething;
        }

        private static bool HandlePropagationForReturnType(Function function, ref BasicDeclaration declaration, string declarationDesc, Function.Instruction instruction, DeclType? typeAtTop)
        {
            bool changedSomething = false;
            var calleeName2 = instruction.LeftArg[1..];
            var callee2Function = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName2 || f.Name == calleeName2[2..]);
            if (callee2Function.ReturnType == DeclType.Unknown)
            {
                if (typeAtTop != null)
                {
                    if (calleeName2.Contains("bbFieldPtrAdd")) { return false; }
                    callee2Function.ReturnType = typeAtTop.Value;
                    Console.WriteLine($"{function.Name}: {calleeName2} returns {(typeAtTop == DeclType.Int ? "int" : "float")}");
                    changedSomething = true;
                }
            }
            else
            {
                declaration = declaration with { DeclType = callee2Function.ReturnType };
                Console.WriteLine($"{function.Name}: {declarationDesc} is {callee2Function.ReturnType} because {calleeName2}");
                changedSomething = true;
            }
            return changedSomething;
        }

        private static bool InferTypesForCall(Function function, Function.Instruction[] instructions, int callLocation)
        {
            var callInstruction = instructions[callLocation];
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return false; }

            bool changedSomething = false;
            
            var calleeName = callInstruction.LeftArg[1..];
            var callee = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName || f.Name == calleeName[2..]);
            for (int i = 0; i < callee.Arguments.Count; i++)
            {
                if (callee.Arguments[i].DeclType != DeclType.Unknown) { continue; }
                var assignmentLocation = callParameterAssignmentIndices[i];
                var assignmentInstruction = instructions[assignmentLocation];
                var trackedLocation = assignmentInstruction.RightArg.StripDeref();
                DeclType? typeAtTop = null;
                for (int j = assignmentLocation - 1; j >= 0; j--)
                {
                    var instruction = instructions[j];
                    if (instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        var arg = callee.Arguments[i];
                        changedSomething |= CheckInstructionForMarkAsFloat(function, ref arg, $"{calleeName} arg {i}", instruction, ref typeAtTop);
                        callee.Arguments[i] = arg;
                    }

                    if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                    }

                    if (instruction.Name is "call")
                    {
                        if (trackedLocation == "eax")
                        {
                            var arg = callee.Arguments[i];
                            changedSomething |= HandlePropagationForReturnType(function, ref arg, $"{calleeName} arg {i}", instruction, typeAtTop);
                            callee.Arguments[i] = arg;
                        }
                        break;
                    }

                    if (instruction.Name is
                        "jmp" or "je" or "jz"
                        or "jne" or "jnz" or "jg"
                        or "jge" or "jl" or "jle")
                    {
                        break;
                    }
                }
            }
            return changedSomething;
        }

        private static bool InferTypesForLocals(Function function, Function.Instruction[] instructions)
        {
            bool changedSomething = false;
            for (int i = 0; i < function.LocalVariables.Count; i++)
            {
                if (function.LocalVariables[i].DeclType != DeclType.Unknown) { continue; }

                DeclType? typeAtTop = null;
                var trackedLocation = $"ebp-0x{((i + 1) * 4):x1}";
                for (int j = instructions.Length - 1; j >= 0; j--)
                {
                    var instruction = instructions[j];
                    if (instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        var variable = function.LocalVariables[i];
                        changedSomething |= CheckInstructionForMarkAsFloat(function, ref variable, $"local {i}", instruction, ref typeAtTop);
                        function.LocalVariables[i] = variable;
                    }

                    if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                    }

                    if (changedSomething) { break; }

                    if (instruction.Name is "call")
                    {
                        if (trackedLocation == "eax")
                        {
                            var variable = function.LocalVariables[i];
                            changedSomething |= HandlePropagationForReturnType(function, ref variable, $"local {i}", instruction, typeAtTop);
                            function.LocalVariables[i] = variable;
                        }
                        break;
                    }

                    if (instruction.Name is
                        "jmp" or "je" or "jz"
                        or "jne" or "jnz" or "jg"
                        or "jge" or "jl" or "jle")
                    {
                        trackedLocation = $"ebp-0x{((i + 1) * 4):x1}";
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
                    for (int i = 0; i < section.Length; i++)
                    {
                        if (section[i].Name != "call")
                        {
                            continue;
                        }

                        changedSomethingNow |= InferTypesForCall(function, section, i);
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