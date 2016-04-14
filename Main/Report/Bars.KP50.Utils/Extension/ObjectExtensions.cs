namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Bars.KP50.Utils.Annotations;

    public static class ObjectExtensions
    {
        public static bool Is(this object obj, Type type)
        {
            ArgumentChecker.NotNull(type, "type");

            return obj != null && obj.GetType().Is(type);
        }

        public static bool Is<T>(this object obj)
        {
            return Is(obj, typeof(T));
        }

        public static bool IsNot<T>(this object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return !obj.Is<T>();
        }

        public static T As<T>(this object obj)            
        {
            if (obj.Is<T>())
                return (T)obj;

            return default(T);
        }

        public static T CastAs<T>(this object obj)
        {
            return (T)obj;
        }

        public static string ToInvariantString(this object obj, string format = null)
        {            
            return string.Format(
                CultureInfo.InvariantCulture,
                format.If(x => x.IsNotEmpty()).Return(x => x.Contains("{") ? x : "{0:" + format + "}", "{0}"),
                obj);
        }

        public static string ToLowerInvariantString(this object obj, string format = null)
        {
            return obj.ToInvariantString(format).ToLower();
        }

        public static TInput? AsNullable<TInput>(this TInput o)
            where TInput : struct
        {
            return o;
        }

        public static TResult With<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator)
            where TResult : class
            where TInput : class
        {
            if (o == null)
            {
                return null;
            }

            TResult result;
            try
            {
                result = evaluator(o);
            }
            catch
            {
                result = null;
            }

            return result;
        }

        public static TResult Return<TInput, TResult>(
            this TInput o,
            Func<TInput, TResult> evaluator, 
            TResult failureValue = default(TResult))            
        {
            if (o == null)
            {
                return failureValue;
            }

            return evaluator(o);
        }
        
        public static TResult IIf<TInput, TResult>(
            this TInput o,
            Func<TInput, bool> evaluator,
            TResult trueValue = default(TResult),
            TResult falseValue = default(TResult))
            where TInput : class
        {
            if (o == null)
            {
                return falseValue;
            }

            return evaluator(o) ? trueValue : falseValue;
        }

        public static TResult ReturnFn<TInput, TResult>(
            this TInput o,
            Func<TInput, TResult> evaluator, 
            Func<TResult> failureValue)
            where TInput : class
        {
            return o == null ? failureValue() : evaluator(o);
        }

        public static TResult ReturnSafe<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator)
            where TInput : class
        {
            if (o == null)
                return default(TResult);

            TResult result;
            try
            {
                result = evaluator(o);
            }
            catch
            {
                result = default(TResult);
            }

            return result;
        }

        [AssertionMethod]
        public static bool IsNull<TInput>([AssertionCondition(AssertionConditionType.IS_NULL)]this TInput o)
            where TInput : class
        {
            return o == null;
        }

        [AssertionMethod]
        public static bool IsNotNull<TInput>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)]this TInput o)
            where TInput : class
        {
            return o != null;
        }

        public static TInput If<TInput>(this TInput o, Func<TInput, bool> evaluator)            
        {
            if (o == null)
            {
                return default(TInput);
            }

            return evaluator(o) ? o : default(TInput);
        }

        public static TInput Unless<TInput>(this TInput o, Func<TInput, bool> evaluator)
               where TInput : class
        {
            return o == null ? null : (evaluator(o) ? null : o);
        }

        public static TInput Do<TInput>(this TInput o, Action<TInput> action)
            where TInput : class
        {
            if (o == null)
            {
                return null;
            }

            action(o);

            return o;
        }

        public static TInput AddTo<TInput>(this TInput o, IList<TInput> container)
            where TInput : class
        {
            if (o != null)
                container.Add(o);

            return o;
        }

        public static TInput OrIfNull<TInput>(this TInput o, TInput r)
            where TInput : class
        {
            return o ?? r;
        }        

        public static TInput OrIfNull<TInput>(this TInput o,
            Func<TInput> r)
            where TInput : class 
        {
            return o ?? r();
        }

        public static TEnum DoEnumerate<TEnum, TItem>(this TEnum o, Action<TItem> action)
            where TEnum : class, IEnumerable<TItem>
        {
            if (o.IsEmpty())
            {
                return o;
            }

            foreach (var item in o)
            {
                action(item);
            }            

            return o;
        }

        public static TReturn ExprBind<TInput, TReturn>(this TInput value, Func<TInput, TReturn> fn)
        {
            return fn(value);
        }
    }
}
