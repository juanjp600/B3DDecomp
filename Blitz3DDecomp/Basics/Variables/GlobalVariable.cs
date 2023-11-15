using System.Diagnostics;

namespace Blitz3DDecomp;

sealed class GlobalVariable : Variable
{
    public static ICollection<GlobalVariable> AllGlobals => lookupDictionary.Values;
    private static Dictionary<string, GlobalVariable> lookupDictionary = new();

    public static GlobalVariable? FindByName(string name)
    {
        name = name.ToLowerInvariant();
        if (name.Length >= 1 && name[0] == '@') { name = name[1..]; }
        if (name.Length >= 2 && name[0] == '_' && name[1] == 'v') { name = name[2..]; }

        if (lookupDictionary.TryGetValue(name, out var global)) { return global; }
        return null;
    }

    public GlobalVariable(string name) : base(name)
    {
        lookupDictionary.Add(name.ToLowerInvariant(), this);
    }

    public override string ToInstructionArg()
        => $"[@_v{Name.ToLowerInvariant()}]";
}
