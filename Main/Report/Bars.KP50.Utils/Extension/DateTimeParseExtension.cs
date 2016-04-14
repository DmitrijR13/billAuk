namespace Bars.KP50.Utils
{
    using System;
    using System.Globalization;

    public static class DateTimeParseExtension
    {
        /// <summary>
        /// The to date time.
        /// </summary>
        /// <param name="obj">
        /// The obj. 
        /// </param>
        /// <param name="ci">used culture</param>
        /// <returns>
        /// </returns>
        public static DateTime ToDateTime(this object obj, CultureInfo ci)
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

            if (DateTime.TryParse(obj.ToString(), ci, DateTimeStyles.None, out dateTimeValue))
            {
                return dateTimeValue;
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Возвращает исходную дату с последним днем месяца
        /// пример: (2015,06,01) -> (2015,06,30)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime WithLastDayMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
        }

        /// <summary>
        /// Возвращает исходную дату с первым днем месяца
        /// пример: (2015,06,30) -> (2015,06,01) 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime WithFirstDayMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static string ToShortDateStringWithQuote(this DateTime dt, string quote = "'")
        {
            return string.Format("{0}{1}{0}", quote, dt.ToShortDateString());
        }

    }
}
