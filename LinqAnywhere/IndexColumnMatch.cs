using System;
using System.Collections;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes how one column within an index has been matched
    /// in some query.
    /// </summary>
    public struct IndexColumnMatch
    {
        /// <summary>
        /// The range of values on the column to match.
        /// </summary>
        public Interval<object> Interval;

        /// <summary>
        /// Metadata for the column.
        /// </summary>
        public ColumnDescriptor Column;

        /// <summary>
        /// For comparing values for this column.
        /// </summary>
        public IComparer Comparer => Column.TotalOrder;
    }
}
