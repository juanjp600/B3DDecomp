using System.Diagnostics;
using System.Security;
using Blitz3DDecomp.LowLevel;

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

        public void ProcessInstruction(Instruction instruction, bool canGenerateTempVars)
        {
            void replace(ref string s)
            {
                foreach (var kvp in Indices)
                {
                    var newName = getIndexedName(kvp.Key);
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
                        s = getIndexedName(tempVar.Name);
                        if (string.IsNullOrEmpty(s)) { Debugger.Break(); }
                        break;
                    }
                }
            }

            string getIndexedName(string originalName)
            {
                int index = Indices.GetValueOrDefault(originalName, -1);

                if (index < 0)
                {
                    return "";
                }
                return $"{originalName}_{index:X4}";
            }
            void incrementIndex(string originalName, DeclType type, out Function.DecompGeneratedTempVariable newVar)
            {
                int index = Indices.TryGetValue(originalName, out var i) ? (i + 1) : 0;
                Indices[originalName] = index;

                var newVarName = getIndexedName(originalName);
                newVar = new Function.DecompGeneratedTempVariable(newVarName) { DeclType = type };
                function.DecompGeneratedTempVars.Add(newVarName, newVar);
            }

            void getLastVarForName(string originalName, DeclType type, out Function.DecompGeneratedTempVariable variable)
            {
                var indexedName = getIndexedName(originalName);
                if (string.IsNullOrEmpty(indexedName))
                {
                    incrementIndex(originalName, type, out variable);
                    return;
                }

                variable = function.DecompGeneratedTempVars[indexedName];
            }

            switch (instruction.Name)
            {
                case "call":
                {
                    incrementIndex("eax", DeclType.Unknown, out instruction.ReturnOutputVar);
                    break;
                }
                case "mov" or "movzx" or "lea" or "pop" or "add":
                {
                    if (!canGenerateTempVars) { return; }

                    if (instruction.Name == "add")
                    {
                        instruction.SrcArg2 = instruction.SrcArg1;
                        instruction.SrcArg1 = instruction.DestArg;
                    }

                    replace(ref instruction.SrcArg1);
                    replace(ref instruction.SrcArg2);

                    if (instruction.DestArg.IsRegister() && instruction.DestArg is not ("esp" or "ebp"))
                    {
                        incrementIndex(instruction.DestArg, DeclType.Unknown, out _);
                    }
                    else
                    {
                        foreach (var tempVar in function.CompilerGeneratedTempVars)
                        {
                            if (instruction.DestArg == tempVar.ToInstructionArg())
                            {
                                incrementIndex(tempVar.Name, DeclType.Unknown, out _);
                                break;
                            }
                        }
                    }

                    replace(ref instruction.DestArg);
                    break;
                }
                case "xchg":
                {
                    if (!canGenerateTempVars) { return; }

                    var destArg = instruction.DestArg;
                    var srcArg = instruction.SrcArg1;

                    replace(ref instruction.DestArg);
                    replace(ref instruction.SrcArg1);

                    incrementIndex(destArg, DeclType.Unknown, out instruction.XchgLhsPost);
                    incrementIndex(srcArg, DeclType.Unknown, out instruction.XchgRhsPost);
                    break;
                }
                case "cdq":
                {
                    if (!canGenerateTempVars) { return; }

                    getLastVarForName("eax", DeclType.Int, out instruction.SignExtensionValueVar);
                    incrementIndex("edx", DeclType.Int, out instruction.SignExtensionSignVar);
                    break;
                }
                case "idiv" or "div":
                {
                    if (!canGenerateTempVars) { return; }

                    replace(ref instruction.DestArg);

                    getLastVarForName("eax", DeclType.Int, out instruction.DivResultVar);
                    incrementIndex("edx", DeclType.Int, out instruction.DivRemainderVar);
                    break;
                }
                default:
                {
                    if (!canGenerateTempVars) { return; }

                    replace(ref instruction.DestArg);
                    replace(ref instruction.SrcArg1);
                    replace(ref instruction.SrcArg2);
                    break;
                }
            }
        }
    }

    private static void ProcessSection(TempTracker tempTracker, AssemblySection section)
    {
        if (section is { Name: "__MAIN", Owner.Name: "EntryPoint" }) { return; }

        var instructions = section.Instructions;

        for (int instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            tempTracker.ProcessInstruction(instruction, canGenerateTempVars: instructionIndex > section.PreambleEndIndex);
        }
    }

    public static void Process(Function function)
    {
        var tempTracker = new TempTracker(function);
        foreach (var section in function.AssemblySections)
        {
            ProcessSection(tempTracker, section);
        }
    }
}