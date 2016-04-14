using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class AddOplatServ : DbAdminClient
    {
        /// <summary>
        /// Загрузка оплат если установлена услуга (грузим напрямую в fn_supplierXX)
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>

        public Returns Run(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            try
            {
                // вставка домовых параметров 
                string sql = "";
                string lastDay = "";
                int kol = 0;
                string for_table_name = DateTime.Today.Hour.ToString() + DateTime.Today.Minute.ToString() + DateTime.Today.Millisecond.ToString();
                string fn_supp_table = "t_fn_supp_" + for_table_name;

                //изменение статуса загрузки
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка оплат' where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                //количество строк в file_oplats с данным nzp_file, где id_serv не null
                sql = "select count(*) as kol from " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats where nzp_file = " + finder.nzp_file ;
                kol = Convert.ToInt32(ExecScalar(conDb, sql, out ret, true));
                if (kol > 0)
                {

                    #region последний день месяца
                    sql = "select calc_date from " + Points.Pref + DBManager.sUploadAliasRest + "file_head where nzp_file = " + finder.nzp_file;

                    lastDay = Convert.ToString(ExecScalar(conDb, sql, out ret, true)).Trim();
                    #endregion

                    sql = " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_pack" +
                          " WHERE nzp_file = " + finder.nzp_file;
                    DataTable dt = ClassDBUtils.OpenSQL(sql, conDb).resultData;
                    foreach (DataRow r in dt.Rows)
                    {
                        decimal nzp_pack = 0;

                        
                        sql =
                            " insert into " + Points.Pref + "_fin_" + lastDay.Substring(8, 2) + tableDelimiter + "pack " +
                            " ( pack_type, nzp_bank,  num_pack, dat_uchet,   dat_pack,  yearr, count_kv," +
                            " sum_pack, flag, dat_vvod,  sum_rasp, sum_nrasp, nzp_rs, dat_inp, file_name)" +
                            " SELECT  20,  1998, '" + r["num_plat"] + "',   '" + r["dat_plat"] + "', '" + r["dat_plat"] + "'," + 
                            lastDay.Substring(6,4) + ", count(*),sum(sum_oplat),  11, " + sCurDate + ", " +
                            "'0', sum(sum_oplat), 1, '" + r["dat_plat"] + "',  'Суммарная пачка по файлу " + finder.nzp_file + "' " +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats a," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar b" +
                            " where a.nzp_file =b.nzp_file and a.nzp_file= " + finder.nzp_file +
                            " and a.ls_id=b.id AND a.nzp_pack = " + r["num_plat"];
                        ret = ExecSQL(conDb, sql, true);

                        nzp_pack = GetSerialValue(conDb);

                        //кладем отдельные оплаты

                        sql =
                            " INSERT INTO " + Points.Pref + "_fin_" + lastDay.Substring(8, 2) + tableDelimiter + "pack_ls " +
                            "( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod, dat_uchet, " +
                            " anketa, info_num, inbasket, alg, unl,  nzp_user, incase, nzp_rs)" +
                            " SELECT " + nzp_pack + ", b.nzp_kvar, a.sum_oplat, '" + lastDay + "', 40, 1,  a.dat_opl," +
                            //sNvlWord + "(dat_uchet, '" + "15" + lastDay.Substring(2, 8) + "')" 
                            " null, nzp_kvar, 1," +
                            "0, '0', 0,  " + finder.nzp_user + ", 0, 1" +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats a," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar b" +
                            " WHERE a.nzp_file =b.nzp_file and a.nzp_file = " + finder.nzp_file +
                            " and a.ls_id=b.id  AND a.nzp_pack = " + r["num_plat"];
                        ret = ExecSQL(conDb, sql, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AddOplatServ: ошибка  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "AddOplatServ: ошибка";
                ret.result = false;
                return ret;
            }
            return ret;
        }
    }
}
