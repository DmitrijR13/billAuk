namespace Bars.KP50.Report
{
    /// <summary>Количество строк в каждом отчете из архива, если строк в отчете меньше, то архив не формируется</summary>
    public class RowCountParameter : UserParam
    {
        public RowCountParameter()
        {
            Name = "Кол-во строк в архиве";
            Code = "RowCount";
            Require = true;
            Value = 50000;
            DefaultValue = 50000;
            JavascriptClassName = "Bars.KP50.report.ReportRowCount";
        }
    }
}