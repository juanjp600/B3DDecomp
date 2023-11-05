using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;

namespace Blitz3DDecomp;

sealed class Function
{
    public sealed class AssemblySection
    {
        public readonly Function Owner;
        public readonly string Name;
        public readonly List<Instruction> Instructions = new List<Instruction>();

        public AssemblySection(Function owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public IEnumerable<GlobalVariable> ReferencedGlobals
            => ReferencedVariables
                .OfType<GlobalVariable>();

        public IEnumerable<Variable> ReferencedVariables
            => Instructions
                .SelectMany(i => new[] { i.LeftArg, i.RightArg })
                .Select(s => s.StripDeref())
                .Select(Owner.InstructionArgumentToVariable)
                .OfType<Variable>()
                .Distinct(); // Removes null entries

        public void CleanupNop()
        {
            var nopIndices = Instructions
                .Select((instr, index) => instr.Name == "nop" ? index : -1)
                .Where(index => index >= 0)
                .Reverse()
                .ToArray();

            foreach (var instruction in Instructions)
            {
                if (instruction.CallParameterAssignmentIndices is not { } callParameterAssignmentIndices) { continue; }

                for (int i = 0; i < callParameterAssignmentIndices.Length; i++)
                {
                    callParameterAssignmentIndices[i] -=
                        nopIndices.Count(index => index <= callParameterAssignmentIndices[i]);
                }
            }

            Instructions.RemoveAll(instr => instr.Name == "nop");
        }
    }

    public sealed class Parameter : Variable
    {
        public readonly int Index;

        public Parameter(string name, int index) : base(name)
        {
            Index = index;
        }

        public override string ToInstructionArg()
            => $"[ebp+0x{((Index << 2) + 0x14):x1}]";
    }

    public sealed class LocalVariable : Variable
    {
        public readonly int Index;

        public LocalVariable(string name, int index) : base(name)
        {
            Index = index;
        }

        public override string ToInstructionArg()
            => $"[ebp-0x{((Index << 2) + 0x4):x1}]";
    }

    public readonly string Name;
    public readonly Dictionary<string, AssemblySection> AssemblySections;

    public int TotalInstructionCount => AssemblySections.Values.Select(s => s.Instructions.Count).Sum();

    public static readonly List<Function> AllFunctions = new List<Function>();

    private static Dictionary<string, Function> lookupDictionary = new Dictionary<string, Function>();

    public static void InitBuiltIn(Compiler compiler)
    {
        switch (compiler)
        {
            case Compiler.Blitz3d:
                Blitz3dBuiltIns.Init();
                break;
            case Compiler.BlitzPlus:
                BlitzPlusBuiltIns.Init();
                break;
        }
    }

    public static Function? GetFunctionWithName(string name)
    {
        name = name.ToLowerInvariant();
        if (name[0] == '@') { name = name[1..]; }

        if (lookupDictionary.TryGetValue(name, out var f))
        {
            return f;
        }

        if (name[0] == '_' && name[1] == 'f')
        {
            name = name[2..];
            if (lookupDictionary.TryGetValue(name, out f))
            {
                return f;
            }
        }

        return null;
    }

    public bool IsBuiltIn
        => Name.StartsWith("_builtIn");

    public string CoreSymbolName
        => Name == "EntryPoint"
            ? "__MAIN"
            : Name.StartsWith("_builtIn")
                ? Name
                : $"_f{Name}";

    public DeclType ReturnType = DeclType.Unknown;
    public readonly List<Parameter> Parameters = new List<Parameter>();
    public readonly List<LocalVariable> LocalVariables = new List<LocalVariable>();
    public readonly List<LocalVariable> CompilerGeneratedTempVars = new List<LocalVariable>();

