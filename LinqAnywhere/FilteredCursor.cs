using System;
using System.Collections;

namespace LinqAnywhere
{
    /// <summary>
    /// Filters an existing cursor by admitting one range of values
    /// for each indexed column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Let Vi, i = 0 to n-1, be the values of the columns 
    /// from the underlying cursor for any particular row.
    /// A filtered cursor produces only those rows such that
    /// Li <= Vi <= Ui for lower bounds Li and upper bounds Ui
    /// for each i = 0 to n-1.  Each of the inequalities may also be 
    /// strict inequalities, equalities, or omitted.
    /// </para>
    /// <para>
    /// Usually in relational queries, the columns being filtered
    /// is a prefix of all the available columns in order, and
    /// all but the last column are subject to equality only.
    /// However, FilteredCursor also works with general combinations
    /// of equalities and inequalities, by repeated seeking the
    /// underlying cursor.  The seeks are obviously not free but
    /// in most queries they should still be faster than scanning
    /// the whole table and filtering out rows individually.
    /// </remarks>
    public class FilteredCursor : IEnumerator, IDisposable
    {
        /// <summary>
        /// Original cursor to the abstract table before
        /// the filters from this cursor get applied.
        /// </summary>
        private ICursor origCursor;

        /// <summary>
        /// Ranged criteria for the columns.
        /// </summary>
        private IndexColumnMatch[] columnMatches;

        /// <summary>
        /// The values of the columns for the current row.
        /// </summary>
        /// <remarks>
        /// This member is a state variable updated by 
        /// MoveNextFiltered.
        /// </remarks>
        private object[] currentKey;

        /// <summary>
        /// Whether MoveNextFiltered is continuing the iteration
        /// from where it left off earlier.
        /// </summary>
        /// <remarks>
        /// This member is a state variable updated by 
        /// MoveNextFiltered.
        /// </remarks>
        private bool hasIterationStarted;

        public FilteredCursor(ICursor origCursor, IndexColumnMatch[] columnMatches)
        {
            this.origCursor = origCursor ?? throw new ArgumentNullException(nameof(origCursor));
            this.columnMatches = columnMatches ?? throw new ArgumentException(nameof(columnMatches));
            this.currentKey = new object[columnMatches.Length];
        }

        /// <inheritdoc />
        public void Reset() 
        {
            if (origCursor == null)
                throw new ObjectDisposedException(nameof(FilteredCursor));

            origCursor.Reset();
            hasIterationStarted = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var c = origCursor;
            origCursor = null;
            c?.Dispose();

            currentKey = null;
        }

        /// <inheritdoc />
        public object Current => origCursor.Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (origCursor == null)
                throw new ObjectDisposedException(nameof(FilteredCursor));

            if (columnMatches.Length == 0)
                return origCursor.MoveNext();

            return MoveNextFiltered();
        }

        /// <summary>
        /// Dances forwards and backwards over the columns to move the
        /// cursor to the next logical row according to the filter criteria.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is written using goto statements.  Not only is the
        /// resulting code more efficient than code written in the usual
        /// structured form, the structured form requires boolean state
        /// variables controlling if branches which obscure the desired 
        /// control flow.
        /// </para>
        /// <para>
        /// Logically, the columns have to be scanned left to right
        /// to enforce the lower and upper bounds specified on their
        /// values.true  But when moving forward in the underlying cursor,
        /// the values of already visited columns may change from what
        /// the values had been before, and then the preceding columns
        /// have to be re-scanned to enforce the lower and upper bounds.
        /// </para>
        /// <para>
        /// The number of times that the underlying cursor has to be
        /// moved and re-checked is not bounded by any fixed multiple of
        /// the number of columns.
        /// </para>
        /// <para>
        /// This method assumes there is at least one column to filter on.
        /// </para>
        /// </remarks>
        /// <returns>True if the next filtered row is available and
        /// the cursor has been positioned on it.  False if the cursor
        /// has exhausted all the filtered rows. </returns>
        private bool MoveNextFiltered()
        {
            int columnOrdinal;          // Index of column we are working on.
            IndexColumnMatch column;    // = columnMatches[columnOrdinal]

            // Resuming the state machine from the last time it emitted a row.
            // We are at the last column, and we can try to emit the next row.
            if (hasIterationStarted)
            {
                if (!origCursor.MoveNext())
                    return false;

                columnOrdinal = columnMatches.Length - 1;
                column = columnMatches[columnOrdinal];
                goto CheckForRoll;
            }

            // Otherwise, starting the state machine from the very beginning.
            hasIterationStarted = true;
            columnOrdinal = 0;

        StartNextColumn:
            column = columnMatches[columnOrdinal];

            if (!column.IsLowerBounded)
                goto UpdateThisColumn;

            // When restarting the iteration on this column,
            // we need to seek to the lower bound value first, if
            // there is one.  
            currentKey[columnOrdinal] = column.LowerBoundValue;
            if (!origCursor.SeekTo(columnOrdinal + 1,
                                   currentKey,
                                   column.IsLowerBoundExclusive))
                return false;

        CheckForRoll:
            // If the underlying cursor has been moved in any way, check if the values
            // of preceding columns have rolled over.  If so, we need to restart looping
            // from the first column that rolled over, in order to check bounds.
            for (int i = 0; i < columnOrdinal; ++i)
            {
                // Update cache the current key for preceding columns.
                var newValue = origCursor.GetColumnValue(i);
                var oldValue = currentKey[i];
                currentKey[i] = newValue;

                if (columnMatches[i].Comparer.Compare(newValue, oldValue) != 0)
                {
                    // Column i has rolled over.  Go back to scanning that column.
                    columnOrdinal = i;
                    column = columnMatches[i];
                    goto CheckThisColumn;
                }
            }

        UpdateThisColumn:
            // Update cache of the current key for this column,
            // when preceding columns are not rolling over.
            currentKey[columnOrdinal] = origCursor.GetColumnValue(columnOrdinal);

        CheckThisColumn:
            // Check upper bound for this column.
            if (column.IsUpperBounded)
            {
                var compareResult = column.Comparer.Compare(currentKey[columnOrdinal], 
                                                            column.UpperBoundValue);

                // When this current value for this column exceeds the 
                // desired upper bound, we have to manually roll over
                // the immediately preceding column.
                if (compareResult >= (column.IsUpperBoundExclusive ? 0 : 1))
                {
                    if (!origCursor.SeekTo(columnOrdinal, 
                                           currentKey,
                                           true))
                        return false;

                    column = columnMatches[--columnOrdinal];
                    goto CheckForRoll;
                }
            }

            // Proceed to scan on the next column, if any.
            if (++columnOrdinal != columnMatches.Length)
                goto StartNextColumn;
            
            // If there are no more columns, that means the underlying cursor
            // is now positioned to a new row, ready to be emitted.
            return true;
        }
    }
}