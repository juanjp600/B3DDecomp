﻿using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp;

sealed class Function
{
    public sealed class Parameter : VariableWithOwnType
    {
        public readonly Function Owner;
        public readonly int Index;

        public Parameter(Function owner, string name, int index) : base(name)
        {
            Owner = owner;
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

    public sealed class DecompGeneratedTempVariable : VariableWithOwnType
    {
        public DecompGeneratedTempVariable(string name) : base(name) { }

        public override string ToInstructionArg()
            => Name;
    }

    public readonly string Name;
    public readonly ImmutableArray<Instruction> Instructions;

    public readonly ImmutableArray<AssemblySection> AssemblySections;
    public readonly ImmutableDictionary<string, AssemblySection> AssemblySectionsByName;

    public readonly List<Statement> HighLevelStatements;
    public readonly List<HighLevelSection> HighLevelSections;
    public ImmutableDictionary<string, HighLevelSection> HighLevelSectionsByName
        => HighLevelSections.ToImmutableDictionary(s => s.Name, s => s);

    public void FindSectionForStatementIndex(int statementIndex, out HighLevelSection section, out int indexInSection)
    {
        indexInSection = -1;

        section = HighLevelSections[0];
        int currentIndexInSection = statementIndex;
        for (int i = 0; i < HighLevelSections.Count; i++)
        {
            if (currentIndexInSection < HighLevelSections[i].Statements.Count)
            {
                section = HighLevelSections[i];
                indexInSection = currentIndexInSection;
                break;
            }
            currentIndexInSection -= HighLevelSections[i].Statements.Count;
        }
    }

    public static ICollection<Function> AllFunctions => lookupDictionary.Values;

    private static Dictionary<string, Function> lookupDictionary = new Dictionary<string, Function>();

    public static void InitBuiltIn(string inputDir, Compiler compiler)
    {
        BlitzSymbol.Init(inputDir.AppendToPath("BuiltInSymbols.txt"));
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
    public readonly Dictionary<string, DecompGeneratedTempVariable> DecompGeneratedTempVars = new();

    public DebugTrace Trace = default;

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
                    if (part.StartsWith("temp", StringComparison.OrdinalIgnoreCase) || (part.Length >= 3 && part[..3].IsRegister()))
                    {
                        currVar ??= DecompGeneratedTempVars.GetValueOrDefault(part);
                    }
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

    public static Function? FromBlitzSymbol(string str)
    {
        var symbolOption = BlitzSymbol.FromString(str);
        if (!symbolOption.TryUnwrap(out var symbol)) { return null; }
        if (BlitzSymbol.SymbolsDeclaredByExecutable.TryGetValue(symbol.FunctionName, out var existingSymbol))
        {
            symbol = existingSymbol;
        }

        var newFunction = new Function($"_builtIn_f{symbol.FunctionName}", 0) { ReturnType = symbol.ReturnType };
        newFunction.Parameters.Clear();
        newFunction.Parameters.AddRange(symbol.Parameters.Select((p, i) => new Parameter(newFunction, p.Name, i) { DeclType = p.DeclType }));
        return newFunction;
    }

    public Function(string name, int argCount) : this(name, Array.Empty<Instruction>(), Array.Empty<IngestCodeFiles.TempSection>())
    {
        Parameters = Enumerable.Range(0, argCount)
            .Select(i => new Parameter(this, $"arg{i}", i) { DeclType = DeclType.Unknown })
            .ToList();
    }

    public Function(string name, params DeclType[] types) : this(name, types.Length)
    {
        for (int i = 0; i < types.Length; i++)
        {
            Parameters[i].DeclType = types[i];
        }
    }

    public Function(string name, IReadOnlyList<Instruction> instructions, IReadOnlyList<IngestCodeFiles.TempSection> assemblySections)
    {
        Name = name;
        Instructions = instructions.ToImmutableArray();
        var constructedSections = new List<AssemblySection>();
        int prevIndex = instructions.Count;
        foreach (var section in assemblySections.OrderByDescending(s => s.StartIndex))
        {
            constructedSections.Insert(0, new AssemblySection(this, section.Name, section.StartIndex..prevIndex));
            prevIndex = section.StartIndex;
        }
        AssemblySections = constructedSections.ToImmutableArray();
        AssemblySectionsByName = AssemblySections.ToImmutableDictionary(s => s.Name, s => s);

        HighLevelStatements = new List<Statement>();
        HighLevelSections = AssemblySections.Select(s => new HighLevelSection(this, s.Name)).ToList();
        lookupDictionary.Add(name.ToLowerInvariant(), this);
    }

    public override string ToString()
    {
        return Name + ReturnType.Suffix + "("
               + string.Join(", ", Parameters) + ")";
    }

    public bool CanReturnTypeBeSourceOfPropagation()
    {
        if (ReturnType == DeclType.Unknown) { return false; }

        if (ReturnType == DeclType.Pointer
            && Name.EndsWith("__LIBS", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public void RemoveUnreferencedHighLevelSections()
    {
        var referencedSectionNames = HighLevelStatements
            .OfType<JumpStatement>()
            .Select(js => js.SectionName)
            .Distinct()
            .ToHashSet();
        var unreferencedSections = HighLevelSections.Where(s => !referencedSectionNames.Contains(s.Name)).ToArray();
        foreach (var section in unreferencedSections)
        {
            var sectionIndex = HighLevelSections.IndexOf(section);
            if (sectionIndex <= 0) { continue; }

            HighLevelSections[sectionIndex - 1].Statements.IncreaseCount(section.Statements.Count);
            HighLevelSections.RemoveAt(sectionIndex);
        }
    }
}
