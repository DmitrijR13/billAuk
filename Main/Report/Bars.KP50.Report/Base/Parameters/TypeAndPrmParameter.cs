using Bars.KP50.Utils;

// ReSharper disable once CheckNamespace
namespace Bars.KP50.Report
{
    public class TypeAndPrmParameter : UserParam
    {
        public TypeAndPrmParameter()
        {
            TypeValue = typeof(TypeAndPrmParameterValue);
            Name = string.Empty;
            Code = "TypeAndPrm";
            JavascriptClassName = "Bars.KP50.report.TypeAndPrmField";
            Require = true;
        }

        public static TypeAndPrmParameterValue GetValue(string value) {
            return value.To<TypeAndPrmParameterValue>();
        }
    }
}
