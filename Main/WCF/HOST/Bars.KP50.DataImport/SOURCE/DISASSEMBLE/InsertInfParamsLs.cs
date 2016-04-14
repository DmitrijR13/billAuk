using System;
using System.Data;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    class InsertInfParamsLs : DbAdminClient
    {
       public Returns DisassInfParamsLs(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            string sql;
            string dat_po;
            DateTime dat_s;

           MonitorLog.WriteLog("Старт разбора 7й секции: Информация о параметрах ЛС в месяце перерасчета", MonitorLog.typelog.Info, true);
           AddKvarByFile obj = new AddKvarByFile();
           
           sql =
               //" SELECT fk.nzp_kvar, cast(reval_month as date) as dat_s, cast( reval_month as date ) + interval '1 month' - interval '1 day' as dat_po" +
               " SELECT  cast(reval_month as date) as dat_s, cast( reval_month as date ) + interval '1 month' - interval '1 day' as dat_po" +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
               Points.Pref + DBManager.sUploadAliasRest + "file_kvarp fkp " +
               " WHERE fk.id = fkp.id " +
               " AND fk.nzp_file = " + finder.nzp_file +
               " AND fkp.nzp_file = " + finder.nzp_file + " Group by 1,2 ";
            DataTable dt = ClassDBUtils.OpenSQL( sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();

           foreach (DataRow rr in dt.Rows)
           {
               dat_s = rr["dat_s"].ToDateTime();
               dat_po = rr["dat_po"].ToString().Substring(0, 10);

               try
               {
                   //obj.WriteKvarParam(conDb, finder.nzp_file, dat_s, finder, dat_po, "file_kvarp", rr["nzp_kvar"].ToString());
                   obj.WriteKvarParam(conDb, finder.nzp_file, dat_s, finder, dat_po, "file_kvarp");
               }
               catch (Exception ex)
               {
                   MonitorLog.WriteLog("Ошибка добавления информации о параметрах ЛС в месяце перерасчета" + ex.Message, MonitorLog.typelog.Error, true);
                   return new Returns(false, "Ошибка добавления информации о параметрах ЛС", -1);                  
               }               

           }
           
           MonitorLog.WriteLog("Успешно завершен разбор информации о параметрах ЛС в месяце перерасчета (ф-ция DisassInfParamsLs)", MonitorLog.typelog.Info, true);

           return ret;
        }
    }
}
