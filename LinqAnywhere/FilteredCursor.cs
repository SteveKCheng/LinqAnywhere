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
        private readonly IndexColumnMatch[] criteria;

        /// <summary>
        /// The values of the columns for the current row.
        /// </summary>
        /// <remarks>
        /// This member is a state variable updated by 
        /// MoveNextFiltered.
        /// </remarks>
        private readonly object[] currentKey;

        /// <summary>
        /// Whether MoveNextFiltered is continuing the iteration
        /// from where it left off earlier.
        /// </summary>
        /// <remarks>
        /// This member is a state variable updated by 
        /// MoveNextFiltered.
        /// </remarks>
        private bool hasIterationStarted;

        /// <summary>
        /// Number of columns to use in the criteria array.
        /// </summary>
        /// <remarks>
        /// The criteria array may be allocated with extra elements
        /// beyond the logical number of criteria being considered.
        /// This number must be non-negative and not greater than
        /// the number of elements in the criteria array.
        /// </remarks>
        private readonly int numCriteria;

        /// <summary>
        /// Construct a cursor which filters on the given criteria.
        /// </summary>
        /// <param name="origCursor">Original cursor for the abstract table. </param>
        /// <param name="criteria">Array of criteria for a prefix sub-sequence of
        /// columns on the table. </param>
        /// <param name="numCriteria">The number of columns to index on. </param>
        public FilteredCursor(ICursor origCursor, IndexColumnMatch[] criteria, int numCriteria)
        {
            this.origCursor = origCursor ?? throw new ArgumentNullException(nameof(origCursor));
            this.criteria = criteria ?? throw new ArgumentException(nameof(criteria));

            if (numCriteria < 0 || numCriteria > criteria.Length)
                throw new ArgumentOutOfRangeException(nameof(numCriteria));

            this.currentKey = new object[numCriteria];
            this.numCriteria = numCriteria;
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
        }

        /// <inheritdoc />
        public object Current => origCursor.Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (origCursor == null)
                throw new ObjectDisposedException(nameof(FilteredCursor));

            if (numCriteria == 0)
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
            int ordinal;          // Index of column we are working on.

            // Resuming the state machine from the last time it emitted a row.
            // We are at the last column, and we can try to emit the next row.
            if (hasIterationStarted)
            {
                if (!origCursor.MoveNext())
                    return false;

                ordinal = numCriteria - 1;
                goto CheckForRoll;
            }

            // Otherwise, starting the state machine from the very beginning.
            hasIterationStarted = true;
            ordinal = 0;

        StartNextColumn:
            if (!criteria[ordinal].Interval.HasLowerBound)
                goto UpdateThisColumn;

            // When restarting the iteration on this column,
            // we need to seek to the lower bound value first, if
            // there is one.  
            currentKey[ordinal] = criteria[ordinal].Interval.LowerBound;
            if (!origCursor.SeekTo(ordinal + 1,
                                   currentKey,
                                   criteria[ordinal].Interval.IsLowerBoundExclusive))
                return false;

        CheckForRoll:
            // If the underlying cursor has been moved in any way, check if the values
            // of preceding columns have rolled over.  If so, we need to restart looping
            // from the first column that rolled over, in order to check bounds.
            for (int i = 0; i < ordinal; ++i)
            {
                // Update cache the current key for preceding columns.
                var newValue = origCursor.GetColumnValue(i);
                var oldValue = currentKey[i];
                currentKey[i] = newValue;

                if (criteria[i].Comparer.Compare(newValue, oldValue) != 0)
                {
                    // Column i has rolled over.  Go back to scanning that column.
                    ordinal = i;
                    goto CheckThisColumn;
                }
            }

        UpdateThisColumn:
            // Update cache of the current key for this column,
            // when preceding columns are not rolling over.
            currentKey[ordinal] = origCursor.GetColumnValue(ordinal);

        CheckThisColumn:
            // Check upper bound for this column.
            if (criteria[ordinal].Interval.HasUpperBound)
            {
                var compareResult = criteria[ordinal].Comparer.Compare(
                                        currentKey[ordinal], 
                                        criteria[ordinal].Interval.UpperBound);

                // When this current value for this column exceeds the 
                // desired upper bound, we have to manually roll over
                // the immediately preceding column.
                if (compareResult >= (criteria[ordinal].Interval.IsUpperBoundExclusive ? 0 : 1))
                {
                    if (!origCursor.SeekTo(ordinal--, 
                                           currentKey,
                                           true))
                        return false;
                    goto CheckForRoll;
                }
            }

            // Proceed to scan on the next column, if any.
            if (++ordinal != numCriteria)
                goto StartNextColumn;
            
            // If there are no more columns, that means the underlying cursor
            // is now positioned to a new row, ready to be emitted.
            return true;
        }
    }
}