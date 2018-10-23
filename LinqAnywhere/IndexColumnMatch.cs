using System;
using System.Collections;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes how one column within an index has been matched
    /// in some query.
    /// </summary>
    public class IndexColumnMatch : IComparable<IndexColumnMatch>
    {
        /// <summary>
        /// The integer index of the column within the table index.
        /// </summary>
        public int ColumnOrdinal;

        /// <summary>
        /// For comparing values for this column.
        /// </summary>
        public IComparer Comparer;

        /// <summary>
        /// The range of values on the column to match.
        /// </summary>
        public Interval<object> Interval;

        public ColumnDescriptor ColumnDescriptor;

        /// <summary>
        /// Orders instances by ordinal, used when matching queries against
        /// available indices.
        /// </summary>
        int IComparable<IndexColumnMatch>.CompareTo(IndexColumnMatch other)
            => this.ColumnOrdinal.CompareTo(other.ColumnOrdinal);
    }
}
