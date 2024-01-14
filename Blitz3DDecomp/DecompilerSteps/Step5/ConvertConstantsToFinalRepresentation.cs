using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class ConvertConstantsToFinalRepresentation
{
    private static void ProcessStatement(Function function, int statementIndex)
    {
        static Expression convertToFinalRepresentation(ConstantExpression expression, DeclType type)
        {
            if (type == DeclType.Int)
            {
                if (expression.Value.TryHexToUint32(out var intValue))
                {
                    return new ConstantExpression("$" + intValue.ToString("X2"));
                }
            }
            if (type == DeclType.Float)
            {
                float floatValue = BitConverter.UInt32BitsToSingle(expression.Value.HexToUint32());
                string stringRepresentation = floatValue.ToString("0.0" + new string('#', 99));
                if (stringRepresentation.IndexOf('.', StringComparison.Ordinal) is >= 0 and var indexOfDecimalPoint)
                {
                    var wholePortion = stringRepresentation[..indexOfDecimalPoint];
                    var fraction = stringRepresentation[(indexOfDecimalPoint+1)..];
                    if (wholePortion.Length <= 3 && fraction.Length >= 4)
                    {
                        var inverseStringRepresentation = (1f / floatValue).ToString("0.0" + new string('#', 99));
                        if (inverseStringRepresentation.Length < stringRepresentation.Length)
                        {
                            return new DivideExpression(
                                Lhs: new ConstantExpression("1.0"),
                                Rhs: new ConstantExpression(inverseStringRepresentation));
                        }
                    }
                }
                return new ConstantExpression(stringRepresentation);
            }
            if (type == DeclType.String)
            {
                if (StringConstants.SymbolToValue.TryGetValue(expression.Value[1..], out var str))
                {
                    return new ConstantExpression($"\"{str}\"");
                }
            }
            return expression;
        }

        static DeclType? extractType(Expression expression)
        {
            switch (expression)
            {
                case AbsExpression absExpression:
                    return extractType(absExpression.OriginalExpression);
                case ArrayAccessExpression arrayAccessExpression:
                    return extractType(arrayAccessExpression.Owner)?.GetElementType();
                case DimAccessExpression dimAccessExpression:
                    return dimAccessExpression.Owner.ElementDeclType;
                case FieldAccessExpression fieldAccessExpression:
                    return fieldAccessExpression.Field.DeclType;
                case VariableExpression variableExpression:
                    return variableExpression.Variable.DeclType;
                case AddExpression addExpression:
                    return extractType(addExpression.Lhs) ?? extractType(addExpression.Rhs);
                case AfterExpression afterExpression:
                    return extractType(afterExpression.OriginalExpression);
                case AndExpression:
                    return DeclType.Int;
                case BeforeExpression beforeExpression:
                    return extractType(beforeExpression.OriginalExpression);
                case CallExpression callExpression:
                    return callExpression.Callee.ReturnType;
                case ConstantExpression:
                    return null;
                case ConstructorExpression constructorExpression:
                    return new DeclType("." + constructorExpression.Type.Name);
                case ConvertHandleToObjectExpression convertHandleToObjectExpression:
                    return new DeclType("." + convertHandleToObjectExpression.ObjectType.Name);
                case ConvertObjectToHandleExpression:
                    return DeclType.Int;
                case ConvertToFloatExpression:
                    return DeclType.Float;
                case ConvertToIntExpression:
                    return DeclType.Int;
                case ConvertToStringExpression:
                    return DeclType.String;
                case DivideExpression divideExpression:
                    return extractType(divideExpression.Lhs) ?? extractType(divideExpression.Rhs);
                case FirstOfTypeExpression firstOfTypeExpression:
                    return new DeclType("." + firstOfTypeExpression.ObjectType.Name);
                case LastOfTypeExpression lastOfTypeExpression:
                    return new DeclType("." + lastOfTypeExpression.ObjectType.Name);
                case ModuloExpression moduloExpression:
                    return extractType(moduloExpression.Lhs) ?? extractType(moduloExpression.Rhs);
                case MultiplyExpression multiplyExpression:
                    return extractType(multiplyExpression.Lhs) ?? extractType(multiplyExpression.Rhs);
                case OneIfExpressionsEqualExpression:
                case OneIfExpressionsNotEqualExpression:
                case OneIfGreaterThanOrEqualToZeroExpression:
                case OneIfGreaterThanZeroExpression:
                case OneIfLessThanOrEqualToZeroExpression:
                case OneIfLessThanZeroExpression:
                case OneIfNotZeroExpression:
                case OneIfZeroExpression:
                case OrExpression:
                case ShiftLeftExpression:
                case ShiftRightSignedExpression:
                case ShiftRightUnsignedExpression:
                    return DeclType.Int;
                case SignExpression signExpression:
                    return extractType(signExpression.OriginalExpression);
                case SignFlipExpression signFlipExpression:
                    return extractType(signFlipExpression.OriginalExpression);
                case SubtractExpression subtractExpression:
                    return extractType(subtractExpression.Lhs) ?? extractType(subtractExpression.Rhs);
                case XorExpression:
                    return DeclType.Int;
                default:
                    return null;
            }
        }

        Expression map(Expression expression, DeclType? declType)
        {
            switch (expression)
            {
                case AbsExpression absExpression:
                    return new AbsExpression(map(absExpression.OriginalExpression, declType));
                case ArrayAccessExpression arrayAccessExpression:
                    return new ArrayAccessExpression(
                        map(arrayAccessExpression.Owner, declType),
                        map(arrayAccessExpression.Index, DeclType.Int));
                case DimAccessExpression dimAccessExpression:
                    return new DimAccessExpression(
                        dimAccessExpression.Owner,
                        dimAccessExpression.Indices.Select(i => map(i, DeclType.Int)).ToArray());
                case FieldAccessExpression fieldAccessExpression:
                    return new FieldAccessExpression(
                        map(fieldAccessExpression.Owner, declType),
                        fieldAccessExpression.Field);
                case VariableExpression variableExpression:
                    return variableExpression;
                case AddExpression addExpression:
                    declType = extractType(addExpression) ?? declType;
                    return new AddExpression(
                        map(addExpression.Lhs, declType),
                        map(addExpression.Rhs, declType));
                case AfterExpression afterExpression:
                    return new AfterExpression(map(afterExpression.OriginalExpression, declType));
                case AndExpression andExpression:
                    return new AndExpression(
                        map(andExpression.Lhs, DeclType.Int),
                        map(andExpression.Rhs, DeclType.Int));
                case BeforeExpression beforeExpression:
                    return new BeforeExpression(map(beforeExpression.OriginalExpression, declType));
                case CallExpression callExpression:
                    var arguments = new Expression[callExpression.Callee.Parameters.Count];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        arguments[i] = map(callExpression.Arguments[i], callExpression.Callee.Parameters[i].DeclType);
                    }
                    return new CallExpression(callExpression.Callee, arguments);
                case ConstantExpression constantExpression:
                    return declType is { } type
                        ? convertToFinalRepresentation(constantExpression, type)
                        : constantExpression;
                case ConstructorExpression constructorExpression:
                    return constructorExpression;
                case ConvertHandleToObjectExpression convertHandleToObjectExpression:
                    return new ConvertHandleToObjectExpression(
                        map(convertHandleToObjectExpression.HandleExpression, DeclType.Int),
                        convertHandleToObjectExpression.ObjectType);
                case ConvertObjectToHandleExpression convertObjectToHandleExpression:
                    return new ConvertObjectToHandleExpression(map(convertObjectToHandleExpression.ObjectExpression, declType));
                case ConvertToFloatExpression convertToFloatExpression:
                    return new ConvertToFloatExpression(map(convertToFloatExpression.OriginalExpression, extractType(convertToFloatExpression.OriginalExpression)));
                case ConvertToIntExpression convertToIntExpression:
                    return new ConvertToIntExpression(map(convertToIntExpression.OriginalExpression, extractType(convertToIntExpression.OriginalExpression)));
                case ConvertToStringExpression convertToStringExpression:
                    return new ConvertToStringExpression(map(convertToStringExpression.OriginalExpression, extractType(convertToStringExpression.OriginalExpression)));
                case DivideExpression divideExpression:
                    declType = extractType(divideExpression) ?? declType;
                    return new DivideExpression(
                        map(divideExpression.Lhs, declType),
                        map(divideExpression.Rhs, declType));
                case FirstOfTypeExpression firstOfTypeExpression:
                    return firstOfTypeExpression;
                case LastOfTypeExpression lastOfTypeExpression:
                    return lastOfTypeExpression;
                case ModuloExpression moduloExpression:
                    declType = extractType(moduloExpression) ?? declType;
                    return new ModuloExpression(
                        map(moduloExpression.Lhs, declType),
                        map(moduloExpression.Rhs, declType));
                case MultiplyExpression multiplyExpression:
                    declType = extractType(multiplyExpression) ?? declType;
                    return new MultiplyExpression(
                        map(multiplyExpression.Lhs, declType),
                        map(multiplyExpression.Rhs, declType));
                case ExponentiationExpression exponentiationExpression:
                    declType = extractType(exponentiationExpression) ?? declType;
                    return new ExponentiationExpression(
                        map(exponentiationExpression.Base, declType),
                        map(exponentiationExpression.Exponent, declType));
                case OneIfExpressionsEqualExpression oneIfExpressionsEqualExpression:
                    declType = extractType(oneIfExpressionsEqualExpression.Lhs) ?? extractType(oneIfExpressionsEqualExpression.Rhs);
                    return new OneIfExpressionsEqualExpression(
                        map(oneIfExpressionsEqualExpression.Lhs, declType),
                        map(oneIfExpressionsEqualExpression.Rhs, declType));
                case OneIfExpressionsNotEqualExpression oneIfExpressionsNotEqualExpression:
                    declType = extractType(oneIfExpressionsNotEqualExpression.Lhs) ?? extractType(oneIfExpressionsNotEqualExpression.Rhs);
                    return new OneIfExpressionsNotEqualExpression(
                        map(oneIfExpressionsNotEqualExpression.Lhs, declType),
                        map(oneIfExpressionsNotEqualExpression.Rhs, declType));
                case OneIfExpressionIsLessThanOtherExpression oneIfExpressionIsLessThanOtherExpression:
                    declType = extractType(oneIfExpressionIsLessThanOtherExpression.Lhs) ?? extractType(oneIfExpressionIsLessThanOtherExpression.Rhs);
                    return new OneIfExpressionIsLessThanOtherExpression(
                        map(oneIfExpressionIsLessThanOtherExpression.Lhs, declType),
                        map(oneIfExpressionIsLessThanOtherExpression.Rhs, declType));
                case OneIfExpressionIsLessThanOrEqualToOtherExpression oneIfExpressionIsLessThanOrEqualToOtherExpression:
                    declType = extractType(oneIfExpressionIsLessThanOrEqualToOtherExpression.Lhs) ?? extractType(oneIfExpressionIsLessThanOrEqualToOtherExpression.Rhs);
                    return new OneIfExpressionIsLessThanOrEqualToOtherExpression(
                        map(oneIfExpressionIsLessThanOrEqualToOtherExpression.Lhs, declType),
                        map(oneIfExpressionIsLessThanOrEqualToOtherExpression.Rhs, declType));
                case OneIfExpressionIsGreaterThanOtherExpression oneIfExpressionIsGreaterThanOtherExpression:
                    declType = extractType(oneIfExpressionIsGreaterThanOtherExpression.Lhs) ?? extractType(oneIfExpressionIsGreaterThanOtherExpression.Rhs);
                    return new OneIfExpressionIsGreaterThanOtherExpression(
                        map(oneIfExpressionIsGreaterThanOtherExpression.Lhs, declType),
                        map(oneIfExpressionIsGreaterThanOtherExpression.Rhs, declType));
                case OneIfExpressionIsGreaterThanOrEqualToOtherExpression oneIfExpressionIsGreaterThanOrEqualToOtherExpression:
                    declType = extractType(oneIfExpressionIsGreaterThanOrEqualToOtherExpression.Lhs) ?? extractType(oneIfExpressionIsGreaterThanOrEqualToOtherExpression.Rhs);
                    return new OneIfExpressionIsGreaterThanOrEqualToOtherExpression(
                        map(oneIfExpressionIsGreaterThanOrEqualToOtherExpression.Lhs, declType),
                        map(oneIfExpressionIsGreaterThanOrEqualToOtherExpression.Rhs, declType));
                case OneIfGreaterThanOrEqualToZeroExpression oneIfGreaterThanOrEqualToZeroExpression:
                    declType = extractType(oneIfGreaterThanOrEqualToZeroExpression.OriginalExpression);
                    return new OneIfGreaterThanOrEqualToZeroExpression(
                        map(oneIfGreaterThanOrEqualToZeroExpression.OriginalExpression, declType));
                case OneIfGreaterThanZeroExpression oneIfGreaterThanZeroExpression:
                    declType = extractType(oneIfGreaterThanZeroExpression.OriginalExpression);
                    return new OneIfGreaterThanZeroExpression(
                        map(oneIfGreaterThanZeroExpression.OriginalExpression, declType));
                case OneIfLessThanOrEqualToZeroExpression oneIfLessThanOrEqualToZeroExpression:
                    declType = extractType(oneIfLessThanOrEqualToZeroExpression.OriginalExpression);
                    return new OneIfLessThanOrEqualToZeroExpression(
                        map(oneIfLessThanOrEqualToZeroExpression.OriginalExpression, declType));
                case OneIfLessThanZeroExpression oneIfLessThanZeroExpression:
                    declType = extractType(oneIfLessThanZeroExpression.OriginalExpression);
                    return new OneIfLessThanZeroExpression(
                        map(oneIfLessThanZeroExpression.OriginalExpression, declType));
                case OneIfNotZeroExpression oneIfNotZeroExpression:
                    declType = extractType(oneIfNotZeroExpression.OriginalExpression);
                    return new OneIfNotZeroExpression(
                        map(oneIfNotZeroExpression.OriginalExpression, declType));
                case OneIfZeroExpression oneIfZeroExpression:
                    declType = extractType(oneIfZeroExpression.OriginalExpression);
                    return new OneIfZeroExpression(
                        map(oneIfZeroExpression.OriginalExpression, declType));
                case OrExpression orExpression:
                    return new OrExpression(
                        map(orExpression.Lhs, DeclType.Int),
                        map(orExpression.Rhs, DeclType.Int));
                case ShiftLeftExpression shiftLeftExpression:
                    return new ShiftLeftExpression(
                        map(shiftLeftExpression.Lhs, DeclType.Int),
                        map(shiftLeftExpression.Rhs, DeclType.Int));
                case ShiftRightSignedExpression shiftRightSignedExpression:
                    return new ShiftRightSignedExpression(
                        map(shiftRightSignedExpression.Lhs, DeclType.Int),
                        map(shiftRightSignedExpression.Rhs, DeclType.Int));
                case ShiftRightUnsignedExpression shiftRightUnsignedExpression:
                    return new ShiftRightUnsignedExpression(
                        map(shiftRightUnsignedExpression.Lhs, DeclType.Int),
                        map(shiftRightUnsignedExpression.Rhs, DeclType.Int));
                case SignExpression signExpression:
                    declType = extractType(signExpression.OriginalExpression);
                    return new SignExpression(map(signExpression.OriginalExpression, declType));
                case SignFlipExpression signFlipExpression:
                    declType = extractType(signFlipExpression.OriginalExpression);
                    return new SignFlipExpression(map(signFlipExpression.OriginalExpression, declType));
                case SubtractExpression subtractExpression:
                    declType = extractType(subtractExpression) ?? declType;
                    return new SubtractExpression(
                        map(subtractExpression.Lhs, declType),
                        map(subtractExpression.Rhs, declType));
                case XorExpression xorExpression:
                    return new XorExpression(
                        map(xorExpression.Lhs, DeclType.Int),
                        map(xorExpression.Rhs, DeclType.Int));
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
        }

        var statement = function.HighLevelStatements[statementIndex];
        function.HighLevelStatements[statementIndex] = statement switch
        {
            AllocateDimStatement allocateDimStatement
                => new AllocateDimStatement(
                    allocateDimStatement.Dim,
                    allocateDimStatement.Dimensions.Select(expr => map(expr, DeclType.Int)).ToArray()),
            AssignmentStatement assignmentStatement
                => new AssignmentStatement(
                    map(assignmentStatement.Destination, null) as AccessExpression
                        ?? throw new Exception("map did not return an AccessExpression"),
                    map(assignmentStatement.Source, extractType(assignmentStatement.Destination))),
            DeleteEachStatement deleteEachStatement
                => deleteEachStatement,
            DestructorStatement destructorStatement
                => new DestructorStatement(map(destructorStatement.ObjectExpression, null)),
            ForEachStatement forEachStatement
                => new ForEachStatement(
                    map(forEachStatement.Iterator, null) as AccessExpression
                        ?? throw new Exception("map did not return an AccessExpression"),
                    forEachStatement.Type),
            FreeStandingExpressionStatement freeStandingExpressionStatement
                => new FreeStandingExpressionStatement(map(freeStandingExpressionStatement.Expression, null)),
            InsertAfterStatement insertAfterStatement
                => new InsertAfterStatement(
                    map(insertAfterStatement.ObjectToInsert, null),
                    map(insertAfterStatement.ObjectThatComesBefore, null)),
            InsertBeforeStatement insertBeforeStatement
                => new InsertBeforeStatement(
                    map(insertBeforeStatement.ObjectToInsert, null),
                    map(insertBeforeStatement.ObjectThatComesAfter, null)),
            JumpIfExpressionStatement jumpIfExpressionStatement
                => new JumpIfExpressionStatement(
                    map(jumpIfExpressionStatement.Expression, null),
                    jumpIfExpressionStatement.Section),
            NextStatement nextStatement
                => nextStatement,
            RestoreStatement restoreStatement
                => restoreStatement,
            ReturnStatement returnStatement
                => new ReturnStatement(map(returnStatement.Expression, function.ReturnType)),
            UnconditionalJumpStatement unconditionalJumpStatement
                => unconditionalJumpStatement,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static void Process(Function function)
    {
        for (var i = 0; i < function.HighLevelStatements.Count; i++)
        {
            ProcessStatement(function, i);
        }
    }
}