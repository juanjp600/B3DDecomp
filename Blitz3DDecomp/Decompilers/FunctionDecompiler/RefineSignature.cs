using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class RefineSignature
    {
        private static bool InferTypesForCall(Function.Instruction[] instructions, int callLocation)
        {
            var callInstruction = instructions[callLocation];
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return false; }
            var calleeName = callInstruction.LeftArg[1..];
            var callee = Function.AllFunctions.FirstOrDefault(f => f.Name == calleeName || f.Name == calleeName[2..]);
            for (int i = 0; i < callee.Arguments.Count; i++)
            {
                var assignmentLocation = callParameterAssignmentIndices[i];
                var guessedType = DeclType.Unknown;
                var assignmentInstruction = instructions[assignmentLocation];
                var trackedLocation = assignmentInstruction.RightArg.StripDeref();
                for (int j = assignmentLocation - 1; j >= 0; j--)
                {
                    var instruction = instructions[j];
                    if (instruction.Name == "mov" && instruction.LeftArg.StripDeref() == trackedLocation)
                    {
                        trackedLocation = instruction.RightArg.StripDeref();
                    }

                    if (instruction.Name == "call")
                    {
                        if (trackedLocation == "eax")
                        {
                            Debugger.Break();
                        }
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
                    retVal |= InferTypesForCall(section, i);
                }
            }
            return retVal;
        }
    }
}