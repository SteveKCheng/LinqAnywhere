using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.ObjectModel;
using System;

namespace LinqAnywhere
{
    /// <summary>
    /// Compares two LINQ expressions structurally, for pattern-matching clauses
    /// in LINQ queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Structural equality in general is slow, and that is why the LINQ Expression
    /// class does not implement it by default.   However, the typical pattern
    /// to match in LINQ clauses is short, and so the equality comparisons
    /// should short-circuit fairly quickly.
    /// </para>
    /// <para>
    /// Comparison can be made with unification of up to one sub-expression.
    /// This feature is used for pattern matching of the bound argument
    /// in LINQ lambda expressions representing a row of a table.
    /// </remarks>
    public struct ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        public int GetHashCode(Expression obj)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Prepare to compare for structural equality, while unifying
        /// two expressions.
        /// </summary>
        /// <param name="unify1">Expression to unify with <paramref name="unify2" />. </param>
        /// <param name="unify2">Expression to unify with <paramref name="unify1" />. </param>
        public ExpressionEqualityComparer(Expression unify1, Expression unify2)
        {
            if ((unify1 == null) != (unify2 == null))
                throw new ArgumentNullException("Two expressions to unify must be both null or both non-null", nameof(unify1));

            if (unify1 == unify2)
                unify1 = unify2 = null;

            this.Unify1 = unify1;
            this.Unify2 = unify2;
        }

        /// <summary>
        /// An expression to unify with Unify2 if it appears.
        /// Typically represents a parameter in the LINQ expression.
        /// </summary>
        public readonly Expression Unify1;

        /// <summary>
        /// An expression to unify with Unify1 if it appears.
        /// Typically represents a parameter in the LINQ expression.
        /// </summary>
        public readonly Expression Unify2;

