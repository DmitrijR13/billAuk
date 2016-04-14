namespace Bars.KP50.Utils
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Вспомогательный класс с дополнительными
    /// методами взаимодействия через рефлексию
    /// </summary>
    public static class ReflectionExtensions
    {        
        /// <summary>
        /// Получение информации о свойстве
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static MemberInfo MemberInfo<TObject>(this Expression<Func<TObject, object>> expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("method");

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr = lambda.Body
                    .As<UnaryExpression>()
                    .Operand
                    .As<MemberExpression>();
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body.As<MemberExpression>();
            }

            return memberExpr == null ? null : memberExpr.Member;
        }

        /// <summary>
        /// Получение имени свойства
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression"></param>
        /// <param name="memberNameGet"></param>
        /// <returns></returns>
        public static string MemberName<TObject, TValue>(this Expression<Func<TObject, TValue>> expression, MemberNameGet memberNameGet = MemberNameGet.Last)
        {
            var lambda = expression as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("expression");

            MemberExpression memberExpr = null;

            switch (lambda.Body.NodeType)
            {
                case ExpressionType.Convert:
                    memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpr = lambda.Body as MemberExpression;
                    break;
            }

            if (memberExpr == null) return "";

            switch (memberNameGet)
            {
                case MemberNameGet.Last:
                    return memberExpr.Member.Name;
                default:
                    var delimiter = memberNameGet == MemberNameGet.Dotted ? "." : "";
                    var name = "";
                    while (memberExpr != null
                           && ((memberExpr.NodeType == ExpressionType.MemberAccess)
                               || (memberExpr.NodeType == ExpressionType.Convert)))
                    {
                        name = memberExpr.Member.Name
                               + (name.IsEmpty() ? "" : delimiter)
                               + name;

                        memberExpr = memberExpr.Expression as MemberExpression;
                    }

                    return name;
            }            
        }

        /// <summary>
        /// Получение имени свойства
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="memberNameGet"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string MemberName<TValue>(this Expression<Func<TValue>> expression, MemberNameGet memberNameGet = MemberNameGet.Last)
        {
            var lambda = expression as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("expression");

            MemberExpression memberExpr = null;

            switch (lambda.Body.NodeType)
            {
                case ExpressionType.Convert:
                    memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpr = lambda.Body as MemberExpression;
                    break;
            }

            if (memberExpr == null) return "";

            switch (memberNameGet)
            {
                case MemberNameGet.Last:
                    return memberExpr.Member.Name;
                default:
                    var delimiter = memberNameGet == MemberNameGet.Dotted ? "." : "";
                    var name = "";
                    while (memberExpr != null
                           && ((memberExpr.NodeType == ExpressionType.MemberAccess)
                               || (memberExpr.NodeType == ExpressionType.Convert)))
                    {
                        name = memberExpr.Member.Name
                               + (name.IsEmpty() ? "" : delimiter)
                               + name;

                        memberExpr = memberExpr.Expression as MemberExpression;
                    }

                    return name;
            }
        }
    }
}
