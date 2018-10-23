using System.Collections;
using System.Collections.Generic;

namespace LinqAnywhere
{
    /// <summary>
    /// Represents an open, half-open, or closed interval
    /// of elements with a total ordering.
    /// </summary>
    public struct Interval<T>
    {
        /// <summary>
        /// Whether the interval is bounded below.
        /// </summary>
        public bool HasLowerBound { get; private set; }

        /// <summary>
        /// Whether the interval excludes the point that is its lower bound.
        /// </summary>
        /// <remarks>
        /// Only applies if HasLowerBound is true.
        /// </remarks>
        public bool IsLowerBoundExclusive { get; private set; }

        /// <summary>
        /// Whether the interval is bounded above.
        /// </summary>
        public bool HasUpperBound { get; private set; }

        /// <summary>
        /// Whether the interval excludes the point that is its upper bound.
        /// </summary>
        /// <remarks>
        /// Only applies if HasUpperBound is true.
        /// </remarks>
        public bool IsUpperBoundExclusive { get; private set; }

        /// <summary>
        /// The (greatest) lower bound point for the interval.
        /// </summary>
        /// <remarks>
        /// Only applies if HasLowerBound is true.
        /// </remarks>
        public T LowerBound { get; private set; }

        /// <summary>
        /// The (least) upper bound point for the interval.
        /// </summary>
        /// <remarks>
        /// Only applies if HasUpperBound is true.
        /// </remarks>
        public T UpperBound { get; private set; }

        /// <summary>
        /// Create an interval that is bounded below but not bounded above.
        /// </summary>
        public static Interval<T> CreateLowerBounded(T value, bool isExclusive)
            => new Interval<T> 
            {
                HasLowerBound = true,
                IsLowerBoundExclusive = isExclusive,
                LowerBound = value
            };
        
        /// <summary>
        /// Create an interval that is bounded above but not bounded below.
        /// </summary>
        public static Interval<T> CreateUpperBounded(T value, bool isExclusive)
            => new Interval<T> 
            {
                HasUpperBound = true,
                IsUpperBoundExclusive = isExclusive,
                UpperBound = value
            };

        /// <summary>
        /// Intersect two intervals.
        /// </summary>
        /// <param name="other">The other interval to intersect with. </param>
        /// <param name="comparer">Defines the total ordering. </param>
        /// <returns>The interval that is the intersection of this 
        /// and <paramref name="other" />. </returns>
        public Interval<T> Intersect<TComparer>(Interval<T> other, TComparer comparer)
            where TComparer : IComparer<T>
        {
            var result = this;

            if (other.HasLowerBound)
            {
                result.HasLowerBound = true;

                if (this.HasLowerBound)
                {
                    var c = comparer.Compare(other.LowerBound, this.LowerBound);
                    result.LowerBound = c >= 0 ? other.LowerBound 
                                                    : this.LowerBound;
                    result.IsLowerBoundExclusive = 
                        (c >= 0 && other.IsLowerBoundExclusive) ||
                        (c <= 0 && this.IsLowerBoundExclusive);
                }
                else
                {
                    result.LowerBound = other.LowerBound;
                    result.IsLowerBoundExclusive = other.IsLowerBoundExclusive;
                }
            }

            if (other.HasUpperBound)
            {
                result.HasUpperBound = true;

                if (this.HasUpperBound)
                {
                    var c = comparer.Compare(other.UpperBound, this.UpperBound);
                    result.UpperBound = c <= 0 ? other.UpperBound 
                                                    : this.UpperBound;
                    result.IsUpperBoundExclusive = 
                        (c <= 0 && other.IsUpperBoundExclusive) ||
                        (c >= 0 && this.IsUpperBoundExclusive);
                }
                else
                {
                    result.UpperBound = other.UpperBound;
                    result.IsUpperBoundExclusive = other.IsUpperBoundExclusive;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Holds extension methods to work with Intervals where the 
    /// values are stored type-erased.
    /// </summary>
    public static class ObjectInterval
    {
        /// <summary>
        /// Converts a non-generic IComparer to IComparer&lt;object&gt;.
        /// </summary>
        private struct BoxingComparer : IComparer<object>
        {
            private IComparer origComparer;

            public BoxingComparer(IComparer origComparer)
            {
                this.origComparer = origComparer;
            }

            public int Compare(object x, object y)
                => this.origComparer.Compare(x, y);
        }

        /// <summary>
        /// Intersect two intervals.
        /// </summary>
        /// <param name="other">The other interval to intersect with. </param>
        /// <param name="comparer">Defines the total ordering. </param>
        /// <returns>The interval that is the intersection of this 
        /// and <paramref name="other" />. </returns>
        public static Interval<object> Intersect(this Interval<object> self, 
                                                 Interval<object> other, 
                                                 IComparer comparer)
            => self.Intersect(other, new BoxingComparer(comparer));
    }
}