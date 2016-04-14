namespace Bars.KP50.Report
{
    using Bars.KP50.Utils;

    /// <summary>Значение параметра отчета</summary>
    public class UserParamValue
    {
        /// <summary>Код параметра</summary>
        public string Code { get; set; }

        /// <summary>Значение</summary>
        public object Value { get; set; }

        /// <summary>Получить значение приведенное к типу</summary>
        /// <typeparam name="T">Тип приведения</typeparam>
        /// <returns>Типизированное значение</returns>
        public T GetValue<T>()
        {
            return Value.To<T>();
        }
    }
}