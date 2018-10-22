using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace LinqAnywhere.Tests
{
    public class ExpressionEqualityTests
    {
        [Fact]
        public void Test1()
        {
            var e1 = AsExpression((int x) => x+4);
            var e2 = AsExpression((int x) => x+4);

            var p1 = e1.Parameters[0];
            var p2 = e2.Parameters[0];
            var comparer = new ExpressionEqualityComparer();
            comparer.ParameterToUnify1 = p1;
            comparer.ParameterToUnify2 = p2;

            Assert.Equal(e1, e2, comparer);
        }

        public static LambdaExpression AsExpression<T, V>(Expression<Func<T, V>> expr) => expr;
    }
}
