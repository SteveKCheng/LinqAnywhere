using System;
using System.Collections;

namespace LinqAnywhere
{
    /// <summary>
    /// An enumerator for an abstract table that can reset to 
    /// a position given by the values of indexed columns.
    /// </summary>
    public interface ICursor : IEnumerator, IDisposable
    {
        /// <summary>
        /// Move the cursor to the first row such that,
        /// a row can be hypothetically inserted before it
        /// with the specified column values without violating
        /// the table index's ordering.  
        /// </summary>
        /// <remarks>
        /// <paramref name="following" /> set to false
        /// means the equivalent of the "lower bound"
        /// operation in the C++ STL; <paramref name="following" />
        /// set to true means the equivalent of the "upper bound"
        /// operation.
        /// </remarks>
        /// <param name="columnOrdinal">The number of values 
        /// to take from the beginning of the array 
        /// <paramref name="columnValue" />.
        /// The entries in the array beyond those values
        /// are ignored. </param>
        /// <param name="columnValue">Sequence of hypothetical values
        /// for the columns. </param>
        /// <param name="following">If true, position the cursor
        /// after the last row, such that if a row was hypothetically
        /// inserted with the specified column values, the table
        /// index's ordering would not be violated.
        /// </param>
        /// <returns>Whether there is a row occurring at the cursor
        /// after seeking.
        /// </returns>
        bool SeekTo(int numColumns, object[] columnValue, bool following);

        /// <summary>
        /// Retrieve the value for a specified column for the
        /// current row.
        /// </summary>
        /// <remarks>
        /// The cursor must be positioned at some row, i.e.
        /// the cursor has not moved past the end of the table.
        /// <param name="columnOrdinal">Which column to retrieve
        /// the value for. </param>
        /// <returns>The (boxed) value of the column for the
        /// current row. </returns>
        object GetColumnValue(int columnOrdinal);
    }
}
