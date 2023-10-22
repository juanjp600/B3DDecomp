namespace Blitz3DDecomp;

static class VectorTypeDeduction
{
    private static void ProcessSection(Function function, Function.AssemblySection section)
    {
        for (int i = 2; i < section.Instructions.Count - 1; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "call"
                || !instruction.LeftArg.Contains("_builtIn__bbVecAlloc", StringComparison.Ordinal))
            {
                continue;
            }

            var vecTypeToRegister = section.Instructions[i - 2];
            var vecType = DeclType.FromDesc(vecTypeToRegister.RightArg[1..]);

            var registerToArg = section.Instructions[i - 1];

            var resultToVariable = section.Instructions[i + 1];
            var variable = function.InstructionArgumentToVariable(resultToVariable.LeftArg);

            if (vecTypeToRegister.Name == "mov"
                && vecType.IsArrayType
                && vecTypeToRegister.LeftArg == registerToArg.RightArg
                && resultToVariable is { Name: "mov", RightArg: "eax" } 
                && variable != null 
                && variable.DeclType == DeclType.Unknown)
            {
                Console.WriteLine($"{function}: {variable.Name} is {vecType} because {vecTypeToRegister}");
                variable.DeclType = vecType;
            }
        }
    }

    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(function, section);
        }
    }
}