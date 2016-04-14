namespace Bars.KP50.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Вспомогательный класс с методами конвертации
    /// значений в ращличные типы
    /// </summary>
    public static class ConvertHelper
    {
        /// <summary>
        /// Преобразование значения в перечисление
        /// </summary>
        /// <param name="value"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static object ToEnum(object value, Type toType)
        {
            var valueType = value.GetType();
            var enumType = Enum.GetUnderlyingType(toType);
            if (enumType == valueType)
            {
                return Convert.ChangeType(valueType, toType, CultureInfo.InvariantCulture);
            }

            object result = null;

            if (value.Is<string>())
            {
                result = Enum.Parse(toType, value.ToString(), true);
            }

            if (result == null)
            {
                try
                {
                    var enumTypeValue = Convert.ChangeType(value, enumType, CultureInfo.InvariantCulture);

                    result = Enum.ToObject(toType, enumTypeValue);

                    return result;
                }
                catch
                {
                    value = value.ToString();
                    valueType = typeof(string);
                }
            }

            if (valueType == typeof(string))
            {
                var strValue = value as string;
                if (string.IsNullOrEmpty(strValue))
                {
                    return null;
                }

                result = Enum.Parse(toType, strValue, true);
            }

            return result;
        }

        /// <summary>
        /// Метод преобразования переданного значения в определенный тип
        /// </summary>
        /// <param name="value">Конвертируемое значение</param>
        /// <param name="toType">Тип, в который необходимо преобразовать значение</param>
        /// <returns></returns>
        public static object ConvertTo(object value, Type toType)
        {

            if (value == null)
            {
                return toType.IsValueType ? Activator.CreateInstance(toType) : null;
            }

            var valueType = value.GetType();

            if (toType == typeof(object) || valueType == toType)
            {
                return value;
            }

            if (toType == typeof(string))
            {                
                return value.ToString();
            }

            if (toType.IsValueType && value.ToString().IsEmpty())
            {
                return Activator.CreateInstance(toType);
            }

            if (toType.IsNullable())
            {
                var nullType = toType.GetGenericArguments().First();
                return ConvertTo(value, nullType);
            }

            if (toType.Is<bool>() && valueType.Is<string>())
            {
                switch(value.ToString().ToLower())
                {
                    case "1":
                    case "true":
                    case "on":
                        return true;                        
                }

            }

            object result;
           
            try
            {
                if ((valueType == toType)
                    || ((toType.IsGenericType == valueType.IsGenericType)
                    && toType.IsAssignableFrom(valueType)))
                {
                    return value;
                }

                if (toType.IsEnum)
                {
                    return ToEnum(value, toType);
                }

                if (toType.Is<DateTime>())
                {
                    DateTime date;
                    if (!DateTime.TryParse(value.ToString(), out date))
                    {
                        date = DateTime.MinValue;
                    }

                    return date;
                }

                if (toType.IsNot<string>() && toType.IsEnumerable())
                {
                    var toArray = toType.Is<Array>();

                    if (!toType.IsGenericType && !toArray)
                    {
                        result = value;
                    }
                    else
                    {
                        var underlyingType = toArray
                                                 ? toType.GetElementType()
                                                 : toType.GetGenericArguments()[0];

                        var listType = typeof(List<>).MakeGenericType(underlyingType);

                        var resultList = Activator.CreateInstance(listType) as IList;
                        IEnumerable valueEnumerator = null;

                        if (valueType.IsEnumerable() && valueType.IsNot<string>())
                        {
                            valueEnumerator = value.As<IEnumerable>();
                        }
                        else
                        {
                            valueEnumerator = new object[] { value };
                        }
                        
                        if (resultList != null && valueEnumerator != null)
                        {
                            foreach (var listItem in
                                from object valueItem in valueEnumerator
                                select ConvertTo(valueItem, underlyingType))
                            {
                                resultList.Add(listItem);
                            }
                        }

                        if (resultList != null && toArray)
                        {
                            var array = (Array)Activator.CreateInstance(toType, resultList.Count);
                            resultList.CopyTo(array, 0);

                            result = array;
                        }
                        else
                        {
                            result = resultList;
                        }
                    }
                }
                else
                {
                    if (valueType.Is<string>() && toType.IsValueType && (toType.Is<float>() || toType.Is<double>() || toType.Is<decimal>()))
                    {
                        var inv = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
                        var str = value.ToString().Replace(",", inv).Replace(".", inv);
                        result = Convert.ChangeType(str, toType, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        result = Convert.ChangeType(value, toType, CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
                result = null;
            }

            return result;
        }

        public static T ConvertTo<T>(object value)
        {
            var res = ConvertTo(value, typeof (T)) ?? default(T);

            return (T) res;
        }
    }
}
