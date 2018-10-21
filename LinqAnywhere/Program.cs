﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqAnywhere
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var table = new AbstractTable<int>();
            table.Where(v => v > 0);
        }
    }

    public class AbstractTableBase
    {
        public ICursor OpenCursor()
        {
            return null;
        }
    }

    public class AbstractTable<T> : AbstractTableBase, IQueryable<T>
    {
        public Type ElementType => typeof(T);

        public Expression Expression { get; }

        private AbstractQueryProvider provider;

        public IQueryProvider Provider => provider;

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal AbstractTable(AbstractQueryProvider provider, Expression expression)
        {
            this.Expression = expression;
            this.provider = provider;
        }

        public AbstractTable()
        {
            this.Expression = Expression.Constant(this);
            this.provider = new AbstractQueryProvider();
        }

        // Need properties/methods to retrieve the available indices
        //
        // A table has zero or more indices
        //
        // An (ordered) index is a sequence of "columns"
        // Each column is realized by a functor Func<TEntity, TColumn>
        // where TColumn is the column type.  This allows computed indices.
        //
        // Given a LINQ expression e, we need to determine if
        // e matches a column.  e follows the IndexableExpression
        // production below.
        //
        // IndexableExpression := IndexableExpression . Property
        //                      | IndexableExpression . Field
        //                      | FunctionCall(IndexableExpression...)
        //                      | IndexableExpression.MethodCall(IndexableExpression...)
        //                      | TEntity this
        //                      | Constant object (compared by object.Equals)
        //
        // IndexableOperations := Equal | LessThan | LessThanOrEqual
        //                              | GreaterThan | GreaterThanOrEqual
        //
        // A Where clause with a conjunctive clause
        //  Term1 && Term2 && Term3 && ...
        //
        // assuming that the terms can commute, can use an index
        // (IndexableExpression1, ..., IndexableExpressionN)
        // if the elements of a subsequence M <= N
        // (IndexableExpression1, ..., IndexableExpressionM)
        // are present on the LHS of some term in the conjunctive
        // clause, and the predicate is equality on every one
        // of IndexableExpression1, ..., IndexableExpression(M-1)

        private TableIndex[] orderedIndices;

        public TableIndex GetOrderedIndex(int indexNumber)
        {
            return orderedIndices[indexNumber];
        }

        public int NumberOfOrderedIndices
        {
            get => orderedIndices.Length;
        }

    

    }


    public struct IndexMatchInfo
    {
        public static bool Match(ParameterExpression subject, Expression[] terms, 
                                 TableIndex tableIndex)
        {
            var matchInfo = tableIndex.ComputeIndexColumnMatches(subject, terms);

            Array.Sort(matchInfo, terms);

            var lastColumnOrdinal = matchInfo[terms.Length-1].ColumnOrdinal;
            if (lastColumnOrdinal < 0)
                return false;

            int termOrdinal = terms.Length;
            while (termOrdinal > 0)
            {
                var columnOrdinal = matchInfo[termOrdinal-1].ColumnOrdinal;
                if (columnOrdinal < 0)
                    break;
                if (columnOrdinal < lastColumnOrdinal-1)
                    return false;
                lastColumnOrdinal = columnOrdinal;
                termOrdinal--;
            }

            return lastColumnOrdinal == 0;

            // Check that each term term[termIndex] ... 
            // have effectively equality predicates

            // Actually this is too strict.  Think about the 2D case
            // where we are restricting to a box { x <= a, y <= b }
            // This cannot be completed as a single index scan,
            // but we can still scan items sequentially in a double loop
            // Do we generate instructions for the loop (of "arbitrary" arity)
            // similar to SQLite's VM language?
        }
    }

    public class AbstractQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type;
            var queryableType = typeof(AbstractTable<>).MakeGenericType(elementType);

            try 
            {
                return (IQueryable)Activator.CreateInstance(
                        queryableType,
                        new object[] { this, expression });
            }
            catch (TargetInvocationException tie) 
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            Console.WriteLine("CreateQuery<TResult> called");
            Console.WriteLine(expression.GetType());
            return new AbstractTable<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
