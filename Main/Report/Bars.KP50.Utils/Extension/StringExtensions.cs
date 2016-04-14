namespace Bars.KP50.Utils
{
    using System.Globalization;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Bars.KP50.Utils.Annotations;

    public static class StringExtensions
    {        
        public static string ComputeHash(this string str, string hashMethod = null)
        {            
            return
                hashMethod
                    .If(x => x.IsNotEmpty())
                    .ReturnFn(System.Security.Cryptography.MD5.Create, System.Security.Cryptography.MD5.Create)
                    .ComputeHash(str.ToUtf8Bytes())
                    .AsBase64String();
        }

        public static string AsBase64String(this byte[] array)
        {
            if (array == null || array.Length == 0)
            {
                return null;
            }

            return Convert.ToBase64String(array);
        }

        public static string AsString(this byte[] array)
        {
            string result = null;

            if (array != null)
            {
                using (var stream = new MemoryStream(array))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }

        public static byte[] ToUtf8Bytes(this string str)
        {
            return str == null ? new byte[0] : Encoding.UTF8.GetBytes(str);
        }

        public static string[] Split(this string str, StringSplitOptions options, params string[] delimiters)
        {
            return str.IsEmpty() ? new string[0] : str.Split(delimiters, options);
        }

        public static string[] Split(this string str, int length, bool byWords = false)
        {
            if (str.IsEmpty() || length <= 0)
            {
                return new string[0];
            }

            if (str.Length <= length)
            {
                return new[] { str };
            }

            if (byWords)
            {
                var lines = new List<string>();
                var parts =
                    str.Split(StringSplitOptions.RemoveEmptyEntries, "\t", "\r\n", "\n", " ").Select(x => x.Trim());

                var line = "";
                var tpl = "{0} {1}";
                foreach (var part in parts)
                {
                    var newValue = tpl.FormatUsing(line, part);
                    if (newValue.Length > length)
                    {
                        lines.Add(line);
                        line = "";
                    }

                    line = tpl.FormatUsing(line, part).Trim();
                }

                if (line.IsNotEmpty())
                {
                    lines.Add(line);
                }

                return lines.ToArray();
            }
            else
            {
                return
                    Enumerable.Range(0, str.Length / length + 1).Select(
                        i => str.Substring(i * length, Math.Min(length, str.Length - i * length))).Select(x => x.Trim())
                        .ToArray();
            }
        }

        public static string[] Split(this string str, params string[] delimiters)
        {
            return str.IsEmpty() ? new string[0] : str.Split(StringSplitOptions.RemoveEmptyEntries, delimiters);
        }

        public static string FormatUsing(this string format, CultureInfo culture, params object[] args)
        {
            return format.IsNotEmpty() ? string.Format(culture, format, args) : null;
        }

        public static string FormatUsing(this string format, params object[] args)
        {            
            return format.IsNotEmpty() ? (args.IsEmpty() ? format : string.Format(CultureInfo.InvariantCulture, format, args)) : null;
        }
        
        [AssertionMethod]
        public static bool IsEmpty([AssertionCondition(AssertionConditionType.IS_NULL)]this string str)
        {
            return string.IsNullOrEmpty(str) || str == " ";
        }

        public static bool IsNotEmpty(this string str)
        {
            return !IsEmpty(str);
        }

        public static string Or(this string str, string other)
        {
            return str.IsEmpty() ? other : str;
        }

        public static string Or(this string str, Func<string> other)
        {
            return str.IsEmpty() ? other() : str;
        }

        public static bool IsOneOf(this string str, params string[] list)
        {
            return list.Any(s => s.Equals(str));
        }

        public static bool ContainsOneOf(this string str, params string[] list)
        {
            return list.Any(str.Contains);
        }
        
        public static bool EqualsIgnoreCase(this string str, params string[] value)
        {
            return value.Any(x => str.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        public static bool ContainsIgnoreCase(this string str, params string[] value)
        {
            if (str.IsEmpty())
            {
                return false;
            }

            var mainStr = str.IsEmpty()
                  ? string.Empty
                  : str.Trim().ToLower();

            return value != null && value
                                        .Select(x => x.IsEmpty() ? string.Empty : x.Trim().ToLower())
                                        .Any(mainStr.Contains);
        }

        public static bool ContainsAllIgnoreCase(this string str, params string[] value)
        {
            if (str.IsEmpty())
            {
                return false;
            }

            var mainStr = str.IsEmpty()
                              ? string.Empty
                              : str.Trim().ToLower();

            return value != null && value
                                        .Select(x => x.IsEmpty() ? string.Empty : x.Trim().ToLower())
                                        .All(mainStr.Contains);
        }

        public static bool StartsWithIgnoreCase(this string str, params string[] value)
        {
            return value != null && value.Any(x => str.StartsWith(x, StringComparison.OrdinalIgnoreCase));
        }

        public static bool EndsWithIgnoreCase(this string str, params string[] value)
        {
            return value != null && value.Any(x => str.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }

        public static bool NotEmpty(string str)
        {
            return !string.IsNullOrEmpty(str) && str != " ";
        }

        public static string NotEmpty(params string[] options)
        {
            if (options == null)
            {
                return null;
            }

            return options.FirstOrDefault(x => NotEmpty(string.IsNullOrEmpty(x) ? string.Empty : x.Trim()));
        }          
     
        public static string Append(this string str, string value, string delim = "")
        {            
            if (str.IsEmpty())
                return value;

            value = value.IsEmpty()
                        ? ""
                        : value;

            var app = delim.IsEmpty()
                          ? value
                          : value.StartsWith(delim)
                                ? value.Substring(delim.Length)
                                : value;

            return str + (str.IsEmpty() || str.EndsWith(delim)
                              ? ""
                              : delim.Or("")) + app;
        }
    }
}
