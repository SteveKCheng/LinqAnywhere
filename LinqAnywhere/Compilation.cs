using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqAnywhere
{
    public static class Compilation
    {
        public static bool MatchPredicate(this IndexColumnMatch column, 
                                          Expression subject,
                                          Expression term)
        {
            var maybeComparison = ColumnComparison.DecodeExpression(column.Column,
                                                                    subject,
                                                                    term);
            if (maybeComparison == null)
                return false;

            var comparison = maybeComparison.Value;

            if (comparison.Operand.NodeType != ExpressionType.Constant)
                return false;

            var operand = ((ConstantExpression)comparison.Operand).Value;

            var totalOrder = column.Column.TotalOrder;

            var interval = comparison.IsEquality 
                                ? Interval.CreateSinglePoint(operand)
                                : Interval.CreateOneSidedBound(operand,
                                                               comparison.IsExclusive,
                                                               comparison.IsUpperBound);
            column.Interval = column.Interval.IntersectTypeErased(interval, totalOrder);
            return true;
        }
    }
}