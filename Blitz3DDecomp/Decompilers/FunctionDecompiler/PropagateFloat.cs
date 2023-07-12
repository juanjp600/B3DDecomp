using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class PropagateFloat
    {
        private static bool InferTypesForCall(Function function, Function.Instruction[] instructions, int callLocation)
        {
            var callInstruction = instructions[callLocation];
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return false; }
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
                        if (instruction.Name.Contains("_markAsFloat"))
                        {
                            if (typeAtTop is null)
                            {
                                if (instruction.Name.StartsWith("fistp"))
                                {
                                    // Convert from float to int and pop -> argument is getting an int
                                    callee.Arguments[i] = callee.Arguments[i] with { DeclType = DeclType.Int };
                                    Console.WriteLine($"{function.Name}: {calleeName} arg {i} is int");
                                }
                                else
                                {
                                    if (calleeName.Contains("bbStrLoad"))
                                    {
                                        Debugger.Break();
                                    }
                                    callee.Arguments[i] = callee.Arguments[i] with { DeclType = DeclType.Float };
                                    Console.WriteLine($"{function.Name}: {calleeName} arg {i} is float");
                                }
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
                    }

                    if (instruction.Name is "mov" or "lea" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                    }

                    if (instruction.Name is "call")
                    {
                        if (trackedLocation == "eax")
                        {
                            var calleeName2 = instruction.LeftArg[1..];
                            var callee2Function = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName2 || f.Name == calleeName2[2..]);
                            if (callee2Function.ReturnType == DeclType.Unknown)
                            {
                                if (typeAtTop != null)
                                {
                                    callee2Function.ReturnType = typeAtTop.Value;
                                    Console.WriteLine($"{function.Name}: {calleeName2} returns {(typeAtTop == DeclType.Int ? "int" : "float")}");
                                }
                            }
                            else
                            {
                                callee.Arguments[i] = callee.Arguments[i] with { DeclType = callee2Function.ReturnType };
                                Console.WriteLine($"{function.Name}: {calleeName} arg {i} is {callee2Function.ReturnType} because {calleeName2}");
                            }
                        }
                        break;
                    }

                    if (instruction.Name is
                        "jmp" or "je" or "jz"
                        or "jne" or "jnz" or "jg" or "jge"
                        or "jl" or "jle")
                    {
                        break;
                    }
                }
            }
            return false;
        }
        
        public static bool Process(Function function)
        {
            bool retVal = false;
            foreach (var section in function.AssemblySections.Values)
            {
                for (int i=0;i<section.Length;i++)
                {
                    if (section[i].Name != "call") { continue; }
                    retVal |= InferTypesForCall(function, section, i);
                }
            }
            return retVal;
        }
    }
}