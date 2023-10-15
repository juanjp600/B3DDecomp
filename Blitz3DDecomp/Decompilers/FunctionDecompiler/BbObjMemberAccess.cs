using System.Diagnostics;

namespace Blitz3DDecomp;

static class BbObjMemberAccess
{
    private static void ProcessSection(Function.AssemblySection section)
    {
        
    }

    public static void Process(Function function)
    {
        foreach (var kvp in function.AssemblySections.ToArray())
        {
            var section = kvp.Value;
            ProcessSection(section);
            function.AssemblySections[kvp.Key] = section;
        }
    }
}