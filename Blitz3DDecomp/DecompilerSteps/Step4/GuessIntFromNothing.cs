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
                    Logger.WriteLine($"{function}: {variable.Name} is probably {DeclType.Int} because type deduction failed on all prior steps");
                }
            }
        }

        if (function.ReturnType == DeclType.Unknown)
        {
            function.ReturnType = DeclType.Int;
            Logger.WriteLine($"{function}: returns {DeclType.Int} because type deduction failed on all prior steps");
        }
        processVariables(function.LocalVariables);
        processVariables(function.Parameters);
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
                Logger.WriteLine($"Global variable {global.Name} is probably {DeclType.Int} because type deduction failed on all prior steps");
            }
        }

        foreach (var dim in DimArray.AllDimArrays)
        {
            if (dim.ElementDeclType == DeclType.Unknown)
            {
                dim.ElementDeclType = DeclType.Int;
                Logger.WriteLine($"Dim {dim.Name} probably has element type {DeclType.Int} because type deduction failed on all prior steps");
            }
        }
    }
}