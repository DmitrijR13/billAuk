namespace Bars.KP50.Report
{
    using System.Collections.Generic;
    using System.Linq;

    using Bars.KP50.Report.Base;

    /// <summary>Провайдер отчетов</summary>
    public class ReportProvider : IReportProvider
    {
        public ReportProvider()
        {
            Reports = new List<ReportInfo>();
        }

        /// <summary>Список отчетов</summary>
        protected List<ReportInfo> Reports { get; set; }

        /// <summary>Зарегистрировать отчет</summary>
        /// <param name="report">Отчет</param>
        public void RegisterReport(IBaseReport report)
        {
            Reports.Add(new ReportInfo(report));
        }

        /// <summary>Получить список описаний отчетов</summary>
        /// <returns>Список отчетов</returns>
        public IList<ReportInfo> GetReports()
        {
            return Reports;
        }

        /// <summary>Получить описание отчета</summary>
        /// <param name="code">Код отчета</param>
        /// <returns>Описание отчета</returns>
        public ReportInfo GetReport(string code)
        {
            return Reports.FirstOrDefault(x => x.Code == code);
        }
    }
}