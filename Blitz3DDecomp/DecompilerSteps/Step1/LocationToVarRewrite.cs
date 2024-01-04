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
                    s = s.Replace(kvp.Key, $"{kvp.Key}_{kvp.Value:X4}");
                }

                if (function.InstructionArgumentToVariable(s) is { } variable)
                {
                    s = s.Replace(s.StripDeref(), variable.Name);
                }

                foreach (var tempVar in function.CompilerGeneratedTempVars)
                {
                    if (s == tempVar.ToInstructionArg())
                    {
                        s = $"[{tempVar.Name}_{Indices[tempVar.Name]:X4}]";
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
                case "mov" or "lea" or "pop":
                {
                    if (instruction.DestArg.IsRegister() && instruction.DestArg is not ("esp" or "ebp"))
                    {
                        incrementIndex(instruction.DestArg);
                    }

                    foreach (var tempVar in function.CompilerGeneratedTempVars)
                    {
                        if (instruction.DestArg == tempVar.ToInstructionArg())
                        {
                            incrementIndex(tempVar.Name);
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
        foreach (var instruction in section.Instructions)
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