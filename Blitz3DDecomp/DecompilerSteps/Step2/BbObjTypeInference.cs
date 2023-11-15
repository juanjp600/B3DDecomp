using System.Diagnostics;
using B3DDecompUtils;

namespace Blitz3DDecomp;

static class BbObjTypeInference
{
    private static void InferTypesForCall(Function function, Function.AssemblySection section, int callLocation)
    {
        var callInstruction = section.Instructions[callLocation];
        if (!string.IsNullOrEmpty(callInstruction.BbObjType)) { return; }
        if (callInstruction.CallParameterAssignmentIndices is not { Length: >0 } callParameterAssignmentIndices) { return; }
        var calleeName = callInstruction.LeftArg[1..];
        var callee = Function.GetFunctionByName(calleeName);
        for (int i = 0; i < callee.Parameters.Count; i++)
        {
            var assignmentLocation = callParameterAssignmentIndices[i];
            var assignmentInstruction = section.Instructions[assignmentLocation];
            if (assignmentInstruction.RightArg.StartsWith("@_t"))
            {
                callInstruction.BbObjType = assignmentInstruction.RightArg[1..];
                break;
            }
            var locationTracker = new LocationTracker(trackDirection: -1, initialLocation: assignmentInstruction.RightArg.StripDeref());
            for (int j = assignmentLocation - 1; j >= 0; j--)
            {
                var instruction = section.Instructions[j];
                if (locationTracker.ProcessInstruction(instruction))
                {
                    if (instruction.RightArg.StartsWith("@_t"))
                    {
                        callInstruction.BbObjType = instruction.RightArg[1..];
                        break;
                    }
                }

                if (instruction.IsJumpOrCall)
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
            Logger.WriteLine($"{function.Name}: {calleeName} -> {callInstruction.BbObjType}");
        }
    }

    private static bool InferTypeForVariable(
        Function function,
        Function.AssemblySection section,
        Variable variable,
        string initialLocation)
    {
        if (variable.DeclType != DeclType.Unknown) { return false; }

        var locationTracker = new LocationTracker(trackDirection: -1, initialLocation: initialLocation);
        for (int j = section.Instructions.Count - 1; j >= 0; j--)
        {
            var instruction = section.Instructions[j];

            locationTracker.ProcessInstruction(instruction);

            if (instruction.Name is "call")
            {
                bool instrIsConstructor = false;
                string? bbObjType = instruction.BbObjType;

                if (locationTracker.Location == "eax"
                    && (instruction.LeftArg.Contains("bbObjNew") || instruction.LeftArg.Contains("bbObjFirst") || instruction.LeftArg.Contains("bbObjLast")))
                {
                    instrIsConstructor = true;
                }
                else if (instruction.LeftArg.Contains("bbObjEachFirst") || instruction.LeftArg.Contains("bbObjStore"))
                {
                    if (instruction.CallParameterAssignmentIndices is null)
                    {
                        throw new Exception(
                            $"{section.Name}:{j}: expected parameter assignment indices, but they were not defined");
                    }

                    if (string.IsNullOrEmpty(bbObjType))
                    {
                        var secondParamAssignmentLocation = instruction.CallParameterAssignmentIndices[1];
                        var secondParamTracker = new LocationTracker(
                            trackDirection: -1,
                            initialLocation: section.Instructions[secondParamAssignmentLocation].LeftArg,
                            preserveDeref: true);
                        for (int k = secondParamAssignmentLocation; k >= 0; k--)
                        {
                            var instruction2 = section.Instructions[k];

                            if (instruction2.IsJumpOrCall)
                            {
                                if (!string.IsNullOrEmpty(instruction2.BbObjType))
                                {
                                    if (secondParamTracker.Location == "eax")
                                    {
                                        bbObjType = instruction2.BbObjType;
                                    }
                                }
                                else
                                {
                                    if (instruction2.Name == "call" && secondParamTracker.Location == "eax")
                                    {
                                        var calledFunction = Function.GetFunctionByName(instruction2.LeftArg);
                                        if (calledFunction is { ReturnType.IsCustomType: true })
                                        {
                                            bbObjType = "_t"+calledFunction.ReturnType.Suffix[1..];
                                        }
                                    }
                                }
                                break;
                            }

                            if (secondParamTracker.ProcessInstruction(instruction2)
                                && instruction.LeftArg.Contains("bbObjStore"))
                            {
                                var variableForSecondParam = function.InstructionArgumentToVariable(secondParamTracker.Location);
                                if (variableForSecondParam is { DeclType.IsCustomType: true })
                                {
                                    if (variableForSecondParam.DeclType.IsArrayType) { Debugger.Break(); }
                                    bbObjType = "_t" + variableForSecondParam.DeclType.Suffix[1..];
                                    break;
                                }
                            }

                            if (instruction.Name == "mov" && instruction.LeftArg == secondParamTracker.Location)
                            {
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(bbObjType))
                    {
                        var firstParamAssignmentLocation = instruction.CallParameterAssignmentIndices[0];
                        var firstParamTracker = new LocationTracker(
                            trackDirection: -1,
                            initialLocation: section.Instructions[firstParamAssignmentLocation].LeftArg,
                            preserveDeref: true);
                        for (int k = firstParamAssignmentLocation; k >= 0; k--)
                        {
                            var instruction2 = section.Instructions[k];

                            if (instruction2.IsJumpOrCall)
                            {
                                break;
                            }

                            firstParamTracker.ProcessInstruction(instruction2);

                            var trackedVariable =
                                function.InstructionArgumentToVariable(firstParamTracker.Location);
                            if (trackedVariable == null) { continue; }

                            if (function.Name.Contains("findpath") &&
                                trackedVariable is Function.Parameter { Index: 0 })
                            {
                                Debugger.Break();
                            }

                            if (trackedVariable == variable)
                            {
                                instrIsConstructor = true;
                            }
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(bbObjType) && instrIsConstructor)
                {
                    variable.DeclType = new DeclType("."+bbObjType[2..]);
                    Logger.WriteLine($"{function.Name}: {variable.Name} is {variable.DeclType} because {instruction.LeftArg}");
                    return true;
                }
            }

            if (instruction.IsJumpOrCall)
            {
                locationTracker.Location = initialLocation;
            }
        }

        return false;
    }
    
    private static bool InferTypesForLocals(Function function, Function.AssemblySection section)
    {
        bool changedSomething = false;
        var variables = section
            .ReferencedVariables
            .Where(g => g.DeclType == DeclType.Unknown)
            .ToArray();
        foreach (var variable in variables)
        {
            changedSomething |= InferTypeForVariable(
                function,
                section,
                variable,
                variable.ToInstructionArg().StripDeref());
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