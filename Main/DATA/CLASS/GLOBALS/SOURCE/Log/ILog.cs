namespace Globals.SOURCE
{
    using System;

    /// <summary>Интерфейс работы с логом</summary>
    public interface ILog
    {
        void Fatal(string message);

        void Fatal(string message, Exception e);

        void FatalFormat(string format, params object[] args);

        void FatalFormat(IFormatProvider formatProvider, string format, params object[] args);

        void FatalFormat(Exception e, string format, params object[] args);

        void FatalFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args);

        void Error(string message);

        void Error(string message, Exception e);

        void ErrorFormat(string format, params object[] args);

        void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args);

        void ErrorFormat(Exception e, string format, params object[] args);

        void ErrorFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args);

        void Warning(string message);

        void Warning(string message, Exception e);

        void WarningFormat(string format, params object[] args);

        void WarningFormat(IFormatProvider formatProvider, string format, params object[] args);

        void WarningFormat(Exception e, string format, params object[] args);

        void WarningFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args);

        void Info(string message);

        void Info(string message, Exception e);

        void InfoFormat(string format, params object[] args);

        void InfoFormat(IFormatProvider formatProvider, string format, params object[] args);

        void InfoFormat(Exception e, string format, params object[] args);

        void InfoFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args);

        void Debug(string message);

        void Debug(string message, Exception e);

        void DebugFormat(string format, params object[] args);

        void DebugFormat(IFormatProvider formatProvider, string format, params object[] args);

        void DebugFormat(Exception e, string format, params object[] args);

        void DebugFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args);
    }
}