namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    using Bars.KP50.Utils.Annotations;

    public static class ExceptionHelper
    {
        private static readonly Dictionary<string, List<Exception>> _exceptionCache = new Dictionary<string, List<Exception>>();

        private static List<Exception> GetCacheList(string key)
        {
            ArgumentChecker.NotNullOrEmptyOrWhitespace(key, "key");
            List<Exception> list = null;
            if (!_exceptionCache.TryGetValue(key, out list))
            {
                list = new List<Exception>();
                _exceptionCache[key] = list;
            }

            return list;
        }

        private static void RemoveCacheList(string cacheKey)
        {
            ArgumentChecker.NotNullOrEmptyOrWhitespace(cacheKey, "cacheKey");
            List<Exception> list;
            if (_exceptionCache.TryGetValue(cacheKey, out list))
            {
                list.Clear();
                _exceptionCache[cacheKey] = null;
            }
        }

        public static void Cache(string key, Exception exc)
        {
            ArgumentChecker.NotNullOrEmptyOrWhitespace(key, "key");
            ArgumentChecker.NotNull(exc, "exc");

            GetCacheList(key).Add(exc);
        }

        [TerminatesProgram]
        public static void Throw(string cacheKey, string message = "", bool includeStackTraceinMessage = false)
        {
            ArgumentChecker.NotNullOrEmptyOrWhitespace(cacheKey, "cacheKey");
            var msg = string.IsNullOrEmpty(message) ? "There are several errors accured" : message;

            var list = GetCacheList(cacheKey);
            if (list.Count > 0)
            {
                for(var index = 0; index < list.Count; index++)
                {
                    msg += "\r\n\r\n";
                    msg += string.Format("{0}) {1}", index + 1, list[index].Message);
                    if (includeStackTraceinMessage)
                    {
                        msg += "\r\n\r\nStack trace for exception:\r\n\r\n";
                        msg += list[index].StackTrace;
                    }
                }

                RemoveCacheList(cacheKey);

                Throw<InvalidOperationException>(msg);
            }
        }

        [TerminatesProgram]
        public static void Throw<T>()
            where T : Exception
        {
            Exception exception = null;

            try
            {
                exception = Activator.CreateInstance<T>();
            }
            catch
            {
                exception = null;
            }

            if (exception == null)
            {
                exception = new InvalidOperationException();
            }

            throw exception;
        }

        [TerminatesProgram]
        public static void Throw<T>(string format, params object[] parameters)
            where T: Exception
        {
            Exception exception = null;
            var message = "Invalid operation";
            if (!string.IsNullOrEmpty(format))
            {
                message = parameters == null || parameters.Length == 0 
                    ? format 
                    : string.Format(format, parameters);
            }

            try
            {
                exception = Activator.CreateInstance(typeof(T), message) as Exception;
            }
            catch
            {
                exception = null;
            }

            if (exception == null)
            {
                exception = new InvalidOperationException(message);
            }

            throw exception;
        }

        [TerminatesProgram]
        public static void Throw<T>(Exception innerException, string format, params object[] parameters)
        {
            Exception exception = null;
            var message = "Invalid operation";
            if (!string.IsNullOrEmpty(format))
            {
                message = parameters == null || parameters.Length == 0
                    ? format
                    : string.Format(format, parameters);
            }

            try
            {
                exception = Activator.CreateInstance(typeof(T), message, innerException) as Exception;
            }
            catch
            {
                exception = null;
            }

            if (exception == null)
            {
                exception = new InvalidOperationException(message, innerException);
            }

            throw exception;
        }

        [TerminatesProgram]
        public static void Throw(Exception exception)
        {
            throw exception;
        }

        private static string ExceptionSpecific(Exception exception)
        {
            var specific = new StringBuilder();
            var index = 1;

            if (exception is ReflectionTypeLoadException)
            {
                var ex = exception as ReflectionTypeLoadException;
                specific
                    .AppendLine()
                    .AppendLine("TypeLoaderExceptions : ")
                    .AppendLine();

                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    specific.AppendFormat("{0}. {1}", index++, loaderEx.Message);
                }
            }

            return specific.ToString();
        }

        public static string FullMessage(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            var message = new StringBuilder();

            message.AppendLine();
            var ex = exception;
            while (ex != null)
            {
                var exceptionData = new StringBuilder();
                var index = 1;
                foreach (var key in ex.Data.Keys)
                {
                    exceptionData.AppendFormat("{0}. {1} = {2}", index++, key, ex.Data[key]);
                }

                message.AppendLine()
                    .AppendLine("-------------------")
                    .AppendLine("Exception Type : " + ex.GetType().FullName)
                    .AppendLine()
                    .AppendLine("Message : " + ex.Message)
                    .AppendLine()
#if !SILVERLIGHT
                    .AppendLine("Method : " + ex.With(e => e.TargetSite).Return(t => t.Name, "Unknown"))
                    .AppendLine()
                    .AppendLine("Source : " + ex.Source)
                    .AppendLine()
#endif
                    .AppendLine("Exception Specific : ")
                    .AppendLine()
                    .AppendLine(ExceptionSpecific(ex))
                    .AppendLine()
                    .AppendLine("StackTrace : ")
                    .AppendLine()
                    .AppendLine(ex.StackTrace)
                    .AppendLine()
                    .AppendLine("Data : ")
                    .AppendLine()
                    .AppendLine(exceptionData.ToString());

                ex = ex.InnerException;
            }

            return message.ToString();
        }
    }
}
