using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class CalleeArgumentTypePropagation
{
    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        foreach (var instruction in section.Instructions)
        {
            if (instruction.Name != "call") { continue; }
            if (instruction.CallParameterAssignmentIndices is not { } assignmentIndices) { continue; }

            var callee = Function.GetFunctionByName(instruction.DestArg);

            for (int i = 0; i < assignmentIndices.Length; i++)
            {
                var assignmentInstruction = section.Instructions[assignmentIndices[i]];
                if (section.Owner.InstructionArgumentToVariable(assignmentInstruction.SrcArg1) is { } variable)
                {
                    if (variable.DeclType != DeclType.Unknown)
                    {
                        if (callee.IsBuiltIn) { continue; }
                        if (callee.Parameters[i].DeclType != DeclType.Unknown) { continue; }

                        callee.Parameters[i].DeclType = variable.DeclType;
                        somethingChanged = true;
                        Logger.WriteLine($"{section.Owner}: callee {callee.Name} arg {i} is {callee.Parameters[i].DeclType} because {variable}");
                    }
                    else
                    {
                        if (callee.Parameters[i].DeclType == DeclType.Unknown) { continue; }

                        variable.DeclType = callee.Parameters[i].DeclType;
                        somethingChanged = true;
                        Logger.WriteLine($"{section.Owner}: {variable.Name} is {variable.DeclType} because {callee.Name} arg {i}");
                    }
                }
            }
        }
        return somethingChanged;
    }

    public static bool Process(Function function)
    {
        bool somethingChanged = false;
        foreach (var section in function.AssemblySections.Values)
        {
            somethingChanged |= ProcessSection(section);
        }
        return somethingChanged;
    }
}