namespace Blitz3DDecomp;

sealed class DimArray
{
    public sealed class AccessVariable : Variable
    {
        private readonly DimArray dimArray;
        public AccessVariable(DimArray dimArray, string arrayIndex) : base($"{dimArray.Name}[{arrayIndex}]")
        {
            this.dimArray = dimArray;
        }

        public override DeclType DeclType
        {
            get => dimArray.ElementDeclType;
            set => dimArray.ElementDeclType = value;
        }

        public override string ToInstructionArg()
            => Name;
    }

    public AccessVariable GetAccessVariable(string arrayIndex) => new AccessVariable(this, arrayIndex);

    public static ICollection<DimArray> AllDimArrays => lookupDictionary.Values;
    private static Dictionary<string, DimArray> lookupDictionary = new();

    public readonly string Name;
    public readonly int NumDimensions;
    public DeclType ElementDeclType;

    private DimArray(string name, int numDimensions, DeclType elementDeclType)
    {
        Name = name;
        NumDimensions = numDimensions;
        ElementDeclType = elementDeclType;

        lookupDictionary.Add(name, this);
    }

    private static (string ArrayName, int NumDimensions, DeclType ElementDeclType) ParseSymbolName(string symbolName)
    {
        var defaultRetVal = ("", 0, DeclType.Unknown);
        
        symbolName = symbolName.StripDeref();
        if (symbolName.Length >= 3 && symbolName[0] == '@') { symbolName = symbolName[1..]; }

        if (symbolName.Length >= 3 && symbolName[0] == '_' && symbolName[1] == 'a')
        {
            symbolName = symbolName[2..];
        }
        else
        {
            return defaultRetVal;
        }

        if (!symbolName.EndsWith("dim", StringComparison.Ordinal)) { return defaultRetVal; }
        symbolName = symbolName[..^3];

        int numDimensions = 0;
        for (int i = symbolName.Length - 1; i >= 0; i--)
        {
            if (symbolName[i] != '_') { continue; }

            if (!int.TryParse(symbolName[(i + 1)..], out numDimensions))
            {
                return defaultRetVal;
            }
            symbolName = symbolName[..i];

            break;
        }
        if (numDimensions <= 0) { return defaultRetVal; }

        string arrayName = "";
        string typeName = "";
        for (int i = symbolName.Length - 1; i >= 0; i--)
        {
            if (symbolName[i] != '_') { continue; }

            arrayName = symbolName[..i];
            typeName = symbolName[(i + 1)..];
            break;
        }

        var elementDeclType = typeName switch
        {
            "int" => DeclType.Int,
            "float" => DeclType.Float,
            "string" => DeclType.String,
            _ => DeclType.Unknown
        };

        return (arrayName, numDimensions, elementDeclType);
    }

    public static DimArray? TryCreateFromSymbolName(string symbolName)
    {
        var (arrayName, numDimensions, elementDeclType) = ParseSymbolName(symbolName);

        if (string.IsNullOrEmpty(arrayName)
            || numDimensions <= 0)
        {
            return null;
        }

        return lookupDictionary.TryGetValue(arrayName, out var retVal)
            ? null
            : new DimArray(name: arrayName, numDimensions: numDimensions, elementDeclType: elementDeclType);
    }

    public static DimArray? TryFindByName(string symbolName)
    {
        if (lookupDictionary.TryGetValue(symbolName, out var retVal))
        {
            return retVal;
        }
        
        var (arrayName, _, _) = ParseSymbolName(symbolName);

        if (string.IsNullOrEmpty(arrayName))
        {
            return null;
        }

        return lookupDictionary.TryGetValue(arrayName, out retVal)
            ? retVal
            : null;
    }
}