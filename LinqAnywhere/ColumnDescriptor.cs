using System.Collections;
using System.Linq.Expressions;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes an indexed column in an AbstractTable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For the purposes of this library, a "column" even without
    /// the qualification "indexable", is assumed to be part of a table index.
    /// An abstract table does not need an explicit representation of 
    /// any other kind of column, because without an index, there is nothing
    /// that a relational query processor can do to optimize accesses to it.
    /// This design is deliberate so that, unlike SQL, some parts of the data 
    /// in the table row can be in any format (e.g. it could be an array of items, 
    /// something that SQL handles terribly) if relational queries on that 
    /// part of data are not needed.  In other words, non-indexable data
    /// is just treated as a blob.
    /// </para>
    /// <para>
    /// This class holds the most essential data for querying on a
    /// column as part of an index, for optimizing relational queries.
    /// </para>
    /// </remarks>
    public class ColumnDescriptor
    {
        /// <summary>
        /// Whether the values of the column have a total ordering.
        /// </summary>
        public bool IsOrdered => TotalOrder != null;

        /// <summary>
        /// Whether multiple rows in the same table can share the
        /// same value for this column.
        /// </summary>
        public bool IsUnique { get; }

        /// <summary>
        /// Construct descriptor for a column with total ordering of its values.
        /// </summary>
        public ColumnDescriptor(Expression rowExpression, 
                                Expression columnExpression, 
                                IComparer totalOrder,
                                bool isUnique)
        {
            this.RowExpression = rowExpression;
            this.ColumnExpression = columnExpression;
            this.TotalOrder = totalOrder;
            this.Equivalence = null;
            this.IsUnique = isUnique;
        }

        /// <summary>
        /// Construct descriptor for a column without total ordering of its values.
        /// </summary>
        public ColumnDescriptor(Expression rowExpression,
                                Expression columnExpression, 
                                IEqualityComparer equivalence,
                                bool isUnique)
        {
            this.RowExpression = rowExpression;
            this.ColumnExpression = columnExpression;
            this.TotalOrder = null;
            this.Equivalence = equivalence;
            this.IsUnique = isUnique;
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

        /// <summary>
        /// If the values of the column are totally ordered, 
        /// this member gives the IComparer implementation to give
        /// the relative positioning of two values.
        /// </summary>
        public IComparer TotalOrder { get; } 

        /// <summary>
        /// If the values do not have a total ordering,
        /// this members gives the IEqualityComparer implementation to
        /// compare if two values are equal.
        /// </summary>
        public IEqualityComparer Equivalence { get; }

        /// <summary>
        /// Determine whether a LINQ expression evaluates to this column
        /// for a row in the abstract table.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As a concrete example, if each row R of the abstract table,
        /// as a .NET object, has a property X that maps to this column,
        /// then this method returns true for the LINQ expression R.X 
        /// and false for any other expression.
        /// </para>
        /// <para>
        /// This method is used to match conditional clauses in a LINQ query
        /// to the indices on an abstract table that can be used to optimize
        /// the query.
        /// </para>
        /// </remarks>
        /// <param name="subject">Expression referring to an arbitrary row of the 
        /// abstract table. </param>
        /// <param name="expr">The LINQ expression to test. </param>
        /// <returns>True if the LINQ expression exactly matches this column,
        /// otherwise false. </returns>
        public bool MatchesExpression(ParameterExpression subject, Expression expr)
        {
            // FIXME subject needs to be considered in the comparison
            return new ExpressionEqualityComparer().Equals(expr, ColumnExpression);
        }
    }
}
