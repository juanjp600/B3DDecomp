using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class BbObjTypeInference
    {
        private static void InferTypesForCall(Function function, Function.Instruction[] instructions, int callLocation)
        {
            var callInstruction = instructions[callLocation];
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return; }
            var calleeName = callInstruction.LeftArg[1..];
            var callee = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName || f.Name == calleeName[2..]);
            for (int i = 0; i < callee.Arguments.Count; i++)
            {
                var assignmentLocation = callParameterAssignmentIndices[i];
                var assignmentInstruction = instructions[assignmentLocation];
                if (assignmentInstruction.RightArg.StartsWith("@_t"))
                {
                    callInstruction.BbObjType = assignmentInstruction.RightArg[1..];
                    break;
                }
                var trackedLocation = assignmentInstruction.RightArg.StripDeref();
                for (int j = assignmentLocation - 1; j >= 0; j--)
                {
                    var instruction = instructions[j];
                    if (instruction.Name is "mov" or "xchg" && instruction.LeftArg.StripDeref() == trackedLocation)
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
        
        public static void Process(Function function)
        {
            foreach (var section in function.AssemblySections.Values)
            {
                for (int i=0;i<section.Length;i++)
                {
                    if (section[i].Name != "call") { continue; }
                    InferTypesForCall(function, section, i);
                }
            }
        }
    }
}