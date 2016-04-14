// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueryableExtention.cs" company="">
//   
// </copyright>
// <summary>
//   The queryable extention.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// The queryable extention.
    /// </summary>
    public static class QueryableExtention
    {
        /// <summary>
        /// The order if.
        /// </summary>
        /// <param name="query">
        /// The query. 
        /// </param>
        /// <param name="condition">
        /// The condition. 
        /// </param>
        /// <param name="asc">
        /// The asc. 
        /// </param>
        /// <param name="keySelector">
        /// The key selector. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <typeparam name="TKey">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IQueryable<T> OrderIf<T, TKey>(
            this IQueryable<T> query,
            bool condition,
            bool asc,
            Expression<Func<T, TKey>> keySelector)
        {
            if (condition)
            {
                if (asc)
                {
                    return query.OrderBy(keySelector);
                }

                return query.OrderByDescending(keySelector);
            }

            return query;
        }

        /// <summary>
        /// The order then if.
        /// </summary>
        /// <param name="query">
        /// The query. 
        /// </param>
        /// <param name="condition">
        /// The condition. 
        /// </param>
        /// <param name="asc">
        /// The asc. 
        /// </param>
        /// <param name="keySelector">
        /// The key selector. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <typeparam name="TKey">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IQueryable<T> OrderThenIf<T, TKey>(
            this IQueryable<T> query,
            bool condition,
            bool asc,
            Expression<Func<T, TKey>> keySelector)
        {
            if (condition)
            {
                if (asc)
                {
                    return ((IOrderedQueryable<T>)query).ThenBy(keySelector);
                }
                else
                {
                    return ((IOrderedQueryable<T>)query).ThenByDescending(keySelector);
                }
            }

            return query;
        }

        /// <summary>
        /// The where if.
        /// </summary>
        /// <param name="query">
        /// The query. 
        /// </param>
        /// <param name="condition">
        /// The condition. 
        /// </param>
        /// <param name="predicate">
        /// The predicate. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            if (condition)
            {
                return query.Where(predicate);
            }

            return query;
        }

        public static bool In<T>(this T @value,
            params T[] values)
        {
            return values != null && values.Contains(@value);
        }

        public static bool In<T>(this T @value,
            IQueryable<T> values)
        {
            return values != null && values.Contains(@value);
        }

        public static bool In<T>(this T @value,
            IEnumerable<T> values)
        {
            return values != null && values.Contains(@value);
        }

        public static bool NotIn<T>(this T @value,
            params T[] values)
        {
            return values == null || !values.Contains(@value);
        }

        public static bool NotIn<T>(this T @value,
            IQueryable<T> values)
        {
            return values == null || !values.Contains(@value);
        }

        public static bool NotIn<T>(this T @value,
            IEnumerable<T> values)
        {
            return values == null || !values.Contains(@value);
        }

        public static IQueryable<T> Distinct<T, V>(this IQueryable<T> query,
            Expression<Func<T, V>> memberExpression)
        {
            return query.Distinct(FnEqualityComparer<T>.Member(memberExpression));
        }

        public static IQueryable<T> Distinct<T>(this IQueryable<T> query, Func<T,T,bool> fn)
        {
            return query.Distinct(FnEqualityComparer<T>.Fn(fn));
        }
    }
}