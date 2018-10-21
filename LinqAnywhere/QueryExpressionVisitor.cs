using System.Linq.Expressions;


namespace LinqAnywhere
{
    internal class QueryExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression e)
        {
            // Match against Where clause here?

            return e;
        }
    }
}
