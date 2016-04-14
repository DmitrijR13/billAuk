// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableExtension.cs" company="">
//   
// </copyright>
// <summary>
//   The enumerable extension.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// The enumerable extension.
    /// </summary>
    public static class EnumerableExtension
    {
        /// <summary>
        /// The for each.
        /// </summary>
        /// <param name="enumeration">
        /// The enumeration. 
        /// </param>
        /// <param name="action">
        /// The action. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T, int> action)
        {
            var index = 0;
            foreach (T item in enumeration)
            {
                T local = item;
                action(local, index);
                index++;
            }
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defValue = default (TValue))
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = defValue;
            }

            return value;
        }

        public static TValue Get<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TValue defValue = default (TValue))
        {
            TValue value;
            var pair = dictionary.FirstOrDefault(x => Equals(x.Key, key));
            if (pair.Value != null && !pair.Value.Equals(default(TValue)))
            {
                value = pair.Value;
            }
            else
            {
                value = defValue;
            }

            return value;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Expression<Func<V>> expression)
    where V : class
        {
            var value = dictionary.Get(key).As<V>();
            if (value == null)
            {
                value = expression.Compile()();
                dictionary[key] = value;
            }

            return value;
        }

        public static T GetOrCreate<T>(this IDictionary<string, object> dictionary, string key, Expression<Func<T>> expression)
            where T : class
        {
            var value = dictionary.Get(key).As<T>();
            if (value == null)
            {
                value = expression.Compile()();
                dictionary[key] = value;
            }

            return value;
        }

        public static T GetOrCreate<T>(this IDictionary<string, object> dictionary, string key)
            where T : class, new()
        {
            return dictionary.GetOrCreate(key, () => new T());
        }

        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }

        public static bool IsNotEmpty<T>(this IEnumerable<T> collection)
        {
            return collection != null && collection.Any();
        }

        public static TDictionary Apply<TDictionary, TKey, TValue>(this TDictionary target, IDictionary<TKey, TValue> source)
            where TDictionary : class, IDictionary<TKey, TValue>
        {
            if (target == null || source == null || target == source)
                return target;

            foreach (var pair in source)
            {
                target[pair.Key] = pair.Value;
            }

            return target;
        }

        public static IDictionary<TKey, TValue> Apply<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            if (target == null || source == null || target == source)
                return target;

            foreach (var pair in source)
            {
                target[pair.Key] = pair.Value;
            }

            return target;
        }

        public static IDictionary<TKey, TValue> ApplyIf<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            if (target == null || source == null)
                return target;

            foreach (var pair in source)
            {
                if (!target.ContainsKey(pair.Key))
                    target[pair.Key] = pair.Value;
            }

            return target;
        }

        public static TResult Get<TKey, TValue, TResult>(this IDictionary<TKey, TValue> dictionary, TKey key, TResult defaultValue = default(TResult))
        {
            if (dictionary == null) return defaultValue;

            //var value = dictionary.Get<TKey, TValue>(key, default(TValue));
            var value = dictionary.Get<TKey, TValue>(key, ConvertHelper.ConvertTo<TValue>(defaultValue));

            var convertedValue = ConvertHelper.ConvertTo<TResult>(value);

            return convertedValue == null ? defaultValue : (TResult)convertedValue;
        }

        public static TResult Get<TResult>(this IDictionary<string, string> dictionary, string key, TResult defaultValue = default(TResult))
        {
            return Get<string, string, TResult>(dictionary, key, defaultValue);
        }

        public static void AddTo<T>(this IEnumerable<T> enumeration, ICollection<T> collection)
        {
            foreach (var value in enumeration)
            {
                collection.Add(value);
            }
        }

        public static void AddTo<T>(this IEnumerable<T> enumeration, List<T> collection)
        {
            collection.AddRange(enumeration);
        }

        public static IEnumerable<T> Distinct<T, V>(this IEnumerable<T> query,
    Expression<Func<T, V>> memberExpression)
        {
            return query.Distinct(FnEqualityComparer<T>.Member(memberExpression));
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> query, Func<T, T, bool> fn)
        {
            return query.Distinct(FnEqualityComparer<T>.Fn(fn));
        }
    }
}