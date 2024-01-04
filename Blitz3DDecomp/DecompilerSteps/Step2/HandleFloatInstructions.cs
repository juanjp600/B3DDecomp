namespace Blitz3DDecomp.DecompilerSteps.Step2;

static class HandleFloatInstructions
{
    private static void ProcessSection(Function.AssemblySection section)
    {
        for (var i = 1; i < section.Instructions.Count - 1; i++)
        {
            void handlePotentialFloatReturn()
            {
                if (section.Owner.ReturnType == DeclType.Unknown && i < section.Instructions.Count - 2)
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