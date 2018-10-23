using System;
using System.Collections;

namespace LinqAnywhere
{
    /// <summary>
    /// Describes how one column within an index has been matched
    /// in some query.
    /// </summary>
    public class IndexColumnMatch
    {
        /// <summary>
        /// For comparing values for this column.
        /// </summary>
        public IComparer Comparer;

        /// <summary>
        /// The range of values on the column to match.
        /// </summary>
        public Interval<object> Interval;

        public ColumnDescriptor ColumnDescriptor;
    }
}
