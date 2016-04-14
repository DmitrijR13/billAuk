namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    /// <summary>Значение параметра адрес</summary>
    public class BankSupplierParameterValue
    {
        /// <summary>Районы</summary>
        public List<int> Banks { get; set; }

        /// <summary>Агенты</summary>
        public List<int> Agents { get; set; }

        /// <summary>Принципалы</summary>
        public List<int> Principals { get; set; }

        /// <summary>Поставщики</summary>
        public List<int> Suppliers { get; set; }

        
    }
}