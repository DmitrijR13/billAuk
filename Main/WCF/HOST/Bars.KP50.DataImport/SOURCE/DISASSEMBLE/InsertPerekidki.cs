using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DataImport;


namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    class InsertPerekidki : DbAdminClient
    {
       public Returns DisassPerekidka(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns();
            string sql;
            //int commandTime = 3600;
            string calc_date = "";

            MonitorLog.WriteLog("Старт разбора перекидок (ф-ция DisassPerekidka)", MonitorLog.typelog.Info, true);
           
            sql =
                " SELECT MAX(calc_date) as calc_date " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_head " +
                " WHERE nzp_file = " + finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            
            sql =
                " SELECT MIN(calc_date)  as calc_date  " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_head " +
                " WHERE nzp_file = " + finder.nzp_file;
            DataTable dt1 = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows[0]["calc_date"].ToString() != dt1.Rows[0]["calc_date"].ToString())
            {
                MonitorLog.WriteLog("При загрузке сальдо месяц должен быть одинаковый", MonitorLog.typelog.Error, true);
                return ret;
            }
            else
            {
                string datet = dt1.Rows[0]["calc_date"].ToString().Substring(0, 10);
                DateTime dtime =
                    new DateTime(Convert.ToInt32(datet.Substring(6, 4)),
                        Convert.ToInt32(datet.Substring(3, 2)), 1).AddMonths(-1);
                calc_date = "'" + dtime.ToString("dd.MM.yyyy") + "'";
            }

            //#region создаем временную таблицу
            //try
            //{
            //    sql = "drop table t_for_perekidki_table";
            //    ExecSQL(conDb, null, sql, false, commandTime);
            //}
            //catch { }
            
            //sql = 
            //      " CREATE TEMP TABLE t_for_perekidki_table (" +
            //      " nzp_kvar INTEGER," +
            //      " nzp_supp INTEGER," +
            //      " nzp_serv INTEGER," +
            //      " id_ls CHAR(20)," +
            //      " dog_id INTEGER,  )"
            //       + sUnlogTempTable;
            //ExecSQL(conDb, null, sql, false, commandTime);
            //#endregion

            //sql =
            //    "INSERT INTO t_for_perekidki_table " +
            //    "( nzp_kvar, nzp_supp, nzp_serv ) " +
            //    " SELECT DISTINCT k.nzp_kvar, s.nzp_supp, srv.nzp_serv " +
            //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki p, " +
            //    Points.Pref + DBManager.sUploadAliasRest + "file_kvar k, " +
            //    Points.Pref + DBManager.sUploadAliasRest + "file_dog s, " +
            //    Points.Pref + DBManager.sUploadAliasRest + "file_services srv" +
            //    " WHERE  k.nzp_kvar = p.id_ls " +
            //    " AND s.nzp_supp = p.dog_id " +
            //    " AND srv.nzp_serv = p.id_serv " +
            //    " AND k.nzp_file = " + finder.nzp_file +
            //    " AND s.nzp_file = " + finder.nzp_file +
            //    " AND srv.nzp_file = " + finder.nzp_file +
            //    " AND p.nzp_file = " + finder.nzp_file;
            //ExecSQL(conDb, sql);

           sql =
               " SELECT fk.id, fd.dog_id, fsv.id_serv, fk.nzp_kvar, fd.nzp_supp, fsv.nzp_serv " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
               Points.Pref + DBManager.sUploadAliasRest + "file_dog fd, " +
               Points.Pref + DBManager.sUploadAliasRest + "file_services fsv, " +
               Points.Pref + DBManager.sUploadAliasRest + "file_perekidki fp" +
               " WHERE fk.id = fp.id_ls " +
               " AND fd.dog_id = fp.dog_id " +
               " AND fsv.id_serv = fp.id_serv " +
               " AND fk.nzp_file = " + finder.nzp_file +
               " AND fd.nzp_file = " + finder.nzp_file +
               " AND fsv.nzp_file = " + finder.nzp_file +
               " ANd fp.nzp_file = " + finder.nzp_file;
                
            DataTable dtper = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtper.Rows.Count == 0)
            {
      //          MonitorLog.WriteLog("Имеются оплаты, которых нет в базе, данные не загружены", MonitorLog.typelog.Error, true);
      //          return new Returns(false, "Имеются оплаты, которых нет в базе", -1);
            }

           //проставляем nzp_kvar, nzp_serv, nzp_supp в file_perekidki
           foreach (DataRow rr in dtper.Rows)
           {
               sql =
                   " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki fp" +
                   " SET nzp_kvar = " + rr["nzp_kvar"].ToString() +
                   " WHERE fp.id_ls = trim('" + rr["id"].ToString() + "') " +
                   " AND fp.nzp_file = " + finder.nzp_file;
               DBManager.ExecSQL(conDb, sql, true);

               sql =
                   " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki fp" +
                   " SET nzp_serv = " + rr["nzp_serv"].ToString() +
                   " WHERE fp.id_serv = " + rr["id_serv"].ToString() +
                   " AND fp.nzp_file = " + finder.nzp_file;
               DBManager.ExecSQL(conDb, sql, true);

               sql =
                   " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki fp" +
                   " SET nzp_supp = " + rr["nzp_supp"].ToString() +
                   " WHERE fp.dog_id = " + rr["dog_id"].ToString() +
                   " AND fp.nzp_file = " + finder.nzp_file;
               DBManager.ExecSQL(conDb, sql, true);

           }
           
           MonitorLog.WriteLog("Вставка в " + finder.bank + "_charge_" + calc_date.Substring(9, 2) + tableDelimiter + "perekidka ", MonitorLog.typelog.Info, true);
           
           sql =
                   " INSERT INTO " + finder.bank + "_charge_" + calc_date.Substring(9, 2) + tableDelimiter +
                   "perekidka " +
                   " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif," +
                   " volum, sum_rcl, month_, comment, nzp_user, nzp_reestr) " +
                   " SELECT  p.nzp_kvar, p.nzp_kvar , p.nzp_serv,  p.nzp_supp , " +
                   " 1, " + calc_date + sConvToDate + " , '0', '0',  p.sum_perekidki," +
                   calc_date.Substring(4, 2) + ", " + sNvlWord + "(comment,'" + finder.nzp_file + "') ," +
                   finder.nzp_user + ", p.nzp_file " +
                   " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki  p " +
                   " WHERE p.sum_perekidki <> 0 and nzp_file = " + finder.nzp_file;

               ret = ExecSQL(conDb, sql, true);
          
           if (!ret.result)
           {
               MonitorLog.WriteLog("Ошибка разбора 33й секции 'Перекидки' ", MonitorLog.typelog.Error, true);
               return new Returns(false, "Ошибка разбора 33й секции 'Перекидки'", -1);
           }
           else
           {
               MonitorLog.WriteLog("Успешно завершен разбора перекидок (ф-ция DisassPerekidka)", MonitorLog.typelog.Info, true);
           }
           return ret;
        }
    }
}
