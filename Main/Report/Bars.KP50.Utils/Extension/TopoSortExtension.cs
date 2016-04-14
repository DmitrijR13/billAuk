// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TopoSortExtension.cs" company="">
//   
// </copyright>
// <summary>
//   The bubble sort extension.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The bubble sort extension.
    /// </summary>
    public static class BubbleSortExtension
    {
        /// <summary>
        /// The topo sort.
        /// </summary>
        /// <param name="elements">
        /// The elements. 
        /// </param>
        /// <param name="dependencies">
        /// The dependencies. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IEnumerable<T> TopoSort<T>(this IEnumerable<T> elements, Func<T, IEnumerable<T>> dependencies)
        {
            var seen = new Dictionary<T, seen_type>();
            var result = new List<T>();

            elements.ForEach(x => Walk(x, seen, result, dependencies));

            return result;
        }

        /// <summary>
        /// The walk.
        /// </summary>
        /// <param name="element">
        /// The element. 
        /// </param>
        /// <param name="seen">
        /// The seen. 
        /// </param>
        /// <param name="result">
        /// The result. 
        /// </param>
        /// <param name="dependencies">
        /// The dependencies. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The walk. 
        /// </returns>
        /// <exception cref="ApplicationException">
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        private static bool Walk<T>(
            T element, Dictionary<T, seen_type> seen, List<T> result, Func<T, IEnumerable<T>> dependencies)
        {
            if (seen.ContainsKey(element))
            {
                switch (seen[element])
                {
                    case seen_type.seeing:
                        throw new ApplicationException(string.Format("Cyclic dependency involving {0}", element));
                    case seen_type.seen:
                        return true;
                    case seen_type.error:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            seen[element] = seen_type.seeing;
            bool ok = dependencies(element).All(x => Walk(x, seen, result, dependencies));

            if (ok)
            {
                seen[element] = seen_type.seen;
                result.Add(element);
            }
            else
            {
                seen[element] = seen_type.error;
            }

            return ok;
        }

        #region Nested type: seen_type

        /// <summary>
        /// The seen_type.
        /// </summary>
        private enum seen_type
        {
            /// <summary>
            ///   The seeing.
            /// </summary>
            seeing,

            /// <summary>
            ///   The seen.
            /// </summary>
            seen,

            /// <summary>
            ///   The error.
            /// </summary>
            error
        };

        #endregion
    }
}