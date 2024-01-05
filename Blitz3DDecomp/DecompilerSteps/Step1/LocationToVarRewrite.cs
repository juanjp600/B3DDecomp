namespace Blitz3DDecomp.DecompilerSteps.Step1;

static class LocationToVarRewrite
{
    private sealed class TempTracker
    {
        private readonly Function function;
        private readonly Dictionary<string, int> Indices = new Dictionary<string, int>();

        public TempTracker(Function function)
        {
            this.function = function;
        }

        public void ProcessInstruction(Instruction instruction)
        {
            void replace(ref string s)
            {
                foreach (var kvp in Indices)
                {
                    var newName = $"{kvp.Key}_{kvp.Value:X4}";
                    s = s == kvp.Key
                        ? newName
                        : s.Replace($"[{kvp.Key}", $"[{newName}");
                }

                if (function.InstructionArgumentToVariable(s) is { } variable)
                {
                    s = s == s.StripDeref()
                        ? variable.Name
                        : s.Replace($"[{s.StripDeref()}", $"[{variable.Name}");
                }

                foreach (var tempVar in function.CompilerGeneratedTempVars)
                {
                    if (s == tempVar.ToInstructionArg())
                    {
                        s = $"[{tempVar.Name}_{Indices[tempVar.Name]:X4}]";
                        break;
                    }
                }
            }

            void incrementIndex(string originalName)
            {
                int index = Indices.TryGetValue(originalName, out var i) ? (i + 1) : 0;
                Indices[originalName] = index;

                var newVarName = $"{originalName}_{index:X4}";
                function.DecompGeneratedTempVars.Add(newVarName, new Function.DecompGeneratedTempVariable(newVarName));
            }

            replace(ref instruction.SrcArg1);
            replace(ref instruction.SrcArg2);

            switch (instruction.Name)
            {
                case "call":
                {
                    incrementIndex("eax");
                    break;
                }
                case "mov" or "movzx" or "lea" or "pop" or "add":
                {
                    if (instruction.Name == "add")
                    {
                        instruction.SrcArg2 = instruction.SrcArg1;
                        instruction.SrcArg1 = instruction.DestArg;
                        replace(ref instruction.SrcArg1);
                    }

                    if (instruction.DestArg.IsRegister() && instruction.DestArg is not ("esp" or "ebp"))
                    {
                        incrementIndex(instruction.DestArg);
                    }

                    foreach (var tempVar in function.CompilerGeneratedTempVars)
                    {
                        if (instruction.DestArg == tempVar.ToInstructionArg())
                        {
                            incrementIndex(tempVar.Name);
                            break;
                        }
                    }

                    break;
                }
            }

            replace(ref instruction.DestArg);
        }
    }

    private static void ProcessSection(TempTracker tempTracker, Function.AssemblySection section)
    {
        IReadOnlyList<Instruction> instructions = section.Instructions;
        if (instructions.Count >= 7
            && instructions[3] is { Name: "push", DestArg: "ebp" }
            && instructions[4] is { Name: "mov", DestArg: "ebp", SrcArg1: "esp" }
            && instructions[5] is { Name: "sub", DestArg: "esp" }
            && instructions[6] is { Name: "mov", SrcArg1: "0x0" }
            && instructions[6].DestArg.IsRegister())
        {
            // Skip preamble and first register assignment because it can't have a concrete type
            instructions = instructions.Skip(7).ToArray();
        }
        foreach (var instruction in instructions)
        {
            tempTracker.ProcessInstruction(instruction);
        }
    }

    public static void Process(Function function)
    {
        var tempTracker = new TempTracker(function);
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(tempTracker, section);
        }
    }
}