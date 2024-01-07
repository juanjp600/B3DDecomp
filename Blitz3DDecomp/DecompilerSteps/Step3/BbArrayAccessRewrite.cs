using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class BbArrayAccessRewrite
{
    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        for (int i = 0; i < section.Instructions.Length; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "add"
                || !instruction.DestArg[..3].IsRegister())
            {
                continue;
            }

            var srcVar1 = section.Owner.InstructionArgumentToVariable(instruction.SrcArg1);
            var srcVar2 = section.Owner.InstructionArgumentToVariable(instruction.SrcArg2);

            var array = srcVar2 is { DeclType.IsArrayType : true }
                ? srcVar2
                : srcVar1 is { DeclType.IsArrayType : true }
                    ? srcVar1
                    : null;
            if (array is null) { continue; }

            string arrayIndex = "";
            void tryExtractArrayIndex(string instructionArg, Variable? variable)
            {
                if (!string.IsNullOrEmpty(arrayIndex)) { return; }

                if (variable?.DeclType == DeclType.Int || variable?.DeclType == DeclType.Unknown)
                {
                    arrayIndex = $"{variable.Name}>>2";
                    if (variable.DeclType == DeclType.Unknown)
                    {
                        variable.DeclType = DeclType.Int;
                        Logger.WriteLine($"{section.Owner}: {variable.Name} is {DeclType.Int} because {instruction}");
                    }
                }
                else if (instructionArg.TryHexToUint32(out var constantIndex))
                {
                    arrayIndex = (constantIndex >> 2).ToString();
                }
            }
            tryExtractArrayIndex(instruction.SrcArg1, srcVar1);
            tryExtractArrayIndex(instruction.SrcArg2, srcVar2);

            if (string.IsNullOrEmpty(arrayIndex)) { continue; }

            instruction.Name = "mov";
            instruction.SrcArg1 =  $"{array.Name}[{arrayIndex}]";
            instruction.SrcArg2 = "";
            Logger.WriteLine($"{section.Owner}: {array.Name} access at {section.Name}:{i}");
            somethingChanged = true;
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