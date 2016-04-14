namespace Bars.KP50.Utils
{
    using System;
    using System.Globalization;

    using Newtonsoft.Json;

    /// <summary>
    /// The object parse extention.
    /// </summary>
    public static class ObjectParseExtention
    {
        public static T To<T>(this object obj)
        {
            return To(obj, default(T));
        }

        public static T To<T>(this object obj, T defaultValue)
        {
            if (obj == null)
            {
                return defaultValue;
            }

            if (obj is T)
            {
                return (T)obj;
            }

            Type type = typeof(T);

            if (type == typeof(string))
            {
                return (T)(object)obj.ToString();
            }

            if ((type.IsClass || type.IsCollection()) && obj is string)
            {
                return TryConvertFromJson<T>(obj as string);
            }

            Type underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                return To(obj, defaultValue, underlyingType);
            }

            return To(obj, defaultValue, type);
        }

        public static bool ToBool(this object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is bool)
            {
                return (bool)obj;
            }

            bool boolValue;

            if (bool.TryParse(obj.ToString(), out boolValue))
            {
                return boolValue;
            }

            int intValue;

            if (int.TryParse(obj.ToString(), out intValue))
            {
                return intValue != 0;
            }

            return false;
        }

        public static DateTime ToDateTime(this object obj)
        {
            if (obj == null)
            {
                return DateTime.MinValue;
            }

            if (obj is DateTime)
            {
                return (DateTime)obj;
            }

            DateTime dateTimeValue;

            if (DateTime.TryParse(obj.ToString(), out dateTimeValue))
            {
                return dateTimeValue;
            }

            return DateTime.MinValue;
        }

        public static decimal ToDecimal(this object obj)
        {
            if (obj == null)
            {
                return 0M;
            }

            if (obj is decimal)
            {
                return (decimal)obj;
            }

            string decimalString = obj.ToString();

            if (decimalString == string.Empty)
            {
                return 0M;
            }

            string value = decimalString.Replace(
                CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator,
                CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);

            decimal decValue;

            if (decimal.TryParse(value, out decValue))
            {
                return decValue;
            }

            return 0M;
        }

        public static double ToDouble(this object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (obj is double)
            {
                return (double)obj;
            }

            double dbValue;

            string value = obj.ToString().Replace(
                CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator,
                CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);

            if (double.TryParse(value, out dbValue))
            {
                return dbValue;
            }

            return 0;
        }

        public static int ToInt(this object obj, int defaultValue)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return defaultValue;
            }

            if (obj is int)
            {
                return (int)obj;
            }

            int intValue;

            if (int.TryParse(obj.ToString(), out intValue))
            {
                return intValue;
            }

            return defaultValue;
        }

        public static int ToInt(this object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return 0;
            }

            if (obj is int)
            {
                return (int)obj;
            }

            if (obj is bool)
            {
                return (bool)obj ? 1 : 0;
            }

            int intValue;

            if (int.TryParse(obj.ToString(), out intValue))
            {
                return intValue;
            }

            return 0;
        }

        public static string ToStr(this object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            if (obj is string)
            {
                return (string)obj;
            }

            return obj.ToString();
        }

        private static T TryConvertFromJson<T>(string obj)
        {
            return JsonConvert.DeserializeObject<T>(obj);
        }

        private static T To<T>(object obj, T defaultValue, Type type)
        {
            if (type.IsEnum)
            {
                if (obj is decimal)
                {
                    return (T)Enum.Parse(type, obj.ToString());
                }

                if (obj is string)
                {
                    return (T)Enum.Parse(type, (string)obj);
                }

                if (obj is long)
                {
                    return (T)Enum.Parse(type, obj.ToString());
                }

                if (Enum.IsDefined(type, obj))
                {
                    return (T)Enum.Parse(type, obj.ToString());
                }

                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(obj, type);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}