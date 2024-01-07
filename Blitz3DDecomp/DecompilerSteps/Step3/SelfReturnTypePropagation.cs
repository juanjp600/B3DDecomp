using B3DDecompUtils;
using B3DDecompUtils.Primitives;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class SelfReturnTypePropagation
{
    private static void ProcessSection(AssemblySection section)
    {
        for (int i = 1; i < section.Instructions.Length; i++)
        {
            var instruction = section.Instructions[i];

            if (instruction.Name != "jmp") { continue; }
            if (!instruction.DestArg.Contains($"_leave{section.Owner.CoreSymbolName}", StringComparison.OrdinalIgnoreCase)) { continue; }

            for (int j = i - 1; j >= 0; j--)
            {
                var prevInstruction = section.Instructions[j];
                if (prevInstruction.IsJumpOrCall)
                {
                    if (prevInstruction.Name != "call") { return; }

                    var callee = Function.GetFunctionByName(prevInstruction.DestArg);
                    if (callee.ReturnType == DeclType.Unknown) { return; }

                    section.Owner.ReturnType = callee.ReturnType;
                    Logger.WriteLine($"{section.Owner}: returns {section.Owner.ReturnType} because {prevInstruction}");
                    return;
                }

                var argsToCheck = new[] { prevInstruction.DestArg, prevInstruction.SrcArg1, prevInstruction.SrcArg2 };
                var matchingArgOption = argsToCheck.FirstOrNone(a => a.StripDeref().StartsWith("eax", StringComparison.OrdinalIgnoreCase));
                if (!matchingArgOption.TryUnwrap(out var matchingArg)) { continue; }

                var variable = section.Owner.InstructionArgumentToVariable(matchingArg);
                if (variable is null || variable.DeclType == DeclType.Unknown) { break; }

                section.Owner.ReturnType = variable.DeclType;
                Logger.WriteLine($"{section.Owner}: returns {section.Owner.ReturnType} because {prevInstruction}");
                return;
            }
        }
    }
    
    public static bool Process(Function function)
    {
        if (function.ReturnType != DeclType.Unknown) { return false; }
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(section);
            if (function.ReturnType != DeclType.Unknown) { return true; }
        }
        return false;
    }
}