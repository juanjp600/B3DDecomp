using B3DDecompUtils;

namespace Blitz3DDecomp.DecompilerSteps.Step4;

public class GuessIntFromNothing
{
    private static void ProcessFunction(Function function)
    {
        void processVariables(IEnumerable<Variable> variables)
        {
            foreach (var variable in variables)
            {
                if (variable.DeclType == DeclType.Unknown)
                {
                    variable.DeclType = DeclType.Int;
                    variable.Trace = variable.Trace.Append($"{function}: {variable.Name} is probably {DeclType.Int} because type deduction failed on all prior steps");
                }
            }
        }

        if (function.ReturnType == DeclType.Unknown)
        {
            function.ReturnType = DeclType.Int;
            function.Trace = function.Trace.Append($"{function}: returns {DeclType.Int} because type deduction failed on all prior steps");
        }
        processVariables(function.LocalVariables);
        processVariables(function.Parameters);
        processVariables(function.DecompGeneratedTempVars.Values);
    }

    public static void Execute()
    {
        foreach (var function in Function.AllFunctions)
        {
            ProcessFunction(function);
        }

        foreach (var global in GlobalVariable.AllGlobals)
        {
            if (global.DeclType == DeclType.Unknown)
            {
                global.DeclType = DeclType.Int;
                global.Trace = global.Trace.Append($"Global variable {global.Name} is probably {DeclType.Int} because type deduction failed on all prior steps");
            }
        }
    }
}