using System.ServiceModel;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Globals.SOURCE.INTF.Report
{
    /// <summary>
    /// Интерфейс сервиса работы с отчетами
    /// </summary>
    [ServiceContract]
    public interface IReportService
    {
        /// <summary>
        /// Формирует отчет
        /// </summary>
        /// <param name="reportId">идентификатор отчета</param>
        /// <param name="userParams">пользовательские параметры</param>
        /// <param name="userFilters">пользовательские фильтры</param>
        /// <param name="curReportKind">текущий тип отчета</param>
        /// <param name="userName">Пользователь вызвавший отчет</param>
        /// <param name="nzpObject">Идентификатор объекта</param>
        /// <returns></returns>
        [OperationContract]
        ReturnsObjectType<ReportResult> PrintReport(string reportId, string userParams, string userFilters, int curReportKind, string userName, int nzpObject);
    }
}
