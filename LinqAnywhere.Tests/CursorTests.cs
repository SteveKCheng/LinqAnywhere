using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinqAnywhere.Tests
{
    public class DigitsCursor : ICursor
    {
        public readonly int NumColumns;

        private int[] currentValues;

        public DigitsCursor(int numColumns)
        {
            if (numColumns <= 0)
                throw new ArgumentOutOfRangeException(nameof(numColumns));

            NumColumns = numColumns;
            currentValues = new int[numColumns];
        }

        public static int GetCurrentValueInteger(int[] c)
        {
            int x = 0;
            for (int i = 0; i < c.Length; ++i)
                x = x * 10 + c[i];
            return x;
        }

        public object Current => currentValues;

        public void Dispose()
        {
        }

        public object GetColumnValue(int columnOrdinal)
        {
            return currentValues[columnOrdinal];
        }

        public bool MoveNext()
        {
            return Increment(NumColumns);
        }

        private bool Increment(int i)
        {
            while (i-- > 0)
            {
                if (++currentValues[i] != 10)
                    return true;

                currentValues[i] = 0;
            }

            return false;
        }

        public void Reset()
        {
            for (int i = 0; i < NumColumns-1; ++i)
                currentValues[i] = 0;

            currentValues[NumColumns-1] = -1;
        }

        public bool SeekTo(int numColumns, object[] columnValue, bool following)
        {
            for (int i = 0; i < numColumns; ++i)
                currentValues[i] = (int)columnValue[i];

            if (following)
            {
                if (!Increment(numColumns))
                    return false;
            }

            for (int i = numColumns; i < NumColumns; ++i)
                currentValues[i] = 0;

            return true;
        }
    }

    public class CursorTests
    {
        private static Interval<object> MakeInterval(object lowerBound, 
                                                     object upperBound,
                                                     IComparer comparer)
        {
            var interval = new Interval<object>();
            if (lowerBound != null)
                interval = interval.IntersectTypeErased(Interval.CreateLowerBounded(lowerBound, false), comparer);
            if (upperBound != null)
                interval = interval.IntersectTypeErased(Interval.CreateUpperBounded(upperBound, false), comparer);
            return interval;
        }

        [Fact]
        public void Test1()
        {
            var origCursor = new DigitsCursor(5);

            var comparer = Comparer<int>.Default;

            var columns = new IndexColumnMatch[4] {
                new IndexColumnMatch { 
                    ColumnOrdinal = 0,
                    Comparer = comparer,
                    Interval = MakeInterval(3, 7, comparer),
                },
                new IndexColumnMatch { 
                    ColumnOrdinal = 1,
                    Comparer = comparer,
                    Interval = MakeInterval(1, 8, comparer),
                },
                new IndexColumnMatch { 
                    ColumnOrdinal = 2,
                    Comparer = comparer,
                    Interval = MakeInterval(9, null, comparer),
                },
                new IndexColumnMatch { 
                    ColumnOrdinal = 3,
                    Comparer = comparer,
                    Interval = MakeInterval(null, 2, comparer),
                },
            };

            int count = 0;
            int i;

            using (var filteredCursor = new FilteredCursor(origCursor, columns, columns.Length))
            {
                int lastValue = -1;

                while (filteredCursor.MoveNext())
                {
                    count++;

                    var c = (int[])filteredCursor.Current;
                    var thisValue = DigitsCursor.GetCurrentValueInteger(c);
                    // Console.WriteLine(thisValue);
                    for (i = 0; i < columns.Length; ++i)
                    {
                        var lowerBound = (int?)columns[i].Interval.LowerBound ?? 0;
                        var upperBound = (int?)columns[i].Interval.UpperBound ?? 9;
                        Assert.InRange(c[i], lowerBound, upperBound);
                    }
                    
                    Assert.True(thisValue > lastValue);
                    lastValue = thisValue;
                }
            }

            int expectedCount = 1;
            for (i = 0; i < columns.Length; ++i)
            {
                var lowerBound = (int?)columns[i].Interval.LowerBound ?? 0;
                var upperBound = (int?)columns[i].Interval.UpperBound ?? 9;
                expectedCount *= upperBound - lowerBound + 1;
            }
                
            for (; i < origCursor.NumColumns; ++i)
                expectedCount *= 10;

            Assert.Equal(expectedCount, count);
        }
    }
}
