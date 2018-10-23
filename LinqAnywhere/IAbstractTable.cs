using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqAnywhere
{
    /// <summary>
    /// Interface to an abstract database table or view which supports
    /// indexing.
    /// </summary>
    public interface IAbstractTable : IEnumerable
    {
        /// <summary>
        /// Open a cursor which can seek and iterate through
        /// the rows of a table via one of its (column) indices.
        /// </summary>
        /// <param name="indexNumber">Specifies which index. 
        /// Must be in the range from 0 to the 
        /// NumberOfIndices minus one.
        /// </param>
        /// <returns>A new cursor to the table. </returns>
        ICursor OpenCursor(int indexNumber);

        /// <summary>
        /// Get the descriptor for one of this table's indices.
        /// </summary>
        /// <param name="indexNumber">Specifies which index. 
        /// Must be in the range from 0 to the 
        /// NumberOfIndices minus one.
        /// </param>
        /// <returns>Descriptor for the desired table index. </returns>
        TableIndex GetTableIndex(int indexNumber);

        /// <summary>
        /// The number of indices that this table makes available.
        /// </summary>
        int NumberOfIndices { get; }

        /// <summary>
        /// The .NET type which represents one row in the table.
        /// </summary>
        Type ElementType { get; }
    }

    /// <summary>
    /// Interface to an abstract database table or view which supports
    /// indexing, and yields rows with static type <typeparamref name="T" />.
    /// </summary>
    public interface IAbstractTable<T> : IAbstractTable, IEnumerable<T>
    {
    }
}