        /// <summary>
        /// Compare two LINQ expressions for structural equality.
        /// </summary>
        /// <param name="x">One expression to compare.</param>
        /// <param name="y">The other expression to compare against. </param>
        /// <returns>Whether the expressions are considered equal. </returns>
        public bool Equals(Expression x, Expression y)
        {
            if ((x == null) != (y == null))
                return false;

            if (x == null)
                return true;

            if ((x == Unify1 || x == Unify2) &&
                (y == Unify1 || y == Unify2))
                return true;

            var nodeType = x.NodeType;
            if (nodeType != y.NodeType)
                return false;

            switch (nodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:

                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.AndAssign:
                case ExpressionType.OrElse:

                case ExpressionType.LeftShiftAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.MultiplyChecked:

                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.SubtractChecked:

                case ExpressionType.DivideAssign:

                case ExpressionType.Power:
                case ExpressionType.PowerAssign:

                case ExpressionType.Assign:
                case ExpressionType.Coalesce:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.Or:
                case ExpressionType.OrAssign:

                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return VisitBinary((BinaryExpression)x, (BinaryExpression)y);

                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.OnesComplement:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Not:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                    return VisitUnary((UnaryExpression)x, (UnaryExpression)y);

                case ExpressionType.MemberAccess:
                    return VisitMember((MemberExpression)x, (MemberExpression)y);

                case ExpressionType.Conditional:
                    return VisitConditional((ConditionalExpression)x, (ConditionalExpression)y);

                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)x, (ConstantExpression)y);

                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression)x, (MethodCallExpression)y);

                case ExpressionType.Default:
                    return VisitDefault((DefaultExpression)x, (DefaultExpression)y);

                case ExpressionType.ArrayIndex:
                case ExpressionType.Index:
                    return VisitIndexExpression((IndexExpression)x, (IndexExpression)y);

                case ExpressionType.New:
                    return VisitNew((NewExpression)x, (NewExpression)y);

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    return VisitNewArray((NewArrayExpression)x, (NewArrayExpression)y);

                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)x, (LambdaExpression)y);

                // MemberInit
                // Loop
                // ListInit
                // Label
                // Goto
                // Extension
                // Dynamic
                // DebugInfo
                // Block

                // Convert
                // ConvertChecked

                // ArrayLength
                // IsFalse
                // IsTrue
                // Invoke

                // TypeAs
                // TypeEqual
                // TypeIs
                // Unbox

                // Try
                // Switch
                // Throw
                // RuntimeVariables
                // Quote

                default:
                    // Fall back to reference equality if structural equality not implemented
                    return x == y;
            }
        }

        private bool VisitBinary(BinaryExpression x, BinaryExpression y)
            => x.Method == y.Method && 
                Equals(x.Left, y.Left) &&
                Equals(x.Right, y.Right) &&
                Equals(x.Conversion, y.Conversion);

        private bool VisitBlock(BlockExpression x, BlockExpression y)
            => ExpressionSequencesEqual(x.Expressions, y.Expressions);

        private bool VisitConditional(ConditionalExpression x, ConditionalExpression y)
            => Equals(x.Test, y.Test) &&
               Equals(x.IfTrue, y.IfTrue) &&
               Equals(x.IfFalse, y.IfFalse);
        
        private bool VisitConstant(ConstantExpression x, ConstantExpression y)
            => x.Value.Equals(y.Value);

        // DebugInfoExpression

        private bool VisitDefault(DefaultExpression x, DefaultExpression y)
            => x.Type == y.Type;

        // DynamicExpression
        // GotoExpression

        private bool VisitIndexExpression(IndexExpression x, IndexExpression y)
            => x.Indexer == y.Indexer && 
               Equals(x.Object, y.Object) &&
               ExpressionSequencesEqual(x.Arguments, y.Arguments);

        // InvocationExpression
        // LabelExpression
        // ListInitExpression
        // LoopExpression

        private bool VisitLambda(LambdaExpression x, LambdaExpression y)
            => x.ReturnType == y.ReturnType &&
               ExpressionSequencesEqual(x.Parameters, y.Parameters) &&
               Equals(x.Body, y.Body);

        private bool VisitMember(MemberExpression x, MemberExpression y)
            => x.Member == y.Member && Equals(x.Expression, y.Expression);

        // MemberInitExpression

        private bool VisitMethodCall(MethodCallExpression x, MethodCallExpression y)
            => x.Method == y.Method && 
                Equals(x.Object, y.Object) &&
                ExpressionSequencesEqual(x.Arguments, y.Arguments);

        // NewArrayExpression
        private bool VisitNewArray(NewArrayExpression x, NewArrayExpression y)
            => x.Type == y.Type &&
               ExpressionSequencesEqual(x.Expressions, y.Expressions);

        private bool VisitNew(NewExpression x, NewExpression y)
            => x.Constructor == y.Constructor &&
               ExpressionSequencesEqual(x.Arguments, y.Arguments) &&
               Enumerable.SequenceEqual(x.Members, y.Members);
               
        // RuntimeVariablesExpression
        // SwitchExpression
        // TryExpression

        private bool VisitTypeBinary(TypeBinaryExpression x, TypeBinaryExpression y)
            => x.TypeOperand == y.TypeOperand && Equals(x.Expression, y.Expression);

        private bool VisitUnary(UnaryExpression x, UnaryExpression y)
            => x.Method == y.Method && Equals(x.Operand, y.Operand);

        /// <summary>
        /// Returns true if and only if two sequences have the same length
        /// and each of their elements are structurally equal.
        /// </summary>
        private bool ExpressionSequencesEqual(ReadOnlyCollection<Expression> x,
                                              ReadOnlyCollection<Expression> y)
        {
            int n = x.Count;            
            if (n != y.Count)
                return false;

            for (int i = 0; i < n; ++i)
            {
                if (!Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if and only if two sequences have the same length
        /// and each of their elements are structurally equal.
        /// </summary>
        private bool ExpressionSequencesEqual(ReadOnlyCollection<ParameterExpression> x,
                                              ReadOnlyCollection<ParameterExpression> y)
        {
            int n = x.Count;            
            if (n != y.Count)
                return false;

            for (int i = 0; i < n; ++i)
            {
                if (!Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }
    }
}

