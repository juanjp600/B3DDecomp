namespace Blitz3DDecomp.DecompilerSteps.Step1;

static class LocationToVarRewrite
{
    private sealed class TempTracker
    {
        private readonly Dictionary<string, int> Indices = new Dictionary<string, int>();

        public void ProcessInstruction(Function function, Instruction instruction)
        {
            void replace(ref string s)
            {
                foreach (var kvp in Indices)
                {
                    s = s.Replace(kvp.Key, $"{kvp.Key}{kvp.Value:X4}");
                }

                foreach (var tempVar in function.CompilerGeneratedTempVars)
                {
                    if (s == tempVar.ToInstructionArg())
                    {
                        s = $"{tempVar.Name}_{Indices[tempVar.Name]}";
                    }
                }

                if (function.InstructionArgumentToVariable(s) is { } variable)
                {
                    s = variable.Name;
                }
            }

            replace(ref instruction.SrcArg1);
            replace(ref instruction.SrcArg2);

            if (instruction.Name is "call")
            {
                int index = Indices.TryGetValue("eax", out var i) ? (i + 1) : 0;
                Indices["eax"] = index;
            }
            if (instruction.Name is "mov" or "lea" or "pop")
            {
                if (instruction.DestArg.IsRegister() && instruction.DestArg is not ("esp" or "ebp"))
                {
                    int index = Indices.TryGetValue(instruction.DestArg, out var i) ? (i + 1) : 0;
                    Indices[instruction.DestArg] = index;
                }

                foreach (var tempVar in function.CompilerGeneratedTempVars)
                {
                    if (instruction.DestArg == tempVar.ToInstructionArg())
                    {
                        var varName = tempVar.Name;
                        int index = Indices.TryGetValue(varName, out var i) ? (i + 1) : 0;
                        Indices[varName] = index;
                    }
                }
            }

            replace(ref instruction.DestArg);
        }
    }

    private static void ProcessSection(TempTracker tempTracker, Function.AssemblySection section)
    {
        foreach (var instruction in section.Instructions)
        {
            tempTracker.ProcessInstruction(section.Owner, instruction);
        }
    }
    
    public static void Process(Function function)
    {
        var tempTracker = new TempTracker();
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(tempTracker, section);
        }
    }
}