using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.ObjectModel;

namespace LinqAnywhere
{
    public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        public bool Equals(Expression x, Expression y)
        {
            return EqualsInternal(x, y);
        }

        public int GetHashCode(Expression obj)
        {
            throw new System.NotImplementedException();
        }

        public static bool EqualsInternal(Expression x, Expression y)
        {
            if ((x == null) != (y == null))
                return false;

            if (x == null)
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
                    return VisitBinary((BinaryExpression)x, (BinaryExpression)y);

                case ExpressionType.MemberAccess:
                    return VisitMember((MemberExpression)x, (MemberExpression)y);

                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)x, (ParameterExpression)y);

                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)x, (ConstantExpression)y);

                default:
                    return false;
            }
        }

        private static bool VisitConstant(ConstantExpression x, ConstantExpression y)
            => x.Value.Equals(y.Value);

        private static bool VisitBinary(BinaryExpression x, BinaryExpression y)
            => x.Method == y.Method && 
                EqualsInternal(x.Left, y.Left) &&
                EqualsInternal(x.Right, y.Right) &&
                EqualsInternal(x.Conversion, y.Conversion);

        private static bool VisitMember(MemberExpression x, MemberExpression y)
            => x.Member == y.Member && EqualsInternal(x.Expression, y.Expression);

        private static bool VisitMemberAssignment(MemberAssignment x, MemberAssignment y)
            => x.Member == y.Member && EqualsInternal(x.Expression, y.Expression);

        private static bool ExpressionsSequenceEqual(ReadOnlyCollection<Expression> x,
                                                     ReadOnlyCollection<Expression> y)
        {
            int n = x.Count;            
            if (n != y.Count)
                return false;

            for (int i = 0; i < n; ++i)
            {
                if (!EqualsInternal(x[i], y[i]))
                    return false;
            }

            return true;
        }

        private static bool VisitMethodCall(MethodCallExpression x, MethodCallExpression y)
            => x.Method == y.Method && 
                EqualsInternal(x.Object, y.Object) &&
                ExpressionsSequenceEqual(x.Arguments, y.Arguments);

        private static bool VisitParameter(ParameterExpression x, ParameterExpression y)
        {
            return true;
        }

        private static bool VisitTypeBinary(TypeBinaryExpression x, TypeBinaryExpression y)
            => x.TypeOperand == y.TypeOperand && EqualsInternal(x.Expression, y.Expression);
    }

/* 
    internal class ExpressionEqualityVisitor : ExpressionVisitor
    {
        

        protected override Expression VisitConstant(ConstantExpression node)
        {
            UpdateHash(node.Value);
            return base.VisitConstant(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            UpdateHash(node.Member);
            return base.VisitMember(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            UpdateHash(node.Member);
            return base.VisitMemberAssignment(node);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            UpdateHash((int)node.BindingType);
            UpdateHash(node.Member);
            return base.VisitMemberBinding(node);
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            UpdateHash((int)node.BindingType);
            UpdateHash(node.Member);
            return base.VisitMemberListBinding(node);
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            UpdateHash((int)node.BindingType);
            UpdateHash(node.Member);
            return base.VisitMemberMemberBinding(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            UpdateHash(node.Method);
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            UpdateHash(node.Constructor);
            return base.VisitNew(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            UpdateHash(node.Type);
            return base.VisitNewArray(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            UpdateHash(node.Type);
            return base.VisitParameter(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            return base.VisitTypeBinary(node);
        }
    }
     */

}

