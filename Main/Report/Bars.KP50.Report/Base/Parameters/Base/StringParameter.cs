namespace Bars.KP50.Report
{
    /// <summary>Строковый параметр</summary>
    public class StringParameter : UserParam
    {
        public StringParameter()
        {
            TypeValue = typeof(string);
            JavascriptClassName = "Bars.KP50.report.TextField";
        }
    }
}