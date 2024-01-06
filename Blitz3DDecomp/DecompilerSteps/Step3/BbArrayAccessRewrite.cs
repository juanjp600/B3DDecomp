using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class BbArrayAccessRewrite
{
    private static bool ProcessSection(Function.AssemblySection section)
    {
        bool somethingChanged = false;
        for (int i = 0; i < section.Instructions.Count; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "add"
                || !instruction.DestArg[..3].IsRegister())
            {
                continue;
            }

            if (section.Owner.InstructionArgumentToVariable(instruction.SrcArg2) is not { } array
                || !array.DeclType.IsArrayType)
            {
                continue;
            }

            instruction.Name = "mov";
            instruction.SrcArg1 =  $"{array.Name}[{instruction.SrcArg1}>>2]";
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