    public Variable? InstructionArgumentToVariable(string arg)
    {
        arg = arg.StripDeref();
        if (arg.StartsWith("ebp-0x", StringComparison.Ordinal))
        {
            var varIndex = (int.Parse(arg[6..], NumberStyles.HexNumber) - 0x4) >> 2;
            if (varIndex >= 0 && varIndex < LocalVariables.Count)
            {
                return LocalVariables[varIndex];
            }
        }
        else if (arg.StartsWith("ebp+0x", StringComparison.Ordinal))
        {
            var paramIndex = (int.Parse(arg[6..], NumberStyles.HexNumber) - 0x14) >> 2;
            if (paramIndex >= 0 && paramIndex < Parameters.Count)
            {
                return Parameters[paramIndex];
            }
        }
        else if (arg.StartsWith("@_v", StringComparison.Ordinal))
        {
            return GlobalVariable.AllGlobals.FirstOrDefault(
                v => v.Name.Equals(arg[3..], StringComparison.OrdinalIgnoreCase));
        }
        else if (arg.Contains('\\', StringComparison.Ordinal))
        {
            var split = arg.Split('\\');

            var matchingRootVar = LocalVariables.Cast<Variable>().Concat(Parameters).Concat(GlobalVariable.AllGlobals)
                .FirstOrDefault(v => v.Name.Equals(split[0], StringComparison.OrdinalIgnoreCase));
            if (matchingRootVar is null) { return null; }

            var currVar = matchingRootVar;
            for (int i = 1; i < split.Length; i++)
            {
                var part = split[i];
                var arrayIndex = "";
                if (part.IndexOf('[', StringComparison.Ordinal) is var indexerStart and >= 0
                    && part.IndexOf(']', StringComparison.Ordinal) is var indexerEnd and >= 0)
                {
                    arrayIndex = part[(indexerStart + 1)..indexerEnd];
                    part = part[..indexerStart];
                }
                var index = int.Parse(string.Join("", part.Where(char.IsDigit)));
                currVar = currVar.Fields[index];
                if (arrayIndex != "" && currVar.GetArrayElement(arrayIndex) is { } arrayElement)
                {
                    currVar = arrayElement;
                }
            }
            return currVar;
        }

        return null;
    }

    public sealed class Instruction
    {
        public string Name;
        public string LeftArg;
        public string RightArg;

        public int[]? CallParameterAssignmentIndices;
        public string? BbObjType;

        public Instruction(string name, string leftArg = "", string rightArg = "")
        {
            Name = name;
            LeftArg = leftArg;
            RightArg = rightArg;
        }

        public bool IsJumpOrCall
            => Name is
                "call" or "jmp" or "je" or "jz"
                or "jne" or "jnz" or "jg" or "jge"
                or "jl" or "jle";

        public override string ToString()
            => string.IsNullOrWhiteSpace(LeftArg)
                ? Name
                : string.IsNullOrWhiteSpace(RightArg)
                    ? $"{Name} {LeftArg}"
                    : $"{Name} {LeftArg}, {RightArg}";
    }

    public static Function FromBlitzSymbol(string str)
    {
        static DeclType ripTypeFromStr(ref string str, DeclType defaultType)
        {
            if (str[0] == '#')
            {
                str = str[1..];
                return DeclType.Float;
            }
            if (str[0] == '%')
            {
                str = str[1..];
                return DeclType.Int;
            }
            if (str[0] == '$')
            {
                str = str[1..];
                return DeclType.String;
            }
            return defaultType;
        }

        DeclType returnType = ripTypeFromStr(ref str, DeclType.Unknown);

        str = str
            .Replace("%", " %")
            .Replace("#", " #")
            .Replace("$", " $");
        var split = str.Split(" ");
        var parameters = new List<Parameter>();
        var funcName = split[0].ToLowerInvariant();
        split = split.Skip(1).ToArray();
        for (var argIndex = 0; argIndex < split.Length; argIndex++)
        {
            var argName = split[argIndex];
            var argType = ripTypeFromStr(ref argName, DeclType.Int);
            parameters.Add(new Parameter(argName, argIndex) { DeclType = argType });
        }

        var newFunction = new Function($"_builtIn_f{funcName}", 0) { ReturnType = returnType };
        newFunction.Parameters.Clear(); newFunction.Parameters.AddRange(parameters);
        return newFunction;
    }

    public Function(string name, int argCount) : this(name, new Dictionary<string, AssemblySection>())
    {
        Parameters = Enumerable.Range(0, argCount)
            .Select(i => new Parameter($"arg{i}", i) { DeclType = DeclType.Unknown })
            .ToList();
    }

    public Function(string name, params DeclType[] types) : this(name, types.Length)
    {
        for (int i = 0; i < types.Length; i++)
        {
            Parameters[i].DeclType = types[i];
        }
    }

    public Function(string name, Dictionary<string, AssemblySection> assemblySections)
    {
        Name = name;
        AssemblySections = assemblySections;
        AllFunctions.Add(this);
        lookupDictionary.Add(name.ToLowerInvariant(), this);
    }

    public override string ToString()
    {
        return Name + ReturnType.Suffix + "("
               + string.Join(", ", Parameters) + ")";
    }
}
