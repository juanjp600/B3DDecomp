﻿using System.Collections.Immutable;
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
                .SelectMany(i => new[] { i.DestArg, i.SrcArg1, i.SrcArg2 })
                .Select(s => s.StripDeref())
                .Select(Owner.InstructionArgumentToVariable)
                .OfType<Variable>() // Removes null entries
                .Distinct();

        public void CleanupNop()
        {
            var nopIndices = Instructions
                .Select((instr, index) => instr.Name == "nop" ? index : -1)
                .Where(index => index >= 0)
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

    public sealed class Parameter : VariableWithOwnType
    {
        public readonly int Index;

        public Parameter(string name, int index) : base(name)
        {
            Index = index;
        }

        public override string ToInstructionArg()
            => $"[ebp+0x{((Index << 2) + 0x14):x1}]";
    }

    public sealed class LocalVariable : VariableWithOwnType
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

    public static ICollection<Function> AllFunctions => lookupDictionary.Values;

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

    public static Function GetFunctionByName(string name)
        => TryGetFunctionByName(name)
           ?? throw new Exception($"Function {name} was not loaded from symbols nor defined as a builtin");

    public static Function? TryGetFunctionByName(string name)
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
            return GlobalVariable.FindByName(arg);
        }
        else
        {
            var split = arg.Split('\\');

            Variable? currVar = null;
            for (int i = 0; i < split.Length; i++)
            {
                var part = split[i];
                var arrayIndex = "";
                if (part.IndexOf('[', StringComparison.Ordinal) is var indexerStart and >= 0
                    && part.IndexOf(']', StringComparison.Ordinal) is var indexerEnd and >= 0)
                {
                    arrayIndex = part[(indexerStart + 1)..indexerEnd];
                    part = part[..indexerStart];
                }

                if (currVar is null)
                {
                    bool doesVarMatchPart(Variable v)
                        => v.Name.Equals(part, StringComparison.OrdinalIgnoreCase);

                    currVar = LocalVariables.Find(doesVarMatchPart);
                    currVar ??= Parameters.Find(doesVarMatchPart);
                    currVar ??= GlobalVariable.FindByName(part);
                    if (currVar is null && arrayIndex != "" && DimArray.TryFindByName(part) is { } dimArray)
                    {
                        currVar = dimArray.GetAccessVariable(arrayIndex);
                        arrayIndex = "";
                    }
                    if (currVar is null) { return null; }
                }
                else
                {
                    currVar = currVar.Fields.First(f => f.Name.EndsWith(part, StringComparison.OrdinalIgnoreCase));
                }
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
        public string DestArg;
        public string SrcArg1;
        public string SrcArg2;

        public int[]? CallParameterAssignmentIndices;
        public string? BbObjType;

        public Instruction(string name, string destArg = "", string srcArg1 = "", string srcArg2 = "")
        {
            Name = name;
            DestArg = destArg;
            SrcArg1 = srcArg1;
            SrcArg2 = srcArg2;
        }

        public bool IsJumpOrCall
            => Name is
                "call" or "jmp" or "je" or "jz"
                or "jne" or "jnz" or "jg" or "jge"
                or "jl" or "jle";

        public override string ToString()
        {
            var retVal = Name;

            if (string.IsNullOrWhiteSpace(DestArg)) { return retVal; }
            retVal += " " + DestArg;

            if (string.IsNullOrWhiteSpace(SrcArg1)) { return retVal; }
            retVal += ", " + SrcArg1;

            if (string.IsNullOrWhiteSpace(SrcArg2)) { return retVal; }
            retVal += ", " + SrcArg2;

            return retVal;
        }
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
        lookupDictionary.Add(name.ToLowerInvariant(), this);
    }

    public override string ToString()
    {
        return Name + ReturnType.Suffix + "("
               + string.Join(", ", Parameters) + ")";
    }
}
