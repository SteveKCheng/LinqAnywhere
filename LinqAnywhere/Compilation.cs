using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqAnywhere
{

    /// <summary>
    /// Normalized representation of a comparison operation 
    /// on an index column.
    /// </summary>
    public struct ColumnComparison
    {
        /// <summary>
        /// If IsEquality is true, this member indicates if 
        /// the predicate is an upper bound or lower bound
        /// on the target column's values.
        /// </summary>
        public bool IsUpperBound;

        /// <summary>
        /// Whether the predicate is an equality or inequality.
        /// </summary>
        public bool IsEquality;

        /// <summary>
        /// If IsEquality is false, sets whether the predicate
        /// fails for the bound value itself.   If IsEquality is
        /// true, this member is set to true to represent 
        /// "not equal".
        /// </summary>
        public bool IsExclusive;

        /// <summary>
        /// The expression for the bound to compare against.
        /// </summary>
        public Expression Operand;
    }

    public static class Compilation
    {
        /// <summary>
        /// Pattern-match a LINQ expression against a recognized comparison
        /// on an indexed column.
        /// </summary>
        /// <param name="columnDescriptor">The column whose values may be
        /// being compared against. </param>
        /// <param name="subject">LINQ expression occurring within 
        /// <paramref="expr" /> referring to the row which
        /// contains the column. </param>
        /// <param name="expr">The LINQ expression to try to match. </param>
        /// <param name="isTopLevel">Whether this is the top-level call;
        /// this function is recursive. </param>
        /// <returns></returns>
        private static ColumnComparison? DecodeColumnComparison(ColumnDescriptor columnDescriptor,
                                                                Expression subject,
                                                                Expression expr,
                                                                bool isTopLevel = true)
        {
            var result = new ColumnComparison();

            switch (expr.NodeType)
            {
                case ExpressionType.NotEqual:
                    result.IsExclusive = true;
                    goto case ExpressionType.Equal;
                case ExpressionType.Equal:
                    result.IsEquality = true;
                    break;

                case ExpressionType.LessThan:
                    result.IsExclusive = true;
                    goto case ExpressionType.LessThanOrEqual;
                case ExpressionType.LessThanOrEqual:
                    result.IsUpperBound = true;
                    break;

                case ExpressionType.GreaterThan:
                    result.IsExclusive = true;
                    goto case ExpressionType.GreaterThanOrEqual;
                case ExpressionType.GreaterThanOrEqual:
                    result.IsUpperBound = false;
                    break;

                case ExpressionType.Not:
                    // Recursive call to handle logical negation
                    var innerExpr = (UnaryExpression)expr;
                    var innerResult = DecodeColumnComparison(columnDescriptor, 
                                                            subject,
                                                            innerExpr.Operand,
                                                            false);
                    if (innerResult == null)
                        return null;

                    // Invert the result of the comparison
                    result = innerResult.Value;
                    result.IsExclusive = !result.IsExclusive;
                    result.IsUpperBound = !result.IsUpperBound;
                    goto DisallowNotEqual;

                default:
                    return null;
            }

            var binaryExpr = (BinaryExpression)expr;
            result.Operand = binaryExpr.Right;

            if (!columnDescriptor.MatchesExpression(subject, binaryExpr.Left))
            {
                if (!columnDescriptor.MatchesExpression(subject, binaryExpr.Right))
                    return null;

                // Reverse the direction of comparison
                result.IsUpperBound = !result.IsUpperBound;
                result.Operand = binaryExpr.Left;
            }

        DisallowNotEqual:
            // Not-equal constraints are not allowed as the final output.
            // But we have to parse it recursively to treat a predicate like
            //  !(x != a) equivalently to (x == a).
            if (isTopLevel && result.IsEquality && result.IsExclusive)
                return null;

            return result;
        }

        public static bool MatchPredicate(this IndexColumnMatch column, 
                                          Expression subject,
                                          Expression term)
        {
            var maybeComparison = DecodeColumnComparison(column.ColumnDescriptor,
                                                         subject,
                                                         term);
            if (maybeComparison == null)
                return false;

            var comparison = maybeComparison.Value;

            if (comparison.Operand.NodeType != ExpressionType.Constant)
                return false;

            var operand = ((ConstantExpression)comparison.Operand).Value;

            var totalOrder = column.ColumnDescriptor.TotalOrder;

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