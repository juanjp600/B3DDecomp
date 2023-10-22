using System.Diagnostics;

namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class BasicFloatPropagation
    {
        private static bool CheckInstructionForMarkAsFloat(Function function, Variable declaration, string declarationDesc, Function.Instruction instruction, int smearDir, ref DeclType? typeBeyondInstruction)
        {
            if (!instruction.Name.Contains("_markAsFloat")) { return false; }
            
            bool changedSomething = false;
            // fild: Load an int, convert to float and push float to stack -> src is an int, dest is a float
            // fistp: Pop float from stack, convert to int and store an int -> dest is an int, src is a float

            var intToFltName = smearDir > 0 ? "fild" : "fistp";
            var fltToIntName = smearDir > 0 ? "fistp" : "fild";

            if (typeBeyondInstruction is null && declaration.DeclType == DeclType.Unknown)
            {
                if (instruction.Name.StartsWith(intToFltName))
                {
                    declaration.DeclType = DeclType.Int;
                    Console.WriteLine($"{function.Name}: {declarationDesc} is int because {instruction}");
                }
                else
                {
                    declaration.DeclType = DeclType.Float;
                    Console.WriteLine($"{function.Name}: {declarationDesc} is float because {instruction}");
                }
                changedSomething = true;
            }

            if (instruction.Name.StartsWith(fltToIntName))
            {
                typeBeyondInstruction = DeclType.Int;
            }
            else
            {
                typeBeyondInstruction = DeclType.Float;
            }
            return changedSomething;
        }

        private static bool HandlePropagationForReturnType(Function function, Variable declaration, string declarationDesc, Function.Instruction instruction, DeclType? typeAtTop)
        {
            bool changedSomething = false;
            var calleeFunction = Function.GetFunctionWithName(instruction.LeftArg.StripDeref());
            if (calleeFunction.ReturnType == DeclType.Unknown)
            {
                if (typeAtTop != null)
                {
                    if (calleeFunction.IsBuiltIn) { return false; }
                    calleeFunction.ReturnType = typeAtTop.Value;
                    Console.WriteLine($"{function.Name}: {calleeFunction.Name} returns {(typeAtTop == DeclType.Int ? "int" : "float")} because {declarationDesc}");
                    changedSomething = true;
                }
            }
            else if (declaration.DeclType == DeclType.Unknown)
            {
                declaration.DeclType = calleeFunction.ReturnType;
                Console.WriteLine($"{function.Name}: {declarationDesc} is {calleeFunction.ReturnType} because {calleeFunction.Name}");
                changedSomething = true;
            }
            return changedSomething;
        }

        private static bool InferTypesForCallInstruction(Function function, Function.AssemblySection section, int callLocation)
        {
            var callInstruction = section.Instructions[callLocation];
            if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return false; }

            bool changedSomething = false;

            var callee = Function.GetFunctionWithName(callInstruction.LeftArg);
            if (callee.IsBuiltIn) { return false; }

            for (int i = 0; i < callee.Parameters.Count; i++)
            {
                if (callee.Parameters[i].DeclType != DeclType.Unknown) { continue; }
                var assignmentLocation = callParameterAssignmentIndices[i];
                var assignmentInstruction = section.Instructions[assignmentLocation];
                var locationTracker = new LocationTracker(trackDirection: -1, initialLocation: assignmentInstruction.RightArg.StripDeref());
                DeclType? typeAtTop = null;
                for (int j = assignmentLocation - 1; j >= 0; j--)
                {
                    var instruction = section.Instructions[j];

                    if (instruction.LeftArg.StripDeref() == locationTracker.Location)
                    {
                        changedSomething |= CheckInstructionForMarkAsFloat(function, callee.Parameters[i], $"{callee.Name} arg {i}", instruction, smearDir: -1, ref typeAtTop);

                        if (instruction.Name == "movzx")
                        {
                            Console.WriteLine($"{callee.Name} arg {i} is int because {instruction}");
                            callee.Parameters[i].DeclType = DeclType.Int;
                            changedSomething = true;
                            break;
                        }
                    }

                    locationTracker.ProcessInstruction(instruction);

                    if (instruction.Name is "call")
                    {
                        if (locationTracker.Location == "eax")
                        {
                            changedSomething |= HandlePropagationForReturnType(function, callee.Parameters[i], $"{callee.Name} arg {i}", instruction, typeAtTop);
                        }
                        break;
                    }

                    if (instruction.IsJumpOrCall)
                    {
                        break;
                    }
                }
            }
            return changedSomething;
        }

        private static bool InferTypesForVariables(Function function, Function.AssemblySection section)
        {
            bool changedSomething = false;

            void smear(Variable declaration, string initialLocation, int smearDir)
            {
                DeclType? typeBeyondInstruction = null;
                var locationTracker = new LocationTracker(trackDirection: smearDir, initialLocation);
                for (int j = smearDir > 0 ? 0 : section.Instructions.Count - 1;
                     j >= 0 && j < section.Instructions.Count;
                     j += smearDir)
                {
                    var instruction = section.Instructions[j];

                    if (instruction.LeftArg.StripDeref() == locationTracker.Location)
                    {
                        changedSomething |= CheckInstructionForMarkAsFloat(function, declaration, declaration.Name, instruction, smearDir: smearDir, ref typeBeyondInstruction);

                        if (smearDir < 0 && instruction.Name == "movzx" && declaration.DeclType == DeclType.Unknown)
                        {
                            Console.WriteLine($"{declaration.Name} is int because {instruction}");
                            declaration.DeclType = DeclType.Int;
                            changedSomething = true;
                            locationTracker.Location = initialLocation;
                        }
                    }

                    locationTracker.ProcessInstruction(instruction);

                    // Return type propagation can only happen in an upwards smear
                    // because a downwards smear would reach a call before anything
                    // about its return type can be known
                    if (smearDir < 0 && instruction.Name is "call")
                    {
                        if (locationTracker.Location == "eax")
                        {
                            changedSomething |= HandlePropagationForReturnType(function, declaration, declaration.Name, instruction, typeBeyondInstruction);
                        }
                    }

                    if (instruction.IsJumpOrCall)
                    {
                        locationTracker.Location = initialLocation;
                    }
                }
            }

            void smearBothWays(Variable variable, string initialLocation)
            {
                smear(variable, initialLocation, smearDir: -1);
                smear(variable, initialLocation, smearDir: 1);
            }

            for (int i = 0; i < function.LocalVariables.Count; i++)
            {
                smearBothWays(function.LocalVariables[i], $"ebp-0x{(i * 4) + 0x4:x1}");
            }
            
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                smearBothWays(function.Parameters[i], $"ebp+0x{((i * 4) + 0x14):x1}");
            }

            foreach (var global in section.ReferencedGlobals)
            {
                smearBothWays(global, $"@_v{global.Name}");
            }

            return changedSomething;
        }
        
        public static bool Process(Function function)
        {
            if (function.IsBuiltIn) { return false; }

            bool changedSomething = false;

            while (true)
            {
                bool changedSomethingNow = false;
                foreach (var section in function.AssemblySections.Values)
                {
                    for (int i = 0; i < section.Instructions.Count; i++)
                    {
                        if (section.Instructions[i].Name != "call") { continue; }
                        changedSomethingNow |= InferTypesForCallInstruction(function, section, i);
                    }

                    changedSomethingNow |= InferTypesForVariables(function, section);
                }
                if (!changedSomethingNow) { break; }

                changedSomething = true;
            }

            return changedSomething;
        }
    }
}