using System.Linq.Expressions;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes an indexed column in an AbstractTable.
    /// </summary>
    public struct ColumnDescriptor
    {
        /// <summary>
        /// Whether the values of the column have a total ordering.
        /// </summary>
        public bool IsOrdered { get; }

        /// <summary>
        /// Whether multiple rows in the same table can share the
        /// same value for this column.
        /// </summary>
        public bool IsUnique { get; }

        public ColumnDescriptor(Expression columnExpression)
        {
            this.ColumnExpression = columnExpression;
            this.IsOrdered = true;
            this.IsUnique = false;
            this.RowExpression = null;
        }

        /// <summary>
        /// LINQ expression which extracts the value of this column
        /// for a given row of the abstract table.
        /// </summary>
        /// <value></value>
        public Expression ColumnExpression { get; }

        /// <summary>
        /// The LINQ expression that represents a given row in the
        /// abstract table inside ColumnExpression.
        /// </summary>
        public Expression RowExpression { get; }

        public bool MatchesExpression(ParameterExpression subject, Expression expr)
        {
            // FIXME subject needs to be considered in the comparison
            return ExpressionEqualityComparer.EqualsInternal(expr, ColumnExpression);
        }
    }
}
