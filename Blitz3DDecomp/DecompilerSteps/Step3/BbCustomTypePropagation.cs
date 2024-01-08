using System.Diagnostics;
using B3DDecompUtils;
using B3DDecompUtils.Primitives;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class BbCustomTypePropagation
{
    private static DeclType InstructionArgToCustomType(AssemblySection section, string arg)
    {
        var variable = section.Owner.InstructionArgumentToVariable(arg);
        if (variable is { DeclType: { IsCustomType: true, IsArrayType: false } })
        {
            return variable.DeclType;
        }

        if (arg.StartsWith("@_t", StringComparison.OrdinalIgnoreCase))
        {
            return new DeclType("." + arg[3..]);
        }

        return DeclType.Unknown;
    }

    private static bool HandleCustomTypeAssignmentToRegister(Function function, Instruction instruction)
    {
        if (instruction.Name != "mov") { return false; }
        if (!instruction.SrcArg1.StartsWith("@_t", StringComparison.OrdinalIgnoreCase)) { return false; }

        var variable = function.InstructionArgumentToVariable(instruction.DestArg);
        if (variable?.DeclType != DeclType.Unknown) { return false; }

        variable.DeclType = new DeclType("." + instruction.SrcArg1[3..]);
        Logger.WriteLine($"{function}: {variable.Name} is {variable.DeclType} because {instruction}");
        return true;
    }

    private static bool HandleReturningCall(AssemblySection section, Instruction instruction)
    {
        if (instruction.Name != "call") { return false; }
        if (!instruction.DestArg.Contains("_builtIn__", StringComparison.OrdinalIgnoreCase)) { return false; }

        bool calleeNameEndsWith(string substr)
        {
            return instruction.DestArg.EndsWith(substr, StringComparison.OrdinalIgnoreCase);
        }

        if (!calleeNameEndsWith("bbObjNew")
            && !calleeNameEndsWith("bbObjNext")
            && !calleeNameEndsWith("bbObjPrev")
            && !calleeNameEndsWith("bbObjFirst")
            && !calleeNameEndsWith("bbObjLast")
            && !calleeNameEndsWith("bbObjFromHandle")
            && !calleeNameEndsWith("bbObjPrev")
            && !calleeNameEndsWith("bbObjLoad"))
        {
            return false;
        }

        if (instruction.ReturnOutputVar is { } returnOutput
            && returnOutput.DeclType == DeclType.Unknown
            && instruction.CallParameterAssignmentIndices is { } assignmentIndices)
        {
            for (int i = 0; i < assignmentIndices.Length; i++)
            {
                var assignmentInstruction = section.Instructions[assignmentIndices[i]];
                var argType = InstructionArgToCustomType(section, assignmentInstruction.SrcArg1);
                if (argType is { IsCustomType: true, IsArrayType: false })
                {
                    returnOutput.DeclType = argType;
                    Logger.WriteLine($"{section.Owner}: {returnOutput.Name} is {returnOutput.DeclType} because ({assignmentInstruction}) & ({instruction})");
                    return true;
                }
            }
        }
        return false;
    }

    private static bool HandleStoreCall(AssemblySection section, Instruction instruction)
    {
        if (instruction.Name != "call") { return false; }
        if (!instruction.DestArg.Contains("_builtIn__", StringComparison.OrdinalIgnoreCase)) { return false; }

        bool calleeNameEndsWith(string substr)
        {
            return instruction.DestArg.EndsWith(substr, StringComparison.OrdinalIgnoreCase);
        }

        if (!calleeNameEndsWith("bbObjStore")
            && !calleeNameEndsWith("bbObjCompare")
            && !calleeNameEndsWith("bbObjInsBefore")
            && !calleeNameEndsWith("bbObjInsAfter")
            && !calleeNameEndsWith("bbObjEachFirst")
            && !calleeNameEndsWith("bbObjEachFirst2"))
        {
            return false;
        }

        if (instruction.CallParameterAssignmentIndices is { } assignmentIndices)
        {
            var assignmentInstructions = assignmentIndices.Select(i => section.Instructions[i]).ToArray();
            var types = assignmentInstructions
                .Select(i => InstructionArgToCustomType(section, i.SrcArg1))
                .ToArray();
            var typeOption = types.FirstOrNone(t => t != DeclType.Unknown);
            if (!typeOption.TryUnwrap(out var type)) { return false; }

            var variables = assignmentInstructions
                .Select(i => section.Owner.InstructionArgumentToVariable(i.SrcArg1))
                .OfType<Variable>()
                .ToArray();
            foreach (var variable in variables)
            {
                if (variable.DeclType != DeclType.Unknown) { continue; }

                variable.DeclType = type;
                Logger.WriteLine($"{section.Owner}: {variable.Name} is {type} because {instruction}");
            }
        }

        return false;
    }

    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        foreach (var instruction in section.Instructions)
        {
            somethingChanged |= HandleCustomTypeAssignmentToRegister(section.Owner, instruction);
            somethingChanged |= HandleReturningCall(section, instruction);
            somethingChanged |= HandleStoreCall(section, instruction);
        }
        return somethingChanged;
    }

    public static bool Process(Function function)
    {
        bool somethingChanged = false;
        foreach (var section in function.AssemblySections)
        {
            somethingChanged |= ProcessSection(section);
        }
        return somethingChanged;
    }
}