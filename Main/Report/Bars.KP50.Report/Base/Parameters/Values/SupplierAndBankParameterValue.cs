namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    /// <summary>Значение параметра адрес</summary>
    public class SupplierAndBankParameterValue
    {
        /// <summary>Банки</summary>
        public List<int> BanksList { get; set; }

        /// <summary>Поставщики</summary>
        public List<int> SuppliersList { get; set; }

    }
}