namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class FnEqualityComparer<T> : IEqualityComparer<T>        
    {
        private readonly Func<T, T, bool> _equalFn;

        private FnEqualityComparer(Func<T, T, bool> equalFn)
        {
            _equalFn = equalFn;
        }

        #region Implementation of IEqualityComparer<in T>

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <paramref name="T"/> to compare.</param><param name="y">The second object of type <paramref name="T"/> to compare.</param>
        public bool Equals(T x,
            T y)
        {
            return _equalFn(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(T obj)
        {
            object v = obj;
            return v == null
                       ? typeof(T).GetHashCode()
                       : obj.GetHashCode();
        }

        #endregion

        public static IEqualityComparer<T> Fn(Func<T, T, bool> equalFn)
        {
            return new FnEqualityComparer<T>(equalFn);
        }

        public static IEqualityComparer<T> Member<V>(Expression<Func<T, V>> memberExp)
        {
            var memberName = memberExp.MemberName();
            var fnParam1 = Expression.Parameter(typeof(T), "x1");
            var fnProp1 = Expression.PropertyOrField(fnParam1, memberName);
            var fnParam2 = Expression.Parameter(typeof(T), "x2");
            var fnProp2 = Expression.PropertyOrField(fnParam2, memberName);

            var eq = Expression.Equal(fnProp1, fnProp2);

            var lambda = Expression.Lambda<Func<T, T, bool>>(eq, fnParam1, fnParam2);

            var fn = lambda.Compile();
            return new FnEqualityComparer<T>(fn);
        }
    }
}