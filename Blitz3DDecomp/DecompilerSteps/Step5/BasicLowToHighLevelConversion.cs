using System.Diagnostics;
using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class BasicLowToHighLevelConversion
{
    private readonly record struct DimSizeAssignment(DimArray Array, int Dimension);

    private readonly record struct Context(
        Dictionary<Function.DecompGeneratedTempVariable, Expression> TempToExpression,
        Dictionary<DimSizeAssignment, Function.DecompGeneratedTempVariable> DimSizeAssignmentToVar,
        Stack<Expression> FloatStack);

    private static void ProcessSection(
        Function function,
        string sectionName,
        Context context)
    {
        if (sectionName == "__MAIN" && function.Name == "EntryPoint") { return; }
        if (sectionName.StartsWith("SGNZERO", StringComparison.Ordinal)) { return; }
        var assemblySection = function.AssemblySectionsByName[sectionName];
        var highLevelSection = function.HighLevelSectionsByName[sectionName];

        if (assemblySection.Instructions.Any(i => i.Name == "ret")) { return; }

        Expression instructionArgToExpression(string instructionArg)
        {
            Expression indexStrToExpression(string indexStr)
                => indexStr.EndsWith(">>2", StringComparison.Ordinal)
                    ? new ShiftRightUnsignedExpression(instructionArgToExpression(indexStr[..^3]), new ConstantExpression("0x2"))
                    : instructionArgToExpression(indexStr);

            Expression[] deconstructDimIndices(Expression compositeExpression)
            {
                // This is fixed up in FixDimIndexer
                return new[] { compositeExpression };
            }

            var srcVar = function.InstructionArgumentToVariable(instructionArg);

            return srcVar switch
            {
                FieldVariable fieldVar
                    => new FieldAccessExpression(instructionArgToExpression(fieldVar.Owner.Name), fieldVar.TypeField),
                ArrayElementVariable arrayElementVariable
                    => new ArrayAccessExpression(instructionArgToNonTempExpression(arrayElementVariable.Owner.Name), indexStrToExpression(arrayElementVariable.Index)),
                DimArray.AccessVariable dimElementVar
                    => dimElementVar.DimArray.NumDimensions == 1
                        ? new DimAccessExpression(dimElementVar.DimArray, indexStrToExpression(dimElementVar.Index))
                        : new DimAccessExpression(dimElementVar.DimArray, deconstructDimIndices(indexStrToExpression(dimElementVar.Index))),
                not null
                    => new VariableExpression(srcVar),
                null
                    => new ConstantExpression(instructionArg.StripDeref())
            };
        }

        Expression instructionArgToNonTempExpression(string instructionArg)
        {
            var expression = instructionArgToExpression(instructionArg);

            while (expression is VariableExpression { Variable: Function.DecompGeneratedTempVariable tempVar })
            {
                expression = context.TempToExpression[tempVar];
            }

            return expression;
        }

        Expression instructionArgToExpressionRecursive(string instructionArg)
        {
            var expression = instructionArgToExpression(instructionArg);

            Expression reduceExpression(Expression expression)
            {
                if (expression is VariableExpression { Variable: Function.DecompGeneratedTempVariable tempVar })
                {
                    expression = context.TempToExpression[tempVar].Map(reduceExpression);
                }
                return expression;
            }

            return expression.Map(reduceExpression);
        }

        Expression? lastPotentialReturnExpression = null;
        Expression? lastCompareExpression = null;
        for (int i = 0; i < assemblySection.Instructions.Length; i++)
        {
            Expression lhsExpression;
            Expression rhsExpression;
            Expression combinedExpression;
            Expression? srcExpression;
            Instruction prevInstruction;
            Instruction nextInstruction;
            Expression st0;
            Expression st1;

            var instruction = assemblySection.Instructions[i];

            void handleAssignmentToInstructionArg(string instructionArg, Expression srcExpression)
            {
                var destVar = function.InstructionArgumentToVariable(instructionArg);
                var instructionArgStripped = instructionArg.StripDeref();
                bool derefDest = instructionArg != instructionArgStripped
                                 && instructionArgStripped.Length >= 3
                                 && instructionArgStripped[..3].IsRegister();
                handleAssignmentToVar(destVar, srcExpression, derefDest);
            }

            void handleAssignmentToVar(Variable? destVar, Expression srcExpression, bool derefDest = false)
            {
                if (destVar is null) { return; }
                if (!destVar.DeclType.IsArrayType)
                {
                    if (derefDest
                        && destVar is Function.DecompGeneratedTempVariable tempDestVar1
                        && context.TempToExpression[tempDestVar1] is AccessExpression accessExpression)
                    {
                        highLevelSection.Statements.Add(new AssignmentStatement(accessExpression, srcExpression));
                    }
                    else
                    {
                        highLevelSection.Statements.Add(new AssignmentStatement(new VariableExpression(destVar), srcExpression));
                    }
                    if (destVar.Name.StartsWith("eax", StringComparison.Ordinal)) { lastPotentialReturnExpression = new VariableExpression(destVar); }
                }
                if (destVar is Function.DecompGeneratedTempVariable tempDestVar2)
                {
                    context.TempToExpression[tempDestVar2] = srcExpression;
                }
            }

            void handleSetcc(Func<Expression> constructor)
            {
                nextInstruction = assemblySection.Instructions[i + 1];
                if (instruction.DestArg != "al" || nextInstruction is not { Name: "movzx", SrcArg1: "al" })
                {
                    Debugger.Break();
                }
                handleAssignmentToInstructionArg(nextInstruction.DestArg, constructor());
                i++;
            }

            void setCompareExpression(string destArg, Expression combinedExpression)
            {
                lastCompareExpression = function.InstructionArgumentToVariable(destArg) is { } destVar
                    ? new VariableExpression(destVar)
                    : combinedExpression;
            }

            switch (instruction.Name)
            {
                case "jz" or "je"
                        or "jnz" or "jne"
                        or "jg"
                        or "jge"
                        or "jl"
                        or "jle":
                    if (lastCompareExpression is null) { throw new Exception($"No expression ready for {instruction}"); }

                    highLevelSection.Statements.Add(new JumpIfExpressionStatement(instruction.Name switch
                    {
                        "jz" or "je" => new OneIfZeroExpression(lastCompareExpression),
                        "jnz" or "jne" => new OneIfNotZeroExpression(lastCompareExpression),
                        "jg" => new OneIfGreaterThanZeroExpression(lastCompareExpression),
                        "jge" => new OneIfGreaterThanOrEqualToZeroExpression(lastCompareExpression),
                        "jl" => new OneIfLessThanZeroExpression(lastCompareExpression),
                        "jle" => new OneIfLessThanOrEqualToZeroExpression(lastCompareExpression),
                        _ => throw new Exception("unreachable")
                    }, function.HighLevelSectionsByName[instruction.DestArg[1..]]));
                    break;
                case "jmp":
                    var jmpSection = function.HighLevelSectionsByName[instruction.DestArg[1..]];
                    if (jmpSection.Name.EndsWith($"_leave_f{function.Name}", StringComparison.Ordinal)
                        && lastPotentialReturnExpression != null)
                    {
                        highLevelSection.Statements.Add(new ReturnStatement(lastPotentialReturnExpression));
                    }
                    else
                    {
                        highLevelSection.Statements.Add(new UnconditionalJumpStatement(jmpSection));
                    }
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
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignmentToInstructionArg(instruction.DestArg, new ShiftLeftExpression(lhsExpression, rhsExpression));
                    break;
                case "shr":
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignmentToInstructionArg(instruction.DestArg, new ShiftRightUnsignedExpression(lhsExpression, rhsExpression));
                    break;
                case "sar":
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignmentToInstructionArg(instruction.DestArg, new ShiftRightSignedExpression(lhsExpression, rhsExpression));
                    break;
                case "sub" or "and" or "or" or "xor":
                    if (instruction.Name == "sub" && instruction.DestArg == "esp") { continue; }

                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    combinedExpression = instruction.Name switch
                    {
                        "sub" => new SubtractExpression(lhsExpression, rhsExpression),
                        "and" => new AndExpression(lhsExpression, rhsExpression),
                        "or" => new OrExpression(lhsExpression, rhsExpression),
                        "xor" => new XorExpression(lhsExpression, rhsExpression),
                        _ => throw new Exception("unreachable")
                    };
                    setCompareExpression(instruction.DestArg, combinedExpression);
                    handleAssignmentToInstructionArg(instruction.DestArg, combinedExpression);
                    break;
                case "cmp":
                    lhsExpression = instructionArgToExpression(instruction.DestArg);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    lastCompareExpression = new SubtractExpression(lhsExpression, rhsExpression);
                    break;
                case "add":
                    if (instructionArgToNonTempExpression(instruction.SrcArg1) is ConstantExpression { Value: var dimSymbol }
                        && DimArray.TryFindByName(dimSymbol) is { } dimArray
                        && instruction.SrcArg2.TryHexToUint32(out var dimSymbolOffset)
                        && function.InstructionArgumentToVariable(instruction.DestArg) is Function.DecompGeneratedTempVariable dimSizeDestVar)
                    {
                        context.DimSizeAssignmentToVar[new DimSizeAssignment(dimArray, (int)((dimSymbolOffset - 0xc) >> 2))] = dimSizeDestVar;
                    }

                    lhsExpression = instructionArgToExpression(instruction.SrcArg1);
                    rhsExpression = instructionArgToExpression(instruction.SrcArg2);
                    combinedExpression = new AddExpression(lhsExpression, rhsExpression);
                    setCompareExpression(instruction.DestArg, combinedExpression);
                    handleAssignmentToInstructionArg(instruction.DestArg, combinedExpression);
                    break;
                case "imul":
                    (string destArg, string srcArg1, string srcArg2) = string.IsNullOrEmpty(instruction.SrcArg2)
                        ? (instruction.DestArg, instruction.DestArg, instruction.SrcArg1)
                        : (instruction.DestArg, instruction.SrcArg1, instruction.SrcArg2);

                    lhsExpression = instructionArgToExpression(srcArg1);
                    rhsExpression = instructionArgToExpression(srcArg2);
                    combinedExpression = new MultiplyExpression(lhsExpression, rhsExpression);
                    setCompareExpression(instruction.DestArg, combinedExpression);
                    handleAssignmentToInstructionArg(destArg, combinedExpression);
                    break;
                case "cdq":
                    var signExtensionSignVar = instruction.SignExtensionSignVar ?? throw new Exception("SignExtensionSignVar is null");
                    var signExtensionValueVar = instruction.SignExtensionValueVar ?? throw new Exception("SignExtensionValueVar is null");
                    handleAssignmentToVar(signExtensionSignVar, new SignFlipExpression(new OneIfLessThanZeroExpression(new VariableExpression(signExtensionValueVar))));
                    break;
                case "idiv":
                    var divResultVar = instruction.DivResultVar ?? throw new Exception("SignExtensionSignVar is null");
                    var divRemainderVar = instruction.DivRemainderVar ?? throw new Exception("SignExtensionValueVar is null");
                    if (function.InstructionArgumentToVariable(instruction.DestArg) is
                        Function.DecompGeneratedTempVariable divisorVar)
                    {
                        lhsExpression = new VariableExpression(divResultVar);
                        rhsExpression = new VariableExpression(divisorVar);
                        handleAssignmentToVar(divRemainderVar, new ModuloExpression(lhsExpression, rhsExpression));
                        handleAssignmentToVar(divResultVar, new DivideExpression(lhsExpression, rhsExpression));
                    }
                    else
                    {
                        throw new Exception($"{instruction.DestArg} does not resolve to a variable");
                    }
                    break;
                case "call":
                    var assignmentIndices = instruction.CallParameterAssignmentIndices
                        ?? throw new Exception($"CallParameterAssignmentIndices is null for {instruction}");
                    var calleeReturnOutputVar = instruction.ReturnOutputVar 
                        ?? throw new Exception($"ReturnOutputVar is null for {instruction}");

                    var callee = Function.GetFunctionByName(instruction.DestArg);
                    if (callee.AssemblySections.Length > 0
                        || callee.Name.EndsWith("__LIBS", StringComparison.Ordinal)
                        || callee.Name.StartsWith("_builtIn_f", StringComparison.Ordinal)
                        || callee.Name is
                            "_builtIn__bbStrCompare" or "_builtIn__bbObjCompare"
                            or "_builtIn__bbReadInt" or "_builtIn__bbReadFloat" or "_builtIn__bbReadStr"
                            or "_builtIn__bbMod" or "_builtIn__bbFMod"
                            or "_builtIn__bbAbs" or "_builtIn__bbFAbs"
                            or "_builtIn__bbSgn" or "_builtIn__bbFSgn"
                            or "_builtIn__bbFPow"
                            or "_builtIn__bbStrConcat"
                            or "_builtIn__bbStrToInt"
                            or "_builtIn__bbStrToFloat"
                            or "_builtIn__bbStrFromInt" or "_builtIn__bbStrFromFloat")
                    {
                        var arguments = new List<Expression>();
                        for (int j = 0; j < assignmentIndices.Length; j++)
                        {
                            var assignInstruction = function.Instructions[assignmentIndices[j]];
                            arguments.Add(instructionArgToExpression(assignInstruction.SrcArg1));
                        }

                        if (callee.ReturnType == DeclType.Float)
                        {
                            context.FloatStack.Push(new VariableExpression(calleeReturnOutputVar));
                        }
                        var callExpression = new CallExpression(callee, arguments.ToArray());
                        handleAssignmentToVar(calleeReturnOutputVar, callExpression);
                    }
                    else if (callee.Name 
                        is "_builtIn__bbStrConst"
                        or "_builtIn__bbStrLoad"
                        or "_builtIn__bbStrToCStr"
                        or "_builtIn__bbCStrToStr"
                        or "_builtIn__bbStrFromCStr"
                        or "_builtIn__bbStrTmp"
                        or "_builtIn__bbStrRetain"
                        or "_builtIn__bbObjLoad"
                        or "_builtIn__bbIStrTmpFree"
                        or "_builtIn__bbFStrTmpFree")
                    {
                        var assignInstruction = function.Instructions[assignmentIndices[0]];
                        srcExpression = instructionArgToExpression(assignInstruction.SrcArg1);
                        handleAssignmentToVar(calleeReturnOutputVar, srcExpression);
                    }
                    else if (callee.Name is "_builtIn__bbStrStore" or "_builtIn__bbObjStore")
                    {
                        var destAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var srcAssignInstruction = function.Instructions[assignmentIndices[1]];
                        srcExpression = instructionArgToExpression(srcAssignInstruction.SrcArg1);
                        handleAssignmentToInstructionArg($"[{destAssignInstruction.SrcArg1}]", srcExpression);
                    }
                    else if (callee.Name == "_builtIn__bbObjNew")
                    {
                        var typeAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var typeAssignExpression = instructionArgToExpressionRecursive(typeAssignInstruction.SrcArg1);
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        handleAssignmentToVar(calleeReturnOutputVar, new ConstructorExpression(customType));
                    }
                    else if (callee.Name == "_builtIn__bbDimArray")
                    {
                        var dimAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var dimAssignExpression = instructionArgToNonTempExpression(dimAssignInstruction.SrcArg1);
                        if (dimAssignExpression is not ConstantExpression { Value: var dimSymbol2 })
                        {
                            throw new Exception($"{dimAssignInstruction.SrcArg1} does not resolve to a dim");
                        }
                        var dim = DimArray.TryFindByName(dimSymbol2) ?? throw new Exception($"Dim {dimSymbol2} not found");
                        var sizeAssignmentExpressions = new Expression[dim.NumDimensions];
                        for (int j = 0; j < dim.NumDimensions; j++)
                        {
                            var sizeAssignmentVariable = context.DimSizeAssignmentToVar.First(kvp => kvp.Key.Array == dim && kvp.Key.Dimension == j);
                            sizeAssignmentExpressions[j] = instructionArgToExpression(sizeAssignmentVariable.Value.Name);
                        }
                        highLevelSection.Statements.Add(new AllocateDimStatement(dim, sizeAssignmentExpressions));
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
                        var destAssignInstruction = function.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToNonTempExpression(destAssignInstruction.SrcArg1);
                        if (lhsExpression is not AccessExpression accessExpression)
                        {
                            throw new Exception($"{destAssignInstruction.SrcArg1} does not resolve to an access expression");
                        }
                        var srcAssignInstruction = function.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpressionRecursive(srcAssignInstruction.SrcArg1);
                        if (rhsExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{srcAssignInstruction.SrcArg1} does not resolve to a type");
                        }

                        highLevelSection.Statements.Add(new ForEachStatement(accessExpression, CustomType.GetTypeWithName(typeName[3..])));

                        var andInstruction = assemblySection.Instructions[i + 1];
                        var jzInstruction = assemblySection.Instructions[i + 2];
                        if (andInstruction.Name != "and"
                            || andInstruction.DestArg != calleeReturnOutputVar.Name
                            || andInstruction.SrcArg1 != calleeReturnOutputVar.Name
                            || jzInstruction.Name != "jz")
                        {
                            Debugger.Break();
                        }
                        i += 2;
                    }
                    else if (callee.Name is "_builtIn__bbObjEachNext" or "_builtIn__bbObjEachNext2")
                    {
                        highLevelSection.Statements.Add(new NextStatement());

                        var andInstruction = assemblySection.Instructions[i + 1];
                        var jnzInstruction = assemblySection.Instructions[i + 2];
                        if (andInstruction.Name != "and"
                            || andInstruction.DestArg != calleeReturnOutputVar.Name
                            || andInstruction.SrcArg1 != calleeReturnOutputVar.Name
                            || jnzInstruction.Name != "jnz")
                        {
                            Debugger.Break();
                        }
                        i += 2;
                    }
                    else if (callee.Name == "_builtIn__bbObjFirst")
                    {
                        var typeAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var typeAssignExpression = instructionArgToExpressionRecursive(typeAssignInstruction.SrcArg1);
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        handleAssignmentToVar(calleeReturnOutputVar, new FirstOfTypeExpression(customType));
                    }
                    else if (callee.Name == "_builtIn__bbObjLast")
                    {
                        var typeAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var typeAssignExpression = instructionArgToExpressionRecursive(typeAssignInstruction.SrcArg1);
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        handleAssignmentToVar(calleeReturnOutputVar, new LastOfTypeExpression(customType));
                    }
                    else if (callee.Name == "_builtIn__bbRestore")
                    {
                        var offsetAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var offsetExpression = instructionArgToExpressionRecursive(offsetAssignInstruction.SrcArg1);
                        if (offsetExpression is AddExpression { Lhs: ConstantExpression { Value: "@__DATA" }, Rhs: ConstantExpression { Value: var offsetStr } }
                            && offsetStr.TryHexToUint32(out var offset))
                        {
                            highLevelSection.Statements.Add(new RestoreStatement($"DATA_{offset:X8}"));
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                    else if (callee.Name == "_builtIn__bbObjToHandle")
                    {
                        var objectAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var objectExpression = instructionArgToExpression(objectAssignInstruction.SrcArg1);

                        handleAssignmentToVar(calleeReturnOutputVar, new ConvertObjectToHandleExpression(objectExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjFromHandle")
                    {
                        var destAssignInstruction = function.Instructions[assignmentIndices[0]];
                        lhsExpression = instructionArgToExpression(destAssignInstruction.SrcArg1);
                        var srcAssignInstruction = function.Instructions[assignmentIndices[1]];
                        rhsExpression = instructionArgToExpressionRecursive(srcAssignInstruction.SrcArg1);
                        if (rhsExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{srcAssignInstruction.SrcArg1} does not resolve to a type");
                        }

                        handleAssignmentToVar(calleeReturnOutputVar, new ConvertHandleToObjectExpression(lhsExpression, CustomType.GetTypeWithName(typeName[3..])));
                    }
                    else if (callee.Name == "_builtIn__bbObjDelete")
                    {
                        var objectAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var objectExpression = instructionArgToExpression(objectAssignInstruction.SrcArg1);

                        highLevelSection.Statements.Add(new DestructorStatement(objectExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjInsBefore")
                    {
                        var objectToInsertAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var objectToInsertExpression = instructionArgToExpression(objectToInsertAssignInstruction.SrcArg1);
                        var objectThatComesAfterAssignInstruction = function.Instructions[assignmentIndices[1]];
                        var objectThatComesAfterExpression = instructionArgToExpression(objectThatComesAfterAssignInstruction.SrcArg1);

                        highLevelSection.Statements.Add(new InsertBeforeStatement(objectToInsertExpression, objectThatComesAfterExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjInsAfter")
                    {
                        var objectToInsertAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var objectToInsertExpression = instructionArgToExpression(objectToInsertAssignInstruction.SrcArg1);
                        var objectThatComesBeforeAssignInstruction = function.Instructions[assignmentIndices[1]];
                        var objectThatComesBeforeExpression = instructionArgToExpression(objectThatComesBeforeAssignInstruction.SrcArg1);

                        highLevelSection.Statements.Add(new InsertAfterStatement(objectToInsertExpression, objectThatComesBeforeExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjDeleteEach")
                    {
                        var typeAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var typeAssignExpression = instructionArgToExpressionRecursive(typeAssignInstruction.SrcArg1);
                        if (typeAssignExpression is not ConstantExpression { Value: var typeName }
                            || !typeName.StartsWith("@_t", StringComparison.Ordinal))
                        {
                            throw new Exception($"{typeAssignInstruction.SrcArg1} does not resolve to a type");
                        }
                        var customType = CustomType.GetTypeWithName(typeName[3..]);
                        highLevelSection.Statements.Add(new DeleteEachStatement(customType));
                    }
                    else if (callee.Name == "_builtIn__bbObjNext")
                    {
                        var argAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var argAssignExpression = instructionArgToExpression(argAssignInstruction.SrcArg1);
                        handleAssignmentToVar(calleeReturnOutputVar, new AfterExpression(argAssignExpression));
                    }
                    else if (callee.Name == "_builtIn__bbObjPrev")
                    {
                        var argAssignInstruction = function.Instructions[assignmentIndices[0]];
                        var argAssignExpression = instructionArgToExpression(argAssignInstruction.SrcArg1);
                        handleAssignmentToVar(calleeReturnOutputVar, new BeforeExpression(argAssignExpression));
                    }
                    else if (callee.Name is
                        "_builtIn__bbUndimArray"
                        or "_builtIn__bbStrRelease"
                        or "_builtIn__bbObjRelease"
                        or "_builtIn__bbStrTmpFree")
                    {
                        continue;
                    }
                    else
                    {
                        Debugger.Break();
                    }
                    break;
                case "mov" or "lea":
                    if (instruction.SrcArg1.IsRegister()) { continue; }

                    srcExpression = instructionArgToExpression(instruction.SrcArg1);
                    handleAssignmentToInstructionArg(instruction.DestArg, srcExpression);
                    break;
                case "xchg":
                    var lhsVarPrev = function.InstructionArgumentToVariable(instruction.DestArg);
                    var rhsVarPrev = function.InstructionArgumentToVariable(instruction.SrcArg1);
                    if (lhsVarPrev is not Function.DecompGeneratedTempVariable tempLhsVarPrev
                        || rhsVarPrev is not Function.DecompGeneratedTempVariable tempRhsVarPrev
                        || instruction.XchgLhsPost is not { } tempLhsVarPost
                        || instruction.XchgRhsPost is not { } tempRhsVarPost)
                    {
                        Debugger.Break();
                        continue;
                    }

                    handleAssignmentToVar(tempRhsVarPost, new VariableExpression(tempLhsVarPrev));
                    handleAssignmentToVar(tempLhsVarPost, new VariableExpression(tempRhsVarPrev));
                    break;
                case "fldz":
                    if (assemblySection.Instructions[i + 1].Name != "fucompp"
                        || assemblySection.Instructions[i + 2].Name != "fnstsw"
                        || assemblySection.Instructions[i + 3].Name != "sahf"
                        || assemblySection.Instructions[i + 4].Name != "jz"
                        || assemblySection.Instructions[i + 5].Name != "fld1"
                        || assemblySection.Instructions[i + 6].Name != "jbe"
                        || assemblySection.Instructions[i + 7].Name != "fchs"
                        || assemblySection.Instructions[i + 8].Name != "jmp")
                    {
                        Debugger.Break();
                    }
                    i += 8;
                    st0 = context.FloatStack.Pop();
                    context.FloatStack.Push(new SignExpression(st0));
                    break;
                case "fabs":
                    st0 = context.FloatStack.Pop();
                    context.FloatStack.Push(new AbsExpression(st0));
                    break;
                case "fmul":
                    if (instruction.DestArg != "st0" || instruction.SrcArg1 != "st0")
                    {
                        Debugger.Break();
                    }
                    st0 = context.FloatStack.Pop();
                    context.FloatStack.Push(new MultiplyExpression(st0, st0));
                    break;
                case "faddp" or "fsubp" or "fsubrp" or "fmulp" or "fdivp" or "fdivrp":
                    if (instruction.DestArg != "st1" || instruction.SrcArg1 != "st0")
                    {
                        Debugger.Break();
                    }

                    st0 = context.FloatStack.Pop();
                    st1 = context.FloatStack.Pop();
                    st0 = instruction.Name switch
                    {
                        "faddp" => new AddExpression(st1, st0),
                        "fsubp" => new SubtractExpression(st1, st0),
                        "fsubrp" => new SubtractExpression(st0, st1),
                        "fmulp" => new MultiplyExpression(st1, st0),
                        "fdivp" => new DivideExpression(st1, st0),
                        "fdivrp" => new DivideExpression(st0, st1),
                        _ => throw new Exception("unreachable")
                    };
                    if (function.ReturnType == DeclType.Float)
                    {
                        lastPotentialReturnExpression = st0;
                    }
                    context.FloatStack.Push(st0);
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

                    handleAssignmentToInstructionArg(nextInstruction.DestArg, srcExpression);

                    if (function.ReturnType == DeclType.Float
                        && function.InstructionArgumentToVariable(nextInstruction.DestArg) is { } destVar)
                    {
                        lastPotentialReturnExpression = new VariableExpression(destVar);
                    }
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

                    handleAssignmentToInstructionArg(nextInstruction.DestArg, srcExpression);
                    break;
                case "fchs":
                    st0 = context.FloatStack.Pop();
                    context.FloatStack.Push(new SignFlipExpression(st0));
                    break;
                case "neg":
                    srcExpression = instructionArgToExpression(instruction.DestArg);
                    handleAssignmentToInstructionArg(instruction.DestArg, new SignFlipExpression(srcExpression));
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
            DimSizeAssignmentToVar: new Dictionary<DimSizeAssignment, Function.DecompGeneratedTempVariable>(),
            FloatStack: new Stack<Expression>());
        foreach (var section in function.AssemblySections)
        {
            ProcessSection(function, section.Name, context);
        }
    }
}