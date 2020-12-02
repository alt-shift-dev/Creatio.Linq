namespace Creatio.Linq
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Linq.Expressions;


    /// <summary>
    /// Helper method to get value from expression.
    /// Thanks to https://gist.github.com/jcansdale/3d4b860188723ea346621b1c51fd8461
    /// </summary>
    public static class ExpressionUtilities
    {
        public static object GetValue(Expression expression)
        {
            return GetValue(expression, true);
        }

        public static object GetValueWithoutCompiling(Expression expression)
        {
            return GetValue(expression, false);
        }

        static object GetValue(Expression expression, bool allowCompile)
        {
            if (expression == null)
            {
                return null;
            }

            if (expression is ConstantExpression constantExpression)
            {
	            return GetValue(constantExpression);
            }

            if (expression is MemberExpression memberExpression)
            {
	            return GetValue(memberExpression, allowCompile);
            }

            if (expression is MethodCallExpression methodCallExpression)
            {
	            return GetValue(methodCallExpression, allowCompile);
            }

            if (allowCompile)
            {
                return GetValueUsingCompile(expression);
            }

            throw new Exception("Couldn't evaluate Expression without compiling: " + expression);
        }

        static object GetValue(ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }

        private static object GetValue(MemberExpression memberExpression, bool allowCompile)
        {
            var value = GetValue(memberExpression.Expression, allowCompile);

            var member = memberExpression.Member;
            if (member is FieldInfo fieldInfo)
            {
	            return fieldInfo.GetValue(value);
            }

            if (member is PropertyInfo propertyInfo)
            {
	            try
                {
                    return propertyInfo.GetValue(value);
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }

            throw new Exception("Unknown member type: " + member.GetType());
        }

        private static object GetValue(MethodCallExpression methodCallExpression, bool allowCompile)
        {
            var paras = GetArray(methodCallExpression.Arguments, true);
            var obj = GetValue(methodCallExpression.Object, allowCompile);

            try
            {
                return methodCallExpression.Method.Invoke(obj, paras);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        static object[] GetArray(IEnumerable<Expression> expressions, bool allowCompile)
        {
            var list = new List<object>();
            foreach (var expression in expressions)
            {
                var value = GetValue(expression, allowCompile);
                list.Add(value);
            }

            return list.ToArray();
        }

        public static object GetValueUsingCompile(Expression expression)
        {
            var lambdaExpression = Expression.Lambda(expression);
            var dele = lambdaExpression.Compile();
            return dele.DynamicInvoke();
        }
    }
}