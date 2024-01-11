using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step2;

static class HandleFloatInstructions
{
    private static void ProcessSection(AssemblySection section)
    {
        for (var i = 1; i < section.Instructions.Length - 1; i++)
        {
            void handlePotentialFloatReturn()
            {
                if (section.Owner.ReturnType == DeclType.Unknown && i < section.Instructions.Length - 2)
                {
                    var potentialLeave = section.Instructions[i + 2];
                    if (potentialLeave.Name is "jmp"
                        && potentialLeave.DestArg.EndsWith("_leave_f"+section.Owner.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        section.Owner.ReturnType = DeclType.Float;
                    }
                }
            }

            var prevInstruction = section.Instructions[i - 1];
            var instruction = section.Instructions[i];
            var nextInstruction = section.Instructions[i + 1];

            if (prevInstruction.Name is "push" && nextInstruction.Name is "pop")
            {
                var prevVariable = section.Owner.InstructionArgumentToVariable(prevInstruction.DestArg);
                var nextVariable = section.Owner.InstructionArgumentToVariable(nextInstruction.DestArg);

                var oldPrevType = prevVariable?.DeclType;
                var oldNextType = nextVariable?.DeclType;

                switch (instruction.Name)
                {
                    case "fld":
                        if (prevVariable?.DeclType == DeclType.Unknown) { prevVariable.DeclType = DeclType.Float; }
                        if (nextVariable?.DeclType == DeclType.Unknown)
                        {
                            nextVariable.DeclType = DeclType.Float;
                            handlePotentialFloatReturn();
                        }
                        break;
                    case "fild":
                        if (prevVariable?.DeclType == DeclType.Unknown) { prevVariable.DeclType = DeclType.Int; }
                        if (nextVariable?.DeclType == DeclType.Unknown)
                        {
                            nextVariable.DeclType = DeclType.Float;
                            handlePotentialFloatReturn();
                        }
                        break;
                    case "fstp":
                        if (nextVariable?.DeclType == DeclType.Unknown) { nextVariable.DeclType = DeclType.Float; }
                        break;
                    case "fistp":
                        if (nextVariable?.DeclType == DeclType.Unknown) { nextVariable.DeclType = DeclType.Int; }
                        break;
                }

                if (oldPrevType == DeclType.Unknown && prevVariable != null && prevVariable.DeclType != DeclType.Unknown)
                {
                    prevVariable.Trace = prevVariable.Trace.Append($"{section.Owner}: {prevVariable.Name} is {prevVariable.DeclType} because {instruction}");
                }
                if (oldNextType == DeclType.Unknown && nextVariable != null && nextVariable.DeclType != DeclType.Unknown)
                {
                    nextVariable.Trace = nextVariable.Trace.Append($"{section.Owner}: {nextVariable?.Name} is {nextVariable?.DeclType} because {instruction}");
                }
            }
        }
    }
    
    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections)
        {
            ProcessSection(section);
        }
    }
}