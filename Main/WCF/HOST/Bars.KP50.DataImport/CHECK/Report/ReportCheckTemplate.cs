using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    
    public abstract class ReportCheckTemplate : ReportTemplate
    {
        /// <summary>
        /// Выставление ссылки на отчет с ошибками
        /// </summary>
        /// <param name="CheckGroupId"></param>
        /// <param name="nzp_exc"></param>
        protected void SetNzpExcInCheckChMon(int CheckGroupId, int nzp_exc)
        {
            string sql =
                " UPDATE " + Points.Pref + DBManager.sDataAliasRest + "checkChMon " +
                " SET nzp_exc = " + nzp_exc +
                " WHERE  month_='" + Month + "'  AND yearr='" + Year + "'" +
                " AND nzp_grp='" + CheckGroupId + "' and pref='" + Bank.pref + "'" +
                " AND status_ = '2' ";

            DBManager.ExecSQL(conn_db, sql.ToString(), true);
        }
    }
}
