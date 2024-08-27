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
            if (destVar.DeclType == DeclType.Pointer || srcVar.DeclType == DeclType.Pointer)
            {
                return;
            }
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

        void handleMovLea(Instruction instruction, int instructionIndex)
        {
            if (instruction.Name is not ("mov" or "lea")) { return; }

            var destVar = function.InstructionArgumentToVariable(instruction.DestArg);
            var srcVar = function.InstructionArgumentToVariable(instruction.SrcArg1);
            if (destVar is null || srcVar is null) { return; }

            if (srcVar.DeclType == DeclType.Int
                && instruction.Name == "mov"
                && instruction.SrcArg1.StripDeref() != instruction.SrcArg1)
            {
                // Might be passing a Bank to a lib function as a pointer,
                // check for some pointer offset magic
                for (int i = instructionIndex - 1; i >= Math.Max(0, instructionIndex - 200); i--)
                {
                    var prevInstruction = function.Instructions[i];
                    if (prevInstruction.Name != "add") { continue; }
                    if (prevInstruction.DestArg != instruction.SrcArg1.StripDeref()) { continue; }

                    var bankOffsetConstant = CurrentCompiler.Value is Compiler.BlitzPlus
                        ? "0x1c"
                        : "0x4";
                    if (prevInstruction.SrcArg2 != bankOffsetConstant) { break; }

                    var newSrcVar = function.InstructionArgumentToVariable(prevInstruction.SrcArg1);
                    if (newSrcVar?.DeclType != DeclType.Int) { break; }

                    instruction.SrcArg1 = prevInstruction.SrcArg1;
                    destVar.DeclType = DeclType.Pointer;
                    destVar.Trace = newSrcVar.Trace.Append($"{function}: {destVar.Name} is {DeclType.Pointer} because it's a bank passed to a lib function");
                    return;
                }
            }

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

        for (var instructionIndex = 0; instructionIndex < function.Instructions.Length; instructionIndex++)
        {
            var instruction = function.Instructions[instructionIndex];
            handleMovLea(instruction, instructionIndex);
            handleXchg(instruction);
            handleCmp(instruction);
        }

        for (var instructionIndex = function.Instructions.Length - 1; instructionIndex >= 0; instructionIndex--)
        {
            var instruction = function.Instructions[instructionIndex];
            handleMovLea(instruction, instructionIndex);
            handleXchg(instruction);
            handleCmp(instruction);
        }

        return somethingChanged;
    }
}