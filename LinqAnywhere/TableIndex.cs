using System.Linq.Expressions;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes a primary or secondary index in an AbstractTable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An index for an abstract table is essentially a sequence of
    /// columns that have been "indexed", i.e. searches for values
    /// over the columns are efficient.  
    /// </para>
    /// <para>
    /// The computational model for an index as defined here has been 
    /// abstracted from the usual way they are implemented in SQL databases: 
    /// the values of the columns, written in the order the columns appear, 
    /// form a sort key is stored in sorted order in a one-dimensional array
    /// associated with the data table.  Thus a multi-dimensional key is flattened 
    /// into a single-dimensional key.  The values in the indexable columns
    /// are assumed to have a total order, and the flattened key has the induced
    /// total ordering.  With this total ordering, the rows corresponding to 
    /// any prefix sub-sequence of values for the indexable columns can be
    /// efficiently found with binary search.
    /// </para>
    /// <para>
    /// SQL databases may also implement indices as hash tables, which do not
    /// require total ordering of the individual columns, and do not provide a
    /// total ordering on the sequence of columns.  Hash tables
    /// do not allow ranged queries or sub-key queries: only the full tuple
    /// of column values may be efficiently sought for. 
    /// </para>
    /// </remarks>
    public struct TableIndex
    {
        /// <summary>
        /// The sequence of columns forming this index.
        /// </summary>
        private ColumnDescriptor[] columns;

        /// <summary>
        /// The number of columns forming this index.
        /// </summary>
        public int NumberOfColumns => columns.Length;

        /// <summary>
        /// Whether this table index is ordered, i.e. supports
        /// sequential iteration in order.
        /// </summary>
        /// <remarks>
        /// If true, the index is probably backed by some kind of sorted
        /// array.   If false, the table is probably backed by a hash table.
        /// </remarks>
        public bool IsOrdered { get; }

        /// <summary>
        /// Construct instance representing a table index.
        /// </summary>
        public TableIndex(ColumnDescriptor[] columns, bool isOrdered)
        {
            this.IsOrdered = isOrdered;
            this.columns = columns;
        }

        /// <summary>
        /// Get information about one column in the index.
        /// </summary>
        /// <param name="columnOrdinal">The ordinal of the column
        /// within this table index.  Ordinals range from zero
        /// to the number of columns minus one.
        /// </param>
        public ColumnDescriptor GetColumn(int columnOrdinal)
            => columns[columnOrdinal];

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
        public IndexColumnMatch[] ComputeIndexColumnMatches(ParameterExpression subject, 
                                                            Expression[] terms)
        {
            var matchInfo = new IndexColumnMatch[this.NumberOfColumns];
            for (int j = 0; j < matchInfo.Length; ++j)
                matchInfo[j].Column = this.GetColumn(j);

            for (int i = 0; i < terms.Length; ++i)
            {
                for (int j = 0; j < matchInfo.Length; ++j)
                {
                    var hasMatched = matchInfo[j].MatchPredicate(subject, terms[i]);
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
