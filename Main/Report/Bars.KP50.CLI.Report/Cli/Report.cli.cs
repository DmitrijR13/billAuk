
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Client
{
    public class cli_Report
    {
        /// <summary>
        /// процедура записи данных о выгружаемом отчете в БД
        /// </summary>
        /// <param name="report">объект отчета</param>
        /// <param name="ret"></param>
        public static void RunReport(ReportParams report, out Returns ret)
        {
            ReportUtils ru = new ReportUtils();
            ru.RunReport(report, out ret);
        }

        /// <summary>
        /// получение объекта отчета
        /// </summary>
        /// <param name="id">идентификатор отчета</param>
        /// <param name="ret"></param>
        public static ReportParams GetReportById(int id, out Returns ret)
        {
            ReportUtils ru = new ReportUtils();
            return ru.GetReportById(id, out ret);
        }
    }
}
