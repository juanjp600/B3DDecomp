using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step4;

static class GuessFloatsFromConstants
{
    private static void GuessForVariable(Function function, Variable variable, uint constant, string reason)
    {
        if (variable.DeclType != DeclType.Unknown) { return; }
        if (constant == 0)
        {
            // Int 0 and float 0 have the same representation
            // so we can't disambiguate here
            return;
        }

        // If the exponent of the float is all zeroes or mostly ones,
        // or if the mantissa is a small non-zero number,
        // this is probably an int because writing that kind of float in source code is hard
        var potentialExponent = (constant & 0x7f80_0000) >> 23;
        var potentialMantissa = constant & 0x007f_ffff;
        var guessedType = DeclType.Float;
        if (potentialExponent is < 96 or > 190) { return; }
        if (potentialMantissa is (> 0) and (< 512)) { return; }
        variable.DeclType = DeclType.Float;
        Logger.WriteLine($"{function}: {variable.Name} is probably {variable.DeclType} because {reason}");
    }

    private static void ProcessSection(AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            uint constant;
            switch (instruction.Name)
            {
                case "mov" or "xchg" or "lea":
                    var variable = section.Owner.InstructionArgumentToVariable(instruction.DestArg)
                                   ?? section.Owner.InstructionArgumentToVariable(instruction.SrcArg1)
                                   ?? section.Owner.InstructionArgumentToVariable(instruction.SrcArg2);
                    if (variable?.DeclType != DeclType.Unknown) { continue; }

                    if (!instruction.DestArg.TryHexToUint32(out constant)
                        && !instruction.SrcArg1.TryHexToUint32(out constant)
                        && !instruction.SrcArg2.TryHexToUint32(out constant))
                    {
                        continue;
                    }

                    GuessForVariable(section.Owner, variable, constant, instruction.ToString());
                    break;
                case "call":
                    var callee = Function.GetFunctionByName(instruction.DestArg);
                    if (instruction.CallParameterAssignmentIndices is not { } assignmentIndices) { continue; }

                    for (int i = 0; i < assignmentIndices.Length; i++)
                    {
                        var parameter = callee.Parameters[i];
                        var assignmentInstruction = section.Instructions[assignmentIndices[i]];

                        if (!assignmentInstruction.SrcArg1.TryHexToUint32(out constant))
                        {
                            continue;
                        }

                        GuessForVariable(callee, parameter, constant, $"({assignmentInstruction}) from {section.Name}");
                    }
                    break;
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(section);
        }
    }
}