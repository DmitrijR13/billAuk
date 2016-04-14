using System;
using System.Data;
using System.Threading;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    class InsertInfoLsByPu : DbAdminClient
    {
        public Returns DisassInfoLsByPu(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            string sql;

            MonitorLog.WriteLog("Старт разбора 34й секции: Информация о лицевых счетах, принадлежащих прибору учета", MonitorLog.typelog.Info, true);
            try
            {
                sql =
                    " SELECT o.nzp_counter, k.nzp_kvar " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu o, " +
                    Points.Pref + DBManager.sUploadAliasRest + "file_kvar k, " +
                    Points.Pref + DBManager.sUploadAliasRest + "file_info_pu ip " +
                    " WHERE o.local_id = ip.id_pu" +
                    " AND k.id = ip.num_ls_pu " +
                    " AND o.type_pu IN (2,3) " +
                    " AND o.nzp_counter is not null " +
                    " AND o.nzp_file = " + finder.nzp_file +
                    " AND k.nzp_file = " + finder.nzp_file +
                    " AND ip.nzp_file = " + finder.nzp_file;

                DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow rr in dt.Rows)
                {
                    sql =
                        " INSERT INTO " + finder.bank + DBManager.sDataAliasRest + "counters_link " +
                        " (nzp_counter, nzp_kvar) " +
                        " VALUES ( " + rr["nzp_counter"].ToString() + ", " + rr["nzp_kvar"].ToString() + ") ";

                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции DisassInfoLsByPu:" + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка разбора 34й секции 'Информация о ЛС, принадлежащих ПУ'", -1);
            }

            MonitorLog.WriteLog("Успешно завершен разбор 34й секции: Информация о ЛС, принадлежащих ПУ (ф-ция DisassInfoLsByPu)", MonitorLog.typelog.Info, true);
            
            return ret;
        }
    }
}
