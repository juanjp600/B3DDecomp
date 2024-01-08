using System.Diagnostics;
using B3DDecompUtils;
using Blitz3DDecomp.MidLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class MidLevelGen
{
    private readonly record struct DimSizeAssignment(DimArray Array, int Dimension);

    private readonly record struct Context(
        Dictionary<Function.DecompGeneratedTempVariable, Expression> TempToExpression,
        Dictionary<Function.DecompGeneratedTempVariable, DimSizeAssignment> VarToDimSizeAssignment,
        Stack<Expression> FloatStack);

    private static void ProcessSection(
        Function function,
        string sectionName,
        Context context)
    {
        if (sectionName == "__MAIN" && function.Name == "EntryPoint") { return; }
        var assemblySection = function.AssemblySectionsByName[sectionName];
        var midLevelSection = function.MidLevelSectionsByName[sectionName];

        if (assemblySection.Instructions.Any(i => i.Name == "ret")) { return; }

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
                    => context.TempToExpression.TryGetValue(tempSrcVar, out var foundExpression)
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

        Expression? lastCompareExpression = null;
        for (int i = 0; i < assemblySection.Instructions.Length; i++)
        {
            Variable? destVar;
            Expression lhsExpression;
            Expression rhsExpression;
            Expression srcExpression;
            Instruction prevInstruction;
            Instruction nextInstruction;
            Expression st0;
            Expression st1;

            var instruction = assemblySection.Instructions[i];

            bool anyReferencesAfterCurrentInstruction(Variable variable)
                => assemblySection.Instructions.Skip(i + 1).Any(instr =>
                    function.InstructionArgumentToVariable(instr.DestArg) == variable
                    || function.InstructionArgumentToVariable(instr.SrcArg1) == variable
                    || function.InstructionArgumentToVariable(instr.SrcArg2) == variable);

            void handleAssignment(Variable destVar, Expression srcExpression)
            {
                if (destVar is Function.DecompGeneratedTempVariable tempDestVar)
                {
                    context.TempToExpression[tempDestVar] = srcExpression;
                }
                else
                {
                    midLevelSection.Statements.Add(new AssignmentStatement(new VariableExpression(destVar), srcExpression));
                }
            }

            void handleSetcc(Func<Expression> constructor)
            {
                nextInstruction = assemblySection.Instructions[i + 1];
                if (instruction.DestArg != "al" || nextInstruction is not { Name: "movzx", SrcArg1: "al" })
                {
                    Debugger.Break();
                }
                destVar = function.InstructionArgumentToVariable(nextInstruction.DestArg);
                if (destVar is null) { return; }
                handleAssignment(destVar, constructor());
                i++;
            }

            switch (instruction.Name)
            {
                case "jz" or "je":
                    midLevelSection.Statements.Add(new JumpIfZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jnz" or "jne":
                    midLevelSection.Statements.Add(new JumpIfNotZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jge":
                    midLevelSection.Statements.Add(new JumpIfGreaterThanOrEqualToZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jg":
                    midLevelSection.Statements.Add(new JumpIfGreaterThanZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jle":
                    midLevelSection.Statements.Add(new JumpIfLessThanOrEqualToZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jl":
                    midLevelSection.Statements.Add(new JumpIfLessThanZeroStatement(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}"), function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jmp":
                    midLevelSection.Statements.Add(new UnconditionalJumpStatement(function.MidLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "setz" or "sete":
                    handleSetcc(() => new OneIfZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
                    break;
                case "setnz" or "setne":
                    handleSetcc(() => new OneIfNotZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
                    break;
                case "setl" or "setb":
                    handleSetcc(() => new OneIfLessThanZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
                    break;
                case "setle" or "setbe":
                    handleSetcc(() => new OneIfLessThanOrEqualToZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
                    break;
                case "setg" or "seta":
                    handleSetcc(() => new OneIfGreaterThanZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
                    break;
                case "setge" or "setae":
                    handleSetcc(() => new OneIfGreaterThanOrEqualToZeroExpression(lastCompareExpression ?? throw new Exception($"No expression ready for {instruction}")));
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
                case "sub":
                    if (instruction.DestArg == "esp") { continue; }

                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new SubtractExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "and":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new AndExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "or":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new OrExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "xor":
                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new XorExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "cmp":
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new SubtractExpression(lhsExpression, rhsExpression);
                    break;
                case "add":
                    if (instructionArgToExpression(instruction.SrcArg1) is ConstantExpression { Value: var dimSymbol }
                        && DimArray.TryFindByName(dimSymbol) is { } dimArray
                        && instruction.SrcArg2.TryHexToUint32(out var dimSymbolOffset)
                        && function.InstructionArgumentToVariable(instruction.DestArg) is Function.DecompGeneratedTempVariable dimSizeDestVar)
                    {
                        context.VarToDimSizeAssignment[dimSizeDestVar] = new DimSizeAssignment(dimArray, (int)((dimSymbolOffset - 0xc) >> 2));
                    }

                    destVar = function.InstructionArgumentToVariable(instruction.DestArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg2);
                    lastCompareExpression = new AddExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "imul":
                    (string destArg, string srcArg1, string srcArg2) = string.IsNullOrEmpty(instruction.SrcArg2)
                        ? (instruction.DestArg, instruction.DestArg, instruction.SrcArg1)
                        : (instruction.DestArg, instruction.SrcArg1, instruction.SrcArg2);

                    destVar = function.InstructionArgumentToVariable(destArg);
                    if (destVar is null) { continue; }

                    lhsExpression = instructionArgToExpression(srcArg1);
                    rhsExpression = instructionArgToExpression(srcArg2);
                    lastCompareExpression = new MultiplyExpression(lhsExpression, rhsExpression);

                    handleAssignment(destVar, lastCompareExpression);
                    break;
                case "call":
                    var assignmentIndices = instruction.CallParameterAssignmentIndices
                        ?? throw new Exception($"CallParameterAssignmentIndices is null for {instruction}");
                    var returnOutputVar = instruction.ReturnOutputVar 
                        ?? throw new Exception($"ReturnOutputVar is null for {instruction}");

                    var callee = Function.GetFunctionByName(instruction.DestArg);
                    if (callee.AssemblySections.Count > 0
                        || callee.Name.EndsWith("__LIBS", StringComparison.Ordinal)
                        || callee.Name.StartsWith("_builtIn_f", StringComparison.Ordinal)
                        || callee.Name is "_builtIn__bbStrCompare" or "_builtIn__bbObjCompare")
                    {
                        var arguments = new List<Expression>();
                        for (int j = 0; j < assignmentIndices.Length; j++)
                        {
                            var assignInstruction = assemblySection.Instructions[assignmentIndices[j]];
                            arguments.Add(instructionArgToExpression(assignInstruction.SrcArg1));
                        }
                        var callExpression = new CallExpression(callee, arguments.ToArray());

                        if (callee.ReturnType == DeclType.Float)
                        {
                            context.FloatStack.Push(callExpression);
                        }

                        context.TempToExpression[returnOutputVar] = callExpression;
                        if (!anyReferencesAfterCurrentInstruction(returnOutputVar))
                        {
                            midLevelSection.Statements.Add(new FreeStandingExpressionStatement(callExpression));
                        }
                    }
                    else if (callee.Name 
                        is "_builtIn__bbStrConst"
                        or "_builtIn__bbStrLoad"
                        or "_builtIn__bbStrToCStr"
                        or "_builtIn__bbCStrToStr"
                        or "_builtIn__bbObjLoad")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        context.TempToExpression[returnOutputVar] = srcExpression;
                    }
                    else if (callee.Name is "_builtIn__bbStrToInt")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        context.TempToExpression[returnOutputVar] = new ConvertToIntExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrToFloat")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        context.TempToExpression[returnOutputVar] = new ConvertToFloatExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrFromInt" or "_builtIn__bbStrFromFloat")
                    {
                        var assignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        context.TempToExpression[returnOutputVar] = new ConvertToStringExpression(srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrConcat")
                    {
                        var destAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToExpression(destAssignInstruction.SrcArg1);
                        var srcAssignInstruction = assemblySection.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpression(srcAssignInstruction.SrcArg1);
                        context.TempToExpression[returnOutputVar] = new AddExpression(lhsExpression, rhsExpression);
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
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        context.TempToExpression[returnOutputVar] = new ConstructorExpression(customType);
                    }
                    else if (callee.Name == "_builtIn__bbDimArray")
                    {
                        var dimAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        var dimAssignExpression = instructionArgToExpression(dimAssignInstruction.SrcArg1);
                        if (dimAssignExpression is not ConstantExpression { Value: var dimSymbol2 })
                        {
                            throw new Exception($"{dimAssignInstruction.SrcArg1} does not resolve to a dim");
                        }
                        var dim = DimArray.TryFindByName(dimSymbol2) ?? throw new Exception($"Dim {dimSymbol2} not found");
                        var sizeAssignmentExpressions = new Expression[dim.NumDimensions];
                        for (int j = 0; j < dim.NumDimensions; j++)
                        {
                            var sizeAssignmentVariable = context.VarToDimSizeAssignment.First(kvp => kvp.Value.Array == dim && kvp.Value.Dimension == j);
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
                    else if (callee.Name is "_builtIn__bbObjEachFirst" or "_builtIn__bbObjEachFirst")
                    {
                        var destAssignInstruction = assemblySection.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToExpression(destAssignInstruction.SrcArg1);
                        if (lhsExpression is not AccessExpression accessExpression)
                        {
                            throw new Exception($"{destAssignInstruction.SrcArg1} does not resolve to an access expression");
                        }
                        var srcAssignInstruction = assemblySection.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpression(srcAssignInstruction.SrcArg1);
                        if (rhsExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{srcAssignInstruction.SrcArg1} does not resolve to a type");
                        }

                        midLevelSection.Statements.Add(new ForEachStatement(accessExpression, CustomType.GetTypeWithName(typeName[3..])));

                        var andInstruction = assemblySection.Instructions[i + 1];
                        var jzInstruction = assemblySection.Instructions[i + 2];
                        if (andInstruction.Name != "and"
                            || andInstruction.DestArg != returnOutputVar.Name
                            || andInstruction.SrcArg1 != returnOutputVar.Name
                            || jzInstruction.Name != "jz")
                        {
                            Debugger.Break();
                        }
                        i += 2;
                    }
                    else if (callee.Name is "_builtIn__bbObjEachNext" or "_builtIn__bbObjEachNext2")
                    {
                        midLevelSection.Statements.Add(new NextStatement());

                        var andInstruction = assemblySection.Instructions[i + 1];
                        var jnzInstruction = assemblySection.Instructions[i + 2];
                        if (andInstruction.Name != "and"
                            || andInstruction.DestArg != returnOutputVar.Name
                            || andInstruction.SrcArg1 != returnOutputVar.Name
                            || jnzInstruction.Name != "jnz")
                        {
                            Debugger.Break();
                        }
                        i += 2;
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

                    lhsExpression = context.TempToExpression[tempLhsVar];
                    rhsExpression = context.TempToExpression[tempRhsVar];
                    context.TempToExpression[tempLhsVar] = rhsExpression;
                    context.TempToExpression[tempRhsVar] = lhsExpression;
                    break;
                case "faddp" or "fsubp" or "fsubrp" or "fmulp" or "fdivp" or "fdivrp":
                    if (instruction.DestArg != "st1" || instruction.SrcArg1 != "st0")
                    {
                        Debugger.Break();
                    }

                    st0 = context.FloatStack.Pop();
                    st1 = context.FloatStack.Pop();
                    context.FloatStack.Push(instruction.Name switch
                    {
                        "faddp" => new AddExpression(st1, st0),
                        "fsubp" => new SubtractExpression(st1, st0),
                        "fsubrp" => new SubtractExpression(st0, st1),
                        "fmulp" => new MultiplyExpression(st1, st0),
                        "fdivp" => new DivideExpression(st1, st0),
                        "fdivrp" => new DivideExpression(st0, st1),
                        _ => throw new Exception("unreachable")
                    });
                    break;
                case "fucompp":
                    var fnstswAxInstruction = assemblySection.Instructions[i + 1];
                    var sahfInstruction = assemblySection.Instructions[i + 2];
                    if (fnstswAxInstruction.Name != "fnstsw"
                        || fnstswAxInstruction.DestArg != "ax"
                        || sahfInstruction.Name != "sahf")
                    {
                        Debugger.Break();
                    }

                    st0 = context.FloatStack.Pop();
                    st1 = context.FloatStack.Pop();
                    lastCompareExpression = new SubtractExpression(st0, st1);
                    i += 2;
                    break;
                case "fld" or "fild":
                    prevInstruction = assemblySection.Instructions[i - 1];
                    nextInstruction = assemblySection.Instructions[i + 1];
                    if (prevInstruction.Name != "push" || nextInstruction.Name != "pop")
                    {
                        Debugger.Break();
                    }
                    srcExpression = instructionArgToExpression(prevInstruction.DestArg);
                    if (instruction.Name == "fild")
                    {
                        srcExpression = new ConvertToFloatExpression(srcExpression);
                    }
                    context.FloatStack.Push(srcExpression);

                    destVar = function.InstructionArgumentToVariable(nextInstruction.DestArg);
                    if (destVar is null) { continue; }
                    handleAssignment(destVar, srcExpression);
                    break;
                case "fstp" or "fistp":
                    nextInstruction = assemblySection.Instructions[i + 1];
                    if (nextInstruction.Name != "pop")
                    {
                        Debugger.Break();
                    }
                    srcExpression = context.FloatStack.Pop();
                    if (instruction.Name == "fistp")
                    {
                        srcExpression = new ConvertToIntExpression(srcExpression);
                    }

                    destVar = function.InstructionArgumentToVariable(nextInstruction.DestArg);
                    if (destVar is null) { continue; }
                    handleAssignment(destVar, srcExpression);
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
        var context = new Context(
            TempToExpression: new Dictionary<Function.DecompGeneratedTempVariable, Expression>(),
            VarToDimSizeAssignment: new Dictionary<Function.DecompGeneratedTempVariable, DimSizeAssignment>(),
            FloatStack: new Stack<Expression>());
        foreach (var section in function.AssemblySections)
        {
            ProcessSection(function, section.Name, context);
        }
    }
}