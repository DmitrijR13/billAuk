namespace Bars.KP50.Report
{
    using System;

    using Bars.KP50.Utils;

    /// <summary>Параметр месяц</summary>
    public class PeriodParameter : ComboBoxParameter
    {
        public PeriodParameter() 
        {
        }

        public PeriodParameter(DateTime date)
        {
            TypeValue = typeof(string);
            Name = "Период";
            Code = "Period";
            Require = true;
            JavascriptClassName = "Bars.KP50.report.PeriodField";

            Value = string.Empty;
            Value = date.ToString("dd.MM.yyyy");
        }
        public PeriodParameter(DateTime beginDate, DateTime endDate)
        {
            TypeValue = typeof(string);
            Name = "Период";
            Code = "Period";
            Require = true;
            JavascriptClassName = "Bars.KP50.report.PeriodField";
            Value = string.Empty;

            Value = beginDate.ToString("dd.MM.yyyy");


            Value += ";" + endDate.ToString("dd.MM.yyyy");
        }

        /// <summary>Получить значения периода</summary>
        /// <param name="value">Значене строкой</param>
        /// <param name="begin">Начало периода</param>
        /// <param name="end">Конец периода</param>
        public static void GetValues(string value, out DateTime begin, out DateTime end)
        {
            begin = new DateTime();
            end =  new DateTime();

            if (!string.IsNullOrEmpty(value))
            {
                var values = value.Split(',');
                if (values.Length == 2)
                {
                    begin = values[0].To<DateTime>();
                    end = values[1].To<DateTime>();
                }
            }
        }

        /// <summary>Получить значение даты</summary>
        /// <param name="value">Значене строкой</param>
        /// <param name="date">Начало периода</param>
        public static void GetValues(string value, out DateTime date)
        {
            date = new DateTime();

            if (!string.IsNullOrEmpty(value))
            {
                var values = value.Split(',');
                date = values[0].To<DateTime>();
            }
        }
    }
}