namespace Bars.KP50.Report
{
    using Bars.KP50.Utils;

    /// <summary>Параметр адрес</summary>
    public class AddressParameter : UserParam
    {
        public AddressParameter()
        {
            TypeValue = typeof(AddressParameterValue);
            Name = string.Empty;
            Code = "Address";
            JavascriptClassName = "Bars.KP50.report.AddressField";
            Require = true;
        }

        public static AddressParameterValue GetValue(string value)
        {
            return value.To<AddressParameterValue>();
        }
    }
}