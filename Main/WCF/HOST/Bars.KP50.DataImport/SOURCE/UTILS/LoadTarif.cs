using System;
using System.Data;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport
{
    public class LoadTarif : DbAdminClient
    {
        private readonly IDbConnection conn_db;

        public LoadTarif(IDbConnection con_db)
        {
            conn_db = con_db;
        }


        public Returns SetTarif(string load_data, string bank)
        {
            Returns ret = new Returns();
            string sql;

            try
            {
                //выбираем все данные из загруженного файла в DataTable
                sql = 
                    " SELECT nzp_kvar," +
                    " num_ls," +
                    " nzp_supp," +
                    " nzp_serv," +
                    " nzp_frm," +
                    " tarif, " +
                    " dat_po," +
                    " dat_s," +
                    " is_actual, " +
                    " nzp_user," +
                    " dat_when, " +
                    " cur_unl " +
                    " FROM " + load_data;
                DataTable dtLoad = DBManager.ExecSQLToTable(conn_db, sql);
                foreach (DataRow r in dtLoad.Rows)
                {
                    //Проверяем, есть ли nzp_supp в локальном банке
                    CheckNzpSupp(r["nzp_supp"].ToString(), bank, conn_db);

                    //изменяем строчки в таблице тариф
                    ret = UpdateTableTarif(bank, r);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка добавления тарифа";
                        ret.tag = -1;
                        MonitorLog.WriteLog("Ошибка добавления тарифа: UpdateTableTarif", MonitorLog.typelog.Error, true);
                        return ret;
                    }
                }
                //добавляем строки из файла загрузки наследуемой информации
                //sql =
                //    " update " + bank + "_data" + tableDelimiter + "tarif  set " +
                //    " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, " +
                //    " dat_when, cur_unl)  from " + load_data + " a where exists (select 1 from " + bank + "_data" + tableDelimiter +
                //    " tarif b where a.nzp_kvar =b.nzp_kvar and a.nzp_serv =b.nzp_serv and a.nzp_supp =b.nzp_supp and a.dat_s=b.dat_s ) ";

                //ret = DBManager.ExecSQL(conn_db, sql, true);



                sql =
                    " insert into " + bank + "_data" + tableDelimiter + "tarif " +
                    " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, " +
                    " dat_when, cur_unl) " +
                    " select nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, " +
                    " dat_when, cur_unl " +
                    " from " + load_data + " a where not exists (select 1 from " + bank + "_data" + tableDelimiter +
                    " tarif b where a.nzp_kvar =b.nzp_kvar and a.nzp_serv =b.nzp_serv and a.nzp_supp =b.nzp_supp and a.dat_s=b.dat_s and b.is_actual =1) ";

                ret = DBManager.ExecSQL(conn_db, sql, true);
            }
            catch(Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка добавления тарифа" + ex.Message;
                ret.tag = -1;
                MonitorLog.WriteLog("Ошибка добавления тарифа: " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            return ret;
        }

        /// <summary>
        /// Функция проверяет есть ли nzp_supp в локальном банке, если нет - добавляет
        /// </summary>
        /// <param name="nzp_supp"></param>
        private void CheckNzpSupp(string nzp_supp, string bank, IDbConnection conDb)
        {
            string sql;
            string name_supp = "";

            //Вытаскиваем имя поставщика
            sql =
                " SELECT name_supp " +
                " FROM " + Points.Pref + "_kernel.supplier " +
                " WHERE nzp_supp = " + nzp_supp;
            DataTable dt1 = DBManager.ExecSQLToTable(conn_db, sql);
            foreach (DataRow r in dt1.Rows)
            {
                name_supp = r["name_supp"].ToString();
            }

            //Добавляем запись в локальный банк
            sql = 
                " SELECT * " +
                " FROM " + bank + "_kernel.supplier " +
                " WHERE nzp_supp = " + nzp_supp;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count == 0)
            {
                sql =
                    " INSERT INTO " + bank + "_kernel.supplier " +
                    " (nzp_supp, name_supp) " +
                    " VALUES (" +
                    nzp_supp + ", '" + name_supp.Trim() + "')";
                DBManager.ExecSQL(conDb, sql, true);
            }
        }

        private Returns UpdateTableTarif(string bank, DataRow r)
        {
            Returns ret = new Returns();
            string sql;
            DateTime new_dat_s;
            DateTime new_dat_po;
            if (!DateTime.TryParse(r["dat_s"].ToString(), out new_dat_s))
            {
                ret.result = false;
                ret.text = "Ошибка определения даты начала действия тарифа в данных из файла";
                return ret;
            }
            if (!DateTime.TryParse(r["dat_po"].ToString(), out new_dat_po))
            {
                ret.result = false;
                ret.text = "Ошибка определения даты окончания действия тарифа в данных из файла";
                return ret;
            }

            //выбираем данные из таблицы тариф
            sql =
                " SELECT nzp_tarif, nzp_kvar, num_ls, nzp_serv, nzp_supp, tarif, dat_s, dat_po, " +
                " nzp_frm, tarif, nzp_user, dat_when, is_unl, dat_del, cur_unl, nzp_wp," +
                " user_del, dat_block, user_block, month_calc " +
                " FROM " + bank + sDataAliasRest + "tarif " +
                " WHERE is_actual =1 " +
                " AND nzp_kvar = " + r["nzp_kvar"] +
                " AND nzp_serv = " + r["nzp_serv"] +
                " AND nzp_supp = " + r["nzp_supp"];
            DataTable from_tarif = DBManager.ExecSQLToTable(conn_db, sql);
            ret = UpdateTarif(new_dat_s, new_dat_po, from_tarif, bank);
            return ret;
        }



        private Returns UpdateTarif(DateTime new_dat_s, DateTime new_dat_po, DataTable from_tarif, string bank)
        {
            Returns ret = new Returns(true);
            try
            {
                DateTime old_dat_s;
                DateTime old_dat_po;

                foreach (DataRow drr in from_tarif.Rows)
                {
                    if (!DateTime.TryParse(drr["dat_s"].ToString(), out old_dat_s))
                    {
                        ret.result = false;
                        ret.text = "Ошибка определения даты начала действия тарифа в данных из таблицы тарифа";
                        return ret;
                    }
                    if (!DateTime.TryParse(drr["dat_po"].ToString(), out old_dat_po))
                    {
                        ret.result = false;
                        ret.text = "Ошибка определения даты окончания действия тарифа в данных из таблицы тарифа";
                        return ret;
                    }
                    //.......................................................
                    //  исх.    <-------->
                    //  нов.  <------------>
                    // старый интервал удалить 
                    //.......................................................
                    if (new_dat_s <= old_dat_s && new_dat_po >= old_dat_po)
                    {
                        OldTarifDelete(bank, drr);
                    }

                    //.......................................................
                    //  исх.  <--------->
                    //  нов.     <--------->
                    // исправляем правый край
                    //.......................................................
                    if (new_dat_s > old_dat_s && new_dat_po >= old_dat_po && new_dat_s <= old_dat_po)
                    {
                        CorrectRightSide(new_dat_s, bank, drr);
                    }

                    //.......................................................
                    //  исх.    <---------->
                    //  нов.  <------->
                    // исправляем левый край
                    //.......................................................
                    if (new_dat_s <= old_dat_s && new_dat_po < old_dat_po && old_dat_s <= new_dat_po)
                    {
                        CorrectLeftSide(new_dat_po, bank, drr);
                    }

                    //.......................................................
                    //  исх.  <------------>
                    //  нов.   <--------->
                    // надо породить два исправленных интервала
                    //.......................................................
                    if (new_dat_s > old_dat_s && new_dat_po < old_dat_po)
                    {
                        NewTarifInOldTarif(new_dat_s, new_dat_po, bank, drr);
                    }
                }
            }
            catch
            {
                ret.result = false;
                return ret;
            }
            return ret;
        }

        private void NewTarifInOldTarif(DateTime new_dat_s, DateTime new_dat_po, string bank, DataRow drr)
        {
            string sql;
            DateTime dt = new_dat_s.AddDays(-1);
            sql =
                " UPDATE " + bank + sDataAliasRest + "tarif " +
                " SET dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                " WHERE nzp_tarif = " + drr["nzp_tarif"];
            ExecSQL(conn_db, sql, true);

            DateTime dt2 = new_dat_po.AddDays(1);
            sql =
                " INSERT INTO " + bank + sDataAliasRest + "tarif" +
                " (nzp_kvar, num_ls," +
                " nzp_serv, nzp_supp," +
                " nzp_frm, tarif," +
                " dat_s," +
                " dat_po, is_actual, nzp_user," +
                " dat_when, is_unl, " +
                " cur_unl, nzp_wp) " +
                "values (" + drr["nzp_kvar"] + "," + drr["num_ls"] + ", " +
                drr["nzp_serv"] + ", " + drr["nzp_supp"] + "," +
                drr["nzp_frm"] + ", '" + drr["tarif"] + "'," +
                "'" + dt2.Day.ToString("00") + "." + dt2.Month.ToString("00") + "." + dt2.Year.ToString("0000") + "'," +
                " '" + drr["dat_po"].ToString().Substring(0, 10) + "', 1, " + drr["nzp_user"] + ", '" +
                drr["dat_when"].ToString().Substring(0, 10) + "', " + drr["is_unl"] + ", " + 
                drr["cur_unl"] + "," + drr["nzp_wp"] + ");";
            ExecSQL(conn_db, sql, true);
        }

        private void CorrectLeftSide(DateTime new_dat_po, string bank, DataRow drr)
        {
            string sql;
            DateTime dt = new_dat_po.AddDays(1);
            sql =
                " UPDATE " + bank + sDataAliasRest + "tarif " +
                " SET dat_s = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                " WHERE nzp_tarif = " + drr["nzp_tarif"];
            ExecSQL(conn_db, sql, true);
        }

        private void CorrectRightSide(DateTime new_dat_s, string bank, DataRow drr)
        {
            string sql;
            DateTime dt = new_dat_s.AddDays(-1);
            sql =
                " UPDATE " + bank + sDataAliasRest + "tarif " +
                " SET dat_po = '" + dt.Day.ToString("00") + "." + dt.Month.ToString("00") + "." + dt.Year.ToString("0000") + "'" +
                " WHERE nzp_tarif = " + drr["nzp_tarif"];
            ExecSQL(conn_db, sql, true);
        }

        private void OldTarifDelete(string bank, DataRow drr)
        {
            string sql;
            sql =
                " UPDATE " + bank + sDataAliasRest + "tarif " +
                " SET is_actual = 100 " +
                " WHERE nzp_tarif = " + drr["nzp_tarif"];
            ExecSQL(conn_db, sql, true);
        }
    }
}
