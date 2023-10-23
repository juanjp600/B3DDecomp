using System.Diagnostics;

namespace Blitz3DDecomp;

sealed class GlobalVariable : Variable
{
    public static readonly List<GlobalVariable> AllGlobals = new List<GlobalVariable>();

    public GlobalVariable(string name) : base(name)
    {
        AllGlobals.Add(this);
    }

    public override string ToInstructionArg()
        => $"[@_v{Name.ToLowerInvariant()}]";
}
