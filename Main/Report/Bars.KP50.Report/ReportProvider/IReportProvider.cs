namespace Bars.KP50.Report
{
    using System.Collections.Generic;

    using Bars.KP50.Report.Base;

    /// <summary>Интерфейс провайдера отчетов</summary>
    public interface IReportProvider
    {
        /// <summary>Зарегистрировать отчет</summary>
        /// <param name="report">Отчет</param>
        void RegisterReport(IBaseReport report);

        /// <summary>Получить список описаний отчетов</summary>
        /// <returns>Список отчетов</returns>
        IList<ReportInfo> GetReports();

        /// <summary>Получить описание отчета</summary>
        /// <param name="code">Код отчета</param>
        /// <returns>Описание отчета</returns>
        ReportInfo GetReport(string code);
    }
}