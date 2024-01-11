using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class RemoveSingleUseTemps
{
    private static void ProcessStatement(Function function, int statementIndex)
    {
        var statement = function.HighLevelSections[statementIndex];
    }

    public static void ProcessFunction(Function function)
    {
        
    }
}