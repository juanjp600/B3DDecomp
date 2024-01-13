using System.Diagnostics;
using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.DecompilerSteps.Step5;

static class FixDimIndexer
{
    public static void Process(Function function)
    {
        Expression unshift(Expression index)
        {
            if (index is ShiftRightUnsignedExpression
                {
                    Rhs: ConstantExpression { Value: "0x2" },
                    Lhs: ShiftLeftExpression
                    {
                        Rhs: ConstantExpression { Value: "0x2" },
                        Lhs: var unshifted
                    }
                })
            {
                return unshifted;
            }
            return index;
        }

        DimAccessExpression rewriteDimAccess(DimAccessExpression dimAccessExpression)
        {
            var dim = dimAccessExpression.Owner;
            var dimSymbolName = $"@_a{dim.Name}_";
            dimSymbolName += dim.ElementDeclType == DeclType.Int
                ? "int"
                : dim.ElementDeclType == DeclType.Float
                    ? "float"
                    : dim.ElementDeclType == DeclType.String
                        ? "string"
                        : "obj";
            dimSymbolName += $"_{dim.NumDimensions}dim";
            var dimSymbolExpression = new ConstantExpression(dimSymbolName);

            var indices = dimAccessExpression
                .Indices
                .Select(unshift)
                .ToArray();
            if (dimAccessExpression.Indices.Length > 1
                || dimAccessExpression.Owner.NumDimensions <= 1)
            {
                return new DimAccessExpression(dim, indices);
            }

            var indexExpression = indices[0];
            var newIndices = new Expression?[dimAccessExpression.Owner.NumDimensions];
            for (int i = 0; i < newIndices.Length; i++)
            {
                bool isDimOffsetExpression(Expression expression, out uint offset)
                {
                    offset = uint.MaxValue;
                    return expression is AddExpression { Lhs: var lhs, Rhs: ConstantExpression { Value: var rhsValue } }
                           && lhs == dimSymbolExpression
                           && rhsValue.TryHexToUint32(out offset);
                }

                Expression replaceOffsetExpressionWithOne(Expression expression)
                {
                    if (isDimOffsetExpression(expression, out _))
                    {
                        return new ConstantExpression("0x1");
                    }
                    return expression;
                }

                bool foundNonZeroIndex = false;
                if (indexExpression is AddExpression { Lhs: var lhs, Rhs: var rhs })
                {
                    foreach (var innerExpression in lhs.InnerExpressions)
                    {
                        if (isDimOffsetExpression(innerExpression, out var offset))
                        {
                            indexExpression = rhs;
                            uint unwrappedIndexNum = ((offset - 0xc) >> 2) + 1;
                            newIndices[unwrappedIndexNum] = lhs.Map(replaceOffsetExpressionWithOne);
                            foundNonZeroIndex = true;
                            break;
                        }
                    }
                    foreach (var innerExpression in rhs.InnerExpressions)
                    {
                        if (isDimOffsetExpression(innerExpression, out var offset))
                        {
                            indexExpression = lhs;
                            uint unwrappedIndexNum = ((offset - 0xc) >> 2) + 1;
                            newIndices[unwrappedIndexNum] = rhs.Map(replaceOffsetExpressionWithOne);
                            foundNonZeroIndex = true;
                            break;
                        }
                    }
                }
                if (!foundNonZeroIndex)
                {
                    newIndices[0] = indexExpression;
                    break;
                }
            }

            if (newIndices.Contains(null)) { throw new Exception($"At least one index not set"); }
            return new DimAccessExpression(dim, newIndices.OfType<Expression>().ToArray());
        }

        for (int i = 0; i < function.HighLevelStatements.Count; i++)
        {
            var statement = function.HighLevelStatements[i];
            function.HighLevelStatements[i] = statement.Map(
                stmt => stmt,
                expr => expr switch
                {
                    DimAccessExpression dimAccessExpression
                        => rewriteDimAccess(dimAccessExpression),
                    _
                        => expr
                });
        }
    }
}