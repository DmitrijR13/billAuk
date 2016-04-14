using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DataImport.SOURCE.UTILS
{
    public class LoadPrm : DataBaseHeadServer
    {
        private IDbConnection conn_db;
        public LoadPrm(IDbConnection connection)
        {
            conn_db = connection;
        }

        public Returns SetPrm(int prm_table_num, string load_data, string bank)
        {
            Returns ret = new Returns(true);
            string sql;
            try
            {
                string table = "prm_" + prm_table_num;
                sql =
                    " SELECT nzp," +
                    " nzp_prm," +
                    " dat_s," +
                    " dat_po," +
                    " val_prm," +
                    " is_actual," +
                    " cur_unl," +
                    " user_del," +
                    " nzp_user" +
                    " FROM " + load_data;
                DataTable dtLoad = DBManager.ExecSQLToTable(conn_db, sql);
                foreach (DataRow r in dtLoad.Rows)
                {
                    //изменяем строчки в таблице тариф
                    ret = UpdateTablePrm(bank, r, table);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления параметра: SetPrm", MonitorLog.typelog.Error, true);
                        return new Returns(false, "Ошибка добавления тарифа", -1);
                    }
                }
                //добавляем строки из файла загрузки наследуемой информации
                sql =
                    " INSERT INTO " + bank + sDataAliasRest + table +
                    " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, user_del, nzp_user, dat_when) " +
                    " SELECT  nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, user_del, nzp_user, " + sCurDate +
                    " FROM " + load_data;

                ret = ExecSQL(conn_db, sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления тарифа: " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления тарифа" + ex.Message, -1);
            }

            return ret;
        }


        private Returns UpdateTablePrm(string bank, DataRow r, string prm_table)
        {
            Returns ret = new Returns();
            string sql;
            DateTime new_dat_s;
            DateTime new_dat_po;
            if (!DateTime.TryParse(r["dat_s"].ToString(), out new_dat_s)) 
                return new Returns(false, "Ошибка определения даты начала действия параметра в данных из файла");
           
            if (!DateTime.TryParse(r["dat_po"].ToString(), out new_dat_po)) 
                return new Returns(false, "Ошибка определения даты окончания действия параметра в данных из файла");

            //выбираем данные из таблицы prm
            sql =
                " SELECT nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, user_del " +
                " FROM " + bank + sDataAliasRest + prm_table +
                " WHERE is_actual <> 100" +
                " AND nzp = " + r["nzp"] +
                " AND nzp_prm = " + r["nzp_prm"];
            DataTable from_prm = DBManager.ExecSQLToTable(conn_db, sql);
            ret = UpdatePrm(new_dat_s, new_dat_po, from_prm, bank, prm_table);
            return ret;
        }

        private Returns UpdatePrm(DateTime new_dat_s, DateTime new_dat_po, DataTable from_prm, string bank, string prm_table)
        {
            Returns ret = new Returns(true);
            try
            {
                DateTime old_dat_s;
                DateTime old_dat_po;

                foreach (DataRow drr in from_prm.Rows)
                {
                    if (!DateTime.TryParse(drr["dat_s"].ToString(), out old_dat_s))
                        return new Returns(false, "Ошибка определения даты начала действия параметра в данных из таблицы параметров");
                    
                    if (!DateTime.TryParse(drr["dat_po"].ToString(), out old_dat_po))
                        return new Returns(false, "Ошибка определения даты окончания действия параметра в данных из таблицы параметров");

                    //.......................................................
                    //  исх.    <-------->
                    //  нов.  <------------>
                    // старый интервал удалить 
                    //.......................................................
                    if (new_dat_s <= old_dat_s && new_dat_po >= old_dat_po)
                    {
                        OldPrmDelete(bank, drr, prm_table);
                    }

                    //.......................................................
                    //  исх.  <--------->
                    //  нов.     <--------->
                    // исправляем правый край
                    //.......................................................
                    if (new_dat_s > old_dat_s && new_dat_po >= old_dat_po && new_dat_s <= old_dat_po)
                    {
                        CorrectRightSide(new_dat_s, bank, drr, prm_table);
                    }

                    //.......................................................
                    //  исх.    <---------->
                    //  нов.  <------->
                    // исправляем левый край
                    //.......................................................
                    if (new_dat_s <= old_dat_s && new_dat_po < old_dat_po && old_dat_s <= new_dat_po)
                    {
                        CorrectLeftSide(new_dat_po, bank, drr, prm_table);
                    }

                    //.......................................................
                    //  исх.  <------------>
                    //  нов.   <--------->
                    // надо породить два исправленных интервала
                    //.......................................................
                    if (new_dat_s > old_dat_s && new_dat_po < old_dat_po)
                    {
                        NewPrmInOldPrm(new_dat_s, new_dat_po, bank, drr, prm_table);
                    }
                }
            }
            catch
            {
                return new Returns(false);
            }
            return ret;
        }

        private void OldPrmDelete(string bank, DataRow drr, string prm_table)
        {
            string sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                         " SET is_actual = 100, dat_when = " + DBManager.sCurDate +
                         " WHERE nzp_key = " + drr["nzp_key"];
            ExecSQL(conn_db, sql );
        }

        private void CorrectRightSide(DateTime new_dat_s, string bank, DataRow drr, string prm_table)
        {
            var ret = STCLINE.KP50.Global.Utils.InitReturns();

            
            DateTime dt = new_dat_s.AddDays(-1);
            
            //проверка на существование такого параметра с этим периодом действия
            string sql = " SELECT count(*) FROM " + bank + sDataAliasRest + prm_table + " p, " +
                         " " + bank + sDataAliasRest + prm_table + " r " + 
                         " WHERE r.nzp = p.nzp AND r.nzp_prm = p.nzp_prm AND p.is_actual <> 100 " +
                         " AND p.dat_s = r.dat_s AND p.dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                         " AND p.nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"];

            object obj = ExecScalar(conn_db, sql, out ret, true);

            if (Convert.ToInt32(obj) > 0)
            {
                //если есть, то is_actual = 100
                sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                      " SET is_actual = 100 WHERE EXISTS (SELECT 1 FROM " + bank + sDataAliasRest + prm_table + " r " + 
                      " WHERE r.nzp = " + bank + sDataAliasRest + prm_table + ".nzp " +
                      " AND r.nzp_prm = " + bank + sDataAliasRest + prm_table + ".nzp_prm " +
                      " AND " + bank + sDataAliasRest + prm_table + ".is_actual <> 100 " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_s = r.dat_s " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                      " AND " + bank + sDataAliasRest + prm_table + ".nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"] + " )";
                ExecSQL(conn_db, sql);
            }

            sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                      " SET dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." +
                      dt.Year.ToString("0000") + "'" +
                      ", dat_when = " + DBManager.sCurDate +
                      " WHERE nzp_key = " + drr["nzp_key"];
            ExecSQL(conn_db, sql);
        }

        private void CorrectLeftSide(DateTime new_dat_po, string bank, DataRow drr, string prm_table)
        {
            var ret = STCLINE.KP50.Global.Utils.InitReturns();

            DateTime dt = new_dat_po.AddDays(1);

            //проверка на существование такого параметра с этим периодом действия
            string sql = " SELECT count(*) FROM " + bank + sDataAliasRest + prm_table + " p, " +
                         " " + bank + sDataAliasRest + prm_table + " r " +
                         " WHERE r.nzp = p.nzp AND r.nzp_prm = p.nzp_prm AND p.is_actual <> 100 " +
                         " AND p.dat_po = r.dat_po AND p.dat_s = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                         " AND p.nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"];

            object obj = ExecScalar(conn_db, sql, out ret, true);

            if (Convert.ToInt32(obj) > 0)
            {
                //если есть, то is_actual = 100
                sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                      " SET is_actual = 100 WHERE EXISTS (SELECT 1 FROM " + bank + sDataAliasRest + prm_table + " r " +
                      " WHERE r.nzp = " + bank + sDataAliasRest + prm_table + ".nzp " +
                      " AND r.nzp_prm = " + bank + sDataAliasRest + prm_table + ".nzp_prm " +
                      " AND " + bank + sDataAliasRest + prm_table + ".is_actual <> 100 " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_po = r.dat_po " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_s = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                      " AND " + bank + sDataAliasRest + prm_table + ".nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"] + " )";
                ExecSQL(conn_db, sql);
            }

            sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                         " SET dat_s = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                         ", dat_when = " + DBManager.sCurDate +
                         " WHERE nzp_key = " + drr["nzp_key"];
            ExecSQL(conn_db, sql);
        }

        private void NewPrmInOldPrm(DateTime new_dat_s, DateTime new_dat_po, string bank, DataRow drr, string prm_table)
        {
            var ret = STCLINE.KP50.Global.Utils.InitReturns();

            DateTime dt = new_dat_s.AddDays(-1);

            string sql = " SELECT count(*) FROM " + bank + sDataAliasRest + prm_table + " p, " +
                         " " + bank + sDataAliasRest + prm_table + " r " +
                         " WHERE r.nzp = p.nzp AND r.nzp_prm = p.nzp_prm AND p.is_actual <> 100 " +
                         " AND p.dat_s = r.dat_s AND p.dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                         " AND p.nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"];

            object obj = ExecScalar(conn_db, sql, out ret, true);

            if (Convert.ToInt32(obj) > 0)
            {
                //если есть, то is_actual = 100
                sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                      " SET is_actual = 100 WHERE EXISTS (SELECT 1 FROM " + bank + sDataAliasRest + prm_table + " r " +
                      " WHERE r.nzp = " + bank + sDataAliasRest + prm_table + ".nzp " +
                      " AND r.nzp_prm = " + bank + sDataAliasRest + prm_table + ".nzp_prm " +
                      " AND " + bank + sDataAliasRest + prm_table + ".is_actual <> 100 " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_s = r.dat_s " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                      " AND " + bank + sDataAliasRest + prm_table + ".nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"] + " )";
                ExecSQL(conn_db, sql);
            }

            sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                         " SET dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                         ", dat_when = " + DBManager.sCurDate +
                         " WHERE nzp_key = " + drr["nzp_key"];
            ExecSQL(conn_db, sql );




            DateTime dt2 = new_dat_po.AddDays(1);

            //проверка на существование такого параметра с этим периодом действия
            sql = " SELECT count(*) FROM " + bank + sDataAliasRest + prm_table + " p, " +
                         " " + bank + sDataAliasRest + prm_table + " r " +
                         " WHERE r.nzp = p.nzp AND r.nzp_prm = p.nzp_prm AND p.is_actual <> 100 " +
                         " AND p.dat_po = r.dat_po AND p.dat_s = '" + dt2.Day.ToString("00") + "." + dt2.Month.ToString("00") + "." + dt2.Year.ToString("0000") + "'" +
                         " AND p.nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"];

            obj = ExecScalar(conn_db, sql, out ret, true);

            if (Convert.ToInt32(obj) > 0)
            {
                //если есть, то is_actual = 100
                sql = " UPDATE " + bank + sDataAliasRest + prm_table +
                      " SET is_actual = 100 WHERE EXISTS (SELECT 1 FROM " + bank + sDataAliasRest + prm_table + " r " +
                      " WHERE r.nzp = " + bank + sDataAliasRest + prm_table + ".nzp " +
                      " AND r.nzp_prm = " + bank + sDataAliasRest + prm_table + ".nzp_prm " +
                      " AND " + bank + sDataAliasRest + prm_table + ".is_actual <> 100 " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_po = r.dat_po " +
                      " AND " + bank + sDataAliasRest + prm_table + ".dat_s = '" + dt2.Day.ToString("00") + "." + dt2.Month.ToString("00") + "." + dt2.Year.ToString("0000") + "'" +
                      " AND " + bank + sDataAliasRest + prm_table + ".nzp_key <> r.nzp_key AND r.nzp_key = " + drr["nzp_key"] + " )";
                ExecSQL(conn_db, sql);
            }

            sql =
                " INSERT INTO " + bank + sDataAliasRest + prm_table +
                " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual,  user_del, dat_when) " +
                "values (" + drr["nzp"] + "," + drr["nzp_prm"] + ", " +
                "'" + dt2.Day.ToString("00") + "." + dt2.Month.ToString("00") + "." + dt2.Year.ToString("0000") + "'," +
                " '" + drr["dat_po"].ToString().Substring(0, 10) + "','" + drr["val_prm"] + "', 1, " +
                drr["user_del"] + ", " + DBManager.sCurDate + ");";
            ExecSQL(conn_db, sql );
        }
    }
}
