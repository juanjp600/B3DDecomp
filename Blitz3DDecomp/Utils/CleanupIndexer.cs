using B3DDecompUtils;
using Blitz3DDecomp.HighLevel;

namespace Blitz3DDecomp.Utils;

static class CleanupIndexer
{
    public static Expression Process(Expression expression)
    {
        switch (expression)
        {
            case ShiftRightUnsignedExpression { Lhs: ConstantExpression lhs, Rhs: ConstantExpression rhs }
                when lhs.Value.TryHexToUint32(out var lhsInt) && rhs.Value.TryHexToUint32(out var rhsInt):
                return new ConstantExpression($"0x{(lhsInt >> (int)rhsInt):X1}");
            case ShiftLeftExpression { Lhs: ConstantExpression lhs, Rhs: ConstantExpression rhs }
                when lhs.Value.TryHexToUint32(out var lhsInt) && rhs.Value.TryHexToUint32(out var rhsInt):
                return new ConstantExpression($"0x{(lhsInt << (int)rhsInt):X1}");
            case ShiftRightUnsignedExpression
            {
                Lhs: ShiftLeftExpression { Lhs: var lhs2, Rhs: ConstantExpression { Value: "0x2" } },
                Rhs: ConstantExpression { Value: "0x2" }
            }:
                return lhs2;
            case MultiplyExpression { Lhs: ConstantExpression { Value: "0x1" }, Rhs: var rhs }:
                return rhs;
            case MultiplyExpression { Lhs: var lhs, Rhs: ConstantExpression { Value: "0x1" } }:
                return lhs;
            case MultiplyExpression { Lhs: ConstantExpression lhs, Rhs: ConstantExpression rhs }
                when lhs.Value.TryHexToUint32(out var lhsInt) && rhs.Value.TryHexToUint32(out var rhsInt):
                return new ConstantExpression($"0x{(lhsInt * rhsInt):X1}");
        }

        return expression;
    }
}