namespace Bars.KP50.Report
{
    using System;

    /// <summary>Ошибка отчетной системы</summary>
    public class ReportException : ApplicationException
    {
        public ReportException(string message) : base(message)
        {
        }

        public ReportException(string message, Exception exc) : base(message, exc)
        {
        }
    }
}
