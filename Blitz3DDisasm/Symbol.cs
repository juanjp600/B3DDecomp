namespace Blitz3DDecomp;

public enum SymbolType
{
    Other = 0,
    Code,
    Variable,
    Type,
    DimArray,
    Vector,
    Data,
    Libs,
    BuiltIn
}

sealed class Symbol
{
    public int Address;
    public string Name;
    public string? NewName;

    public string NameToPrint => NewName ?? Name;

    public string? OwnerName { get; private set; }

    private SymbolType inferredType;

    public Symbol(string name)
    {
        Name = name;
    }

    public void TrySetInferredType(SymbolType type, string? ownerName)
    {
        if (Type != SymbolType.Other) { return; }
        ForceSetInferredType(type, ownerName);
    }

    public void ForceSetInferredType(SymbolType type, string? ownerName)
    {
        inferredType = type;
        OwnerName = ownerName;
    }

    public SymbolType Type
    {
        get
        {
            if (inferredType != SymbolType.Other)
            {
                return inferredType;
            }
            if (Name == "__DATA")
            {
                OwnerName = Name;
                inferredType = SymbolType.Data;
                return SymbolType.Data;
            }
            if (Name == "__LIBS")
            {
                OwnerName = Name;
                inferredType = SymbolType.Libs;
                return SymbolType.Libs;
            }
            if (Name.StartsWith("_v"))
            {
                OwnerName = Name;
                inferredType = SymbolType.Variable;
                return SymbolType.Variable;
            }
            if (Name.StartsWith("_t"))
            {
                OwnerName = Name;
                inferredType = SymbolType.Type;
                return SymbolType.Type;
            }
            if (Name.StartsWith("_a"))
            {
                OwnerName = Name;
                inferredType = SymbolType.DimArray;
                return SymbolType.DimArray;
            }
            if (Name == "__MAIN"
                || Name.StartsWith("_f"))
            {
                OwnerName = Name;
                inferredType = SymbolType.Code;
                return SymbolType.Code;
            }
            return SymbolType.Other;
        }
    }
}
