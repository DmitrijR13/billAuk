namespace Bars.KP50.Report
{
    using System;

    /// <summary>Параметр дата</summary>
    public class DateTimeParameter : UserParam
    {
        public DateTimeParameter()
        {
            TypeValue = typeof(DateTime);
            JavascriptClassName = "Bars.KP50.report.DateTimeField";
        }
    }
}