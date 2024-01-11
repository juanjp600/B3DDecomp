using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class CalleeReturnTypePropagation
{
    private static bool ProcessSection(AssemblySection section)
    {
        bool somethingChanged = false;
        for (int i = 0; i < section.Instructions.Length - 1; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "call") { continue; }

            var callee = Function.GetFunctionByName(instruction.DestArg);
            if (callee.IsBuiltIn && callee.ReturnType == DeclType.Unknown) { continue; }

            var variable = instruction.ReturnOutputVar;
            if (variable is null) { continue; }

            if (variable.DeclType == DeclType.Unknown && callee.ReturnType != DeclType.Unknown)
            {
                variable.DeclType = callee.ReturnType;
                somethingChanged = true;
                variable.Trace = callee.Trace.Append($"{section.Owner}: {variable.Name} is {variable.DeclType} because {callee.Name}'s return type");
            }
            else if (callee.ReturnType == DeclType.Unknown && variable.DeclType != DeclType.Unknown)
            {
                callee.ReturnType = variable.DeclType;
                somethingChanged = true;
                variable.Trace = callee.Trace.Append($"{section.Owner}: {callee.Name}'s return type is {callee.ReturnType} because {variable.Name}");
            }
        }

        return somethingChanged;
    }

    public static bool Process(Function function)
    {
        bool somethingChanged = false;
        foreach (var section in function.AssemblySections)
        {
            somethingChanged |= ProcessSection(section);
        }
        return somethingChanged;
    }
}