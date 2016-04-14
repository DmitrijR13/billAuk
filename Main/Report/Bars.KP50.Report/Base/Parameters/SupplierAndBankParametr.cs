namespace Bars.KP50.Report
{
    using Bars.KP50.Utils;

    /// <summary>Параметр адрес</summary>
    public class SupplierAndBankParameter : UserParam
    {
        public SupplierAndBankParameter()
        {
            TypeValue = typeof(AddressParameterValue);
            Name = string.Empty;
            Code = "SupplierAndBank";
            JavascriptClassName = "Bars.KP50.report.SupplierAndBankField";
            Require = true;
        }

        public static SupplierAndBankParameterValue GetValue(string value)
        {
            return value.To<SupplierAndBankParameterValue>();
        }
    }
}