namespace Bars.KP50.Report
{
    using Bars.KP50.Utils;

    /// <summary>Параметр адрес</summary>
    public class BankSupplierParameter : UserParam
    {
        public BankSupplierParameter()
        {
            TypeValue = typeof(BankSupplierParameterValue);
            Name = string.Empty;
            Code = "BankSupplier";
            JavascriptClassName = "Bars.KP50.report.BankSupplierField";
            Require = true;
        }

        public static BankSupplierParameterValue GetValue(string value)
        {
            return value.To<BankSupplierParameterValue>();
        }
    }
}