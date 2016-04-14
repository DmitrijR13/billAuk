namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    /// <summary>Значение параметра адрес</summary>
    public class AddressParameterValue
    {
        /// <summary>Районы</summary>
        public List<int> Raions { get; set; }

        /// <summary>Улицы</summary>
        public List<int> Streets { get; set; }

        /// <summary>Дома</summary>
        public List<string> Houses { get; set; }
    }
}