using System.Diagnostics;
using System.Reflection.Metadata;
using B3DDecompUtils;
using B3DDecompUtils.Primitives;
using Blitz3DDecomp.MidLevel;
using Blitz3DDecomp.MidLevel.Casts;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class MidLevelGen
{
    private readonly record struct DimSizeAssignment(DimArray Array, int Dimension);

    private static void ProcessSection(Function function, string sectionName)
    {
        if (sectionName == "__MAIN" && function.Name == "EntryPoint") { return; }

        var tempToExpression = new Dictionary<Function.DecompGeneratedTempVariable, Expression>();
        var varToDimSizeAssignment = new Dictionary<Function.DecompGeneratedTempVariable, DimSizeAssignment>();
        var assemblySection = function.AssemblySections[sectionName];
        var midLevelSection = function.MidLevelSections[sectionName];

        Expression instructionArgToExpression(string instructionArg)
        {
            Expression shiftRight2(Expression original)
                => original is ShiftLeftExpression { Lhs: var innerExpression, Rhs: ConstantExpression { Value: "0x2" } }
                    ? innerExpression
                    : new ShiftRightUnsignedExpression(original, new ConstantExpression("0x2"));
            Expression indexStrToExpression(string indexStr)
                => indexStr.EndsWith(">>2", StringComparison.Ordinal)
                    ? shiftRight2(instructionArgToExpression(indexStr[..^3]))
                    : instructionArgToExpression(indexStr);

            var srcVar = function.InstructionArgumentToVariable(instructionArg);

            return srcVar switch
            {
                Function.DecompGeneratedTempVariable tempSrcVar
                    => tempToExpression.TryGetValue(tempSrcVar, out var foundExpression)
                        ? foundExpression
                        : new VariableExpression(tempSrcVar),
                FieldVariable fieldVar
                    => new FieldAccessExpression(instructionArgToExpression(fieldVar.Owner.Name), fieldVar.TypeField),
                ArrayElementVariable arrayElementVariable
                    => new ArrayAccessExpression(instructionArgToExpression(arrayElementVariable.Owner.Name), indexStrToExpression(arrayElementVariable.Index)),
                DimArray.AccessVariable dimElementVar
                    => dimElementVar.DimArray.NumDimensions == 1
                        ? new DimAccessExpression(dimElementVar.DimArray, indexStrToExpression(dimElementVar.Index))
                        : throw new NotImplementedException(),
                not null
                    => new VariableExpression(srcVar),
                null
                    => new ConstantExpression(instructionArg.StripDeref())
            };
        }

        Expression? jumpSrcExpression = null;
        for (int i = 0; i < assemblySection.Instructions.Length; i++)
        {
            Variable? destVar;
            Expression lhsExpression;
            Expression rhsExpression;
            Expression srcExpression;

            bool anyReferencesAfterCurrentInstruction(Variable variable)
                => assemblySection.Instructions.Skip(i + 1).Any(instr =>
                    function.InstructionArgumentToVariable(instr.DestArg) == variable
                    || function.InstructionArgumentToVariable(instr.SrcArg1) == variable
                    || function.InstructionArgumentToVariable(instr.SrcArg2) == variable);

            void handleAssignment(Variable destVar, Expression srcExpression)
            {
                if (destVar is Function.DecompGeneratedTempVariable tempDestVar)
                {
                    tempToExpression[tempDestVar] = srcExpression;
                }
                else
                {
                    midLevelSection.Statements.Add(new AssignmentStatement(new VariableExpression(destVar), srcExpression));
                }
            }

            var instruction = assemblySection.Instructions[i];
            switch (instruction.Name)
            {
                case "jz" or "je":
                    midLevelSection.Statements.Add(new JumpIfZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jnz" or "jne":
                    midLevelSection.Statements.Add(new JumpIfNotZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jge":
                    midLevelSection.Statements.Add(new JumpIfGreaterThanOrEqualToZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jg":
                    midLevelSection.Statements.Add(new JumpIfGreaterThanZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jle":
                    midLevelSection.Statements.Add(new JumpIfLessThanOrEqualToZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jl":
                    midLevelSection.Statements.Add(new JumpIfLessThanZeroStatement(jumpSrcExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "jmp":
                    midLevelSection.Statements.Add(new UnconditionalJumpStatement(function.MidLevelSections[instruction.DestArg[1..]]));
                    break;
                case "shl":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignment(destVar, new ShiftLeftExpression(lhsExpression, rhsExpression));
                    break;
                case "shr":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignment(destVar, new ShiftRightUnsignedExpression(lhsExpression, rhsExpression));
                    break;
                case "sar":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignment(destVar, new ShiftRightSignedExpression(lhsExpression, rhsExpression));
                    break;
                case "and":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    jumpSrcExpression = new AndExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, jumpSrcExpression);
                    break;
                case "or":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    jumpSrcExpression = new OrExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, jumpSrcExpression);
                    break;
                case "xor":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    jumpSrcExpression = new XorExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, jumpSrcExpression);
                    break;
                case "cmp":
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    jumpSrcExpression = new SubtractExpression(lhsExpression, rhsExpression);
                    break;
                case "sub":
                    if (instruction.DestArg == "esp") { continue; }
                    Debugger.Break();
                    break;
                case "add":
                    if (instructionArgToExpression(instruction.SrcArg1) is ConstantExpression { Value: var dimSymbol }
                        && DimArray.TryFindByName(dimSymbol) is { } dimArray
                        && instruction.SrcArg2.TryHexToUint32(out var dimSymbolOffset)
                        && function.InstructionArgumentToVariable(instruction.DestArg) is Function.DecompGeneratedTempVariable dimSizeDestVar)
                    {
                        varToDimSizeAssignment[dimSizeDestVar] = new DimSizeAssignment(dimArray, (int)((dimSymbolOffset - 0xc) >> 2));
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    break;
                case "call":
                    var assignmentIndices = instruction.CallParameterAssignmentIndices
                        ?? throw new Exception($"CallParameterAssignmentIndices is null for {instruction}");
                    var returnOutputVar = instruction.ReturnOutputVar 
                        ?? throw new Exception($"ReturnOutputVar is null for {instruction}");

                    var callee = Function.GetFunctionByName(instruction.DestArg);
                    if (callee.AssemblySections.Count > 0
                        || callee.Name.EndsWith("__LIBS", StringComparison.Ordinal)
                        || callee.Name.StartsWith("_builtIn_f", StringComparison.Ordinal))
                    {
                        var arguments = new List<Expression>();
                        for (int j = 0; j < assignmentIndices.Length; j++)
                        {
                            var assignInstruction = assemblySection.Instructions[assignmentIndices[j]];
                            arguments.Add(instructionArgToExpression(assignInstruction.SrcArg1));
                        }
                        var callExpression = new CallExpression(callee, arguments.ToArray());
                        tempToExpression[returnOutputVar] = callExpression;
                        if (!anyReferencesAfterCurrentInstruction(returnOutputVar))
                        {
                            midLevelSection.Statements.Add(new FreeStandingExpressionStatement(callExpression));
                        }
                    }
                    else if (callee.Name is "_builtIn__bbStrConst" or "_builtIn__bbStrLoad")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        tempToExpression[returnOutputVar] = srcExpression;
                    }
                    else if (callee.Name is "_builtIn__bbStrToInt")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        tempToExpression[returnOutputVar] = new ConvertToIntExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrToFloat")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        tempToExpression[returnOutputVar] = new ConvertToFloatExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrFromInt" or "_builtIn__bbStrFromFloat")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        tempToExpression[returnOutputVar] = new ConvertToStringExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrConcat")
                    {
                        var destAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToExpression(destAssignInstruction.SrcArg1);
                        var srcAssignInstruction = assemblySection.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpression(srcAssignInstruction.SrcArg1);
                        tempToExpression[returnOutputVar] = new AddExpression(lhsExpression, rhsExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrStore" or "_builtIn__bbObjStore")
                    {
                        var destAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToExpression(destAssignInstruction.SrcArg1);
                        if (lhsExpression is not AccessExpression accessExpression)
                        {
                            throw new Exception($"{destAssignInstruction.SrcArg1} does not resolve to an access expression");
                        }

                        var srcAssignInstruction = assemblySection.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpression(srcAssignInstruction.SrcArg1);
                        midLevelSection.Statements.Add(new AssignmentStatement(accessExpression, rhsExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjNew")
                    {
                        var typeAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        var typeAssignExpression = instructionArgToExpression(typeAssignInstruction.SrcArg1);
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName })
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        tempToExpression[returnOutputVar] = new ConstructorExpression(customType);
                    }
                    else if (callee.Name == "_builtIn__bbDimArray")
                    {
                        var dimAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        var dimAssignExpression = instructionArgToExpression(dimAssignInstruction.SrcArg1);
                        if (dimAssignExpression is not ConstantExpression { Value: var dimSymbol2 })
                        {
                            throw new Exception($"{dimAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var dim = DimArray.TryFindByName(dimSymbol2) ?? throw new Exception($"Dim {dimSymbol2} not found");
                        var sizeAssignmentExpressions = new Expression[dim.NumDimensions];
                        for (int j = 0; j < dim.NumDimensions; j++)
                        {
                            var sizeAssignmentVariable = varToDimSizeAssignment.First(kvp => kvp.Value.Array == dim && kvp.Value.Dimension == j);
                            sizeAssignmentExpressions[j] = instructionArgToExpression(sizeAssignmentVariable.Key.Name);
                        }
                        midLevelSection.Statements.Add(new AllocateDimStatement(dim, sizeAssignmentExpressions));
                    }
                    else if (callee.Name == "_builtIn__bbUndimArray")
                    {
                        continue;
                    }
                    else if (callee.Name == "_builtIn__bbVecAlloc")
                    {
                        if (function.InstructionArgumentToVariable(assemblySection.Instructions[i + 1].SrcArg1) != instruction.ReturnOutputVar)
                        {
                            Debugger.Break();
                        }
                        i++;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    break;
                case "mov" or "lea":
                    if (instruction.SrcArg1.IsRegister()) { continue; }

                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    srcExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignment(destVar, srcExpression);
                    break;
                case "xchg":
                    var lhsVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    var rhsVar = function.InstructionArgumentToVariable(instruction.SrcArg1);
                    if (lhsVar is not Function.DecompGeneratedTempVariable tempLhsVar
                        || rhsVar is not Function.DecompGeneratedTempVariable tempRhsVar)
                    {
                        Debugger.Break();
                        continue;
                    }

                    lhsExpression = tempToExpression[tempLhsVar];
                    rhsExpression = tempToExpression[tempRhsVar];
                    tempToExpression[tempLhsVar] = rhsExpression;
                    tempToExpression[tempRhsVar] = lhsExpression;
                    break;
                case "push" or "pop":
                    // Skip because there's nothing useful that can be done with these
                    break;
                default:
                    Debugger.Break();
                    break;
            }
        }
    }
    
    public static void Process(Function function)
    {
        foreach (var sectionName in function.AssemblySections.Keys)
        {
            function.MidLevelSections.Add(sectionName, new MidLevelSection(sectionName));
        }
        foreach (var sectionName in function.AssemblySections.Keys)
        {
            ProcessSection(function, sectionName);
        }
    }
}