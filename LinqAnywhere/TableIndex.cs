using System.Linq.Expressions;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes a primary or secondary index in an AbstractTable.
    /// </summary>
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
        public ColumnDescriptor GetTerm(int columnOrdinal)
            => columns[columnOrdinal];

        /// <summary>
        /// Attempt to match each term (an expression) to a column of a table index.
        /// </summary>
        /// <param name="subject">Expression referring to a row of the table, used
        /// inside each term. </param>
        /// <param name="terms">Expression which may be a predicate involving
        /// a column of an index and a value to match.</param>
        /// <returns></returns>
        public IndexColumnMatch[] ComputeIndexColumnMatches(ParameterExpression subject, 
                                                            Expression[] terms)
        {
            var matchInfo = new IndexColumnMatch[terms.Length];

            for(int i = 0; i < terms.Length; ++i)
            {
                var term = terms[i];

                for (int j = 0; j < this.NumberOfColumns; ++j)
                {
                    var column = this.GetTerm(j);
                    bool matches = column.MatchesExpression(subject, term);
                    matchInfo[i].ColumnOrdinal = matches ? j : -1;
                }
            }

            return matchInfo;
        }
    }
}
