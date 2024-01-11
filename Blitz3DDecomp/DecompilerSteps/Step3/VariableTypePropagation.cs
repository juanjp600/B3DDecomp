using B3DDecompUtils;
using Blitz3DDecomp.LowLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step3;

static class VariableTypePropagation
{
    public static bool Process(Function function)
    {
        bool somethingChanged = false;

        void exchangeTypes(Variable destVar, Variable srcVar)
        {
            if (destVar.DeclType == DeclType.Unknown && srcVar.DeclType != DeclType.Unknown)
            {
                somethingChanged = true;
                destVar.DeclType = srcVar.DeclType;
                destVar.Trace = srcVar.Trace.Append($"{function}: {destVar.Name} is {destVar.DeclType} because {srcVar}");
            }
            if (srcVar.DeclType == DeclType.Unknown && destVar.DeclType != DeclType.Unknown)
            {
                somethingChanged = true;
                srcVar.DeclType = destVar.DeclType;
                srcVar.Trace = destVar.Trace.Append($"{function}: {srcVar.Name} is {srcVar.DeclType} because {destVar}");
            }
        }

        void handleMovLea(Instruction instruction)
        {
            if (instruction.Name is not ("mov" or "lea")) { return; }

            var destVar = function.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = function.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null || srcVar is null) { return; }

            exchangeTypes(destVar, srcVar);
        }

        void handleXchg(Instruction instruction)
        {
            if (instruction.Name is not "xchg") { return; }

            var lhsVarPrev = function.InstructionArgumentToVariable(instruction.DestArg);
            var rhsVarPrev = function.InstructionArgumentToVariable(instruction.SrcArg1);
            var lhsVarPost = instruction.XchgLhsPost;
            var rhsVarPost = instruction.XchgRhsPost;

            if (lhsVarPrev is null || rhsVarPrev is null || lhsVarPost is null || rhsVarPost is null) { return; }

            exchangeTypes(rhsVarPost, lhsVarPrev);
            exchangeTypes(lhsVarPost, rhsVarPrev);
        }

        void handleCmp(Instruction instruction)
        {
            if (instruction.Name is not "cmp") { return; }

            var destVar = function.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = function.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null && srcVar is null) { return; }

            uint parsedUint;
            if (destVar != null && srcVar != null)
            {
                exchangeTypes(destVar, srcVar);
            }
            else if (instruction.DestArg.TryHexToUint32(out parsedUint) && parsedUint != 0 && srcVar?.DeclType == DeclType.Unknown)
            {
                srcVar.DeclType = DeclType.Int;
                srcVar.Trace = srcVar.Trace.Append($"{function}: {srcVar.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }
            else if (instruction.SrcArg1.TryHexToUint32(out parsedUint) && parsedUint != 0 && destVar?.DeclType == DeclType.Unknown)
            {
                destVar.DeclType = DeclType.Int;
                destVar.Trace = destVar.Trace.Append($"{function}: {destVar.Name} is {DeclType.Int} because {instruction}");
                somethingChanged = true;
            }
        }

        foreach (var instruction in function.Instructions)
        {
            handleMovLea(instruction);
            handleXchg(instruction);
            handleCmp(instruction);
        }
        foreach (var instruction in function.Instructions.Reverse())
        {
            handleMovLea(instruction);
            handleXchg(instruction);
            handleCmp(instruction);
        }

        return somethingChanged;
    }
}