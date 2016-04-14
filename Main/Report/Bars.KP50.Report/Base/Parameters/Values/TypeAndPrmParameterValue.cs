using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Bars.KP50.Report
{
    /// <summary>Тип параметра, наименование параметра </summary>
    public class TypeAndPrmParameterValue
    {
        /// <summary>Тип параметра</summary>
        public List<int> TypePrm { get; set; }

        /// <summary>Номер параметра</summary>
        public List<int> NzpPrm { get; set; }
    }
}
