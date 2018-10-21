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
        /// Whether the column value is to be bounded below.
        /// </summary>
        public bool IsLowerBounded;

        /// <summary>
        /// Whether the lower bound is exclusive of the target value.
        /// </summary>
        public bool IsLowerBoundExclusive;

        /// <summary>
        /// Whether the column value is to be bounded above.
        /// </summary>
        public bool IsUpperBounded;

        /// <summary>
        /// Whether the upper bound is exclusive of the target value.
        /// </summary>
        public bool IsUpperBoundExclusive;

        /// <summary>
        /// The desired lower bound on the column value.
        /// </summary>
        public object LowerBoundValue;

        /// <summary>
        /// The desired lower bound on the column value.
        /// </summary>
        public object UpperBoundValue;

        /// <summary>
        /// Orders instances by ordinal, used when matching queries against
        /// available indices.
        /// </summary>
        int IComparable<IndexColumnMatch>.CompareTo(IndexColumnMatch other)
            => this.ColumnOrdinal.CompareTo(other.ColumnOrdinal);
    }
}
