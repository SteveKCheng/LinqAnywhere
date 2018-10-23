using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqAnywhere
{

    public static class Compilation
    {
        private static bool IsAllowedPredicateNodeType(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    return true;

                default:
                    return false;
            }
        }

        public static bool MatchPredicate(this IndexColumnMatch column, 
                                          Expression subject,
                                          Expression term)
        {
            var operation = term.NodeType;

            if (!IsAllowedPredicateNodeType(operation))
                return false;

            var binaryExpr = (BinaryExpression)term;

            var columnDescriptor = column.ColumnDescriptor;

            var operandExpr = binaryExpr.Right;
            var reverseOperation = false;

            if (!columnDescriptor.MatchesExpression(subject, binaryExpr.Left))
            {
                if (!columnDescriptor.MatchesExpression(subject, binaryExpr.Right))
                    return false;

                reverseOperation = true;
                operandExpr = binaryExpr.Left;
            }

            if (operandExpr.NodeType != ExpressionType.Constant)
                return false;

            var operand = ((ConstantExpression)operandExpr).Value;

            var totalOrder = columnDescriptor.TotalOrder;

            Interval<object> interval;

            if (operation == ExpressionType.Equal)
            {
                interval = Interval.CreateSinglePoint(operand);
            }
            else
            {
                interval = Interval.CreateOneSidedBound(operand, 
                            operation == ExpressionType.GreaterThan ||
                            operation == ExpressionType.LessThan,
                            reverseOperation != (operation == ExpressionType.LessThan ||
                                                 operation == ExpressionType.LessThanOrEqual));
            }

            column.Interval = column.Interval.IntersectTypeErased(interval, totalOrder);
            return true;
        }
    }
}