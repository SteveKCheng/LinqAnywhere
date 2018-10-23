using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqAnywhere
{
    /// <summary>
    /// Pattern matching for LINQ expressions that compare against
    /// the constituent columns of an abstract table's index.
    /// </summary>
    public static class TableIndexMatch
    {
        /// <summary>
        /// Determine if a LINQ expression is a comparison on the given
        /// column's values, and if so, update the comparison interval.
        /// </summary>
        /// <param name="column">The column that may be matched. </param>
        /// <param name="subject">The LINQ expression referring to the table's row. </param>
        /// <param name="term">A LINQ expression that may be a comparison on the
        /// column's values. </param>
        /// <returns>If the <paramref name="term" /> is a recognized
        /// comparison operation on <paramref name="column" />. </returns>
        private static bool TryMatchColumnComparison(this IndexColumnMatch column, 
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

        /// <summary>
        /// Attempt to match each term (in a LINQ expression) to a column of a table index.
        /// </summary>
        /// <param name="subject">Expression referring to a row of the table, used
        /// inside each term. </param>
        /// <param name="terms">Each element may be an expression that is a 
        /// predicate involving a column of an index and a value to match.  If so, 
        /// the reference to the Expression is erased from the array on return.
        /// </param>
        /// <returns>
        /// Match information on each of the columns of this table index.
        /// Any range critiera specified by the expressions in 
        /// <paramref name="terms" /> are incorporated here.
        /// </returns>
        public static IndexColumnMatch[] ComputeIndexColumnMatches(
            this TableIndex tableIndex,
            ParameterExpression subject, 
            Expression[] terms)
        {
            var matchInfo = new IndexColumnMatch[tableIndex.NumberOfColumns];
            for (int j = 0; j < matchInfo.Length; ++j)
                matchInfo[j].Column = tableIndex.GetColumn(j);

            for (int i = 0; i < terms.Length; ++i)
            {
                for (int j = 0; j < matchInfo.Length; ++j)
                {
                    var hasMatched = matchInfo[j].TryMatchColumnComparison(subject, terms[i]);
                    if (hasMatched)
                    {
                        terms[i] = null;
                        break;
                    }
                }
            }

            return matchInfo;
        }
    }
}