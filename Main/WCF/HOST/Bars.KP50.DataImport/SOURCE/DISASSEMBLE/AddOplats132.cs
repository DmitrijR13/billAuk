using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    class AddOplats132 : DbAdminClient
    {
        /// <summary>
        /// Загрузка оплат если установлена услуга (грузим напрямую в fn_supplierXX)
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>

        public Returns Runold(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            try
            {
                // вставка домовых параметров 
                string sql = "";

                //изменение статуса загрузки
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка оплат' where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                MonitorLog.WriteLog("AddOplatServ: начало  " , MonitorLog.typelog.Info, true);
                // надо убрать count -не рационально
                sql =
                    " SELECT count(*) as num6 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec6exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                sql =
                    " SELECT count(*) as num22 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec22exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                sql =
                    " SELECT count(*) as num32 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec32exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                string calcMonth = "01." + finder.month.ToString("00") + "." + finder.year;

                #region имеется только 6 секция
                if (sec6exists && !sec22exists)
                {
                    MonitorLog.WriteLog("имеется только 6 секция ", MonitorLog.typelog.Info, true);
                    decimal nzp_pack;

                    string table = "_temp_table_info_pack";
                    try
                    {
                        sql = "DROP TABLE " + table;
                        ret = ExecSQL(conDb, sql, false);
                    }
                    catch { }

                    sql = " CREATE TEMP TABLE " + table + "( " +
                          " id serial," +
                          " nzp_kvar " + DBManager.sDecimalType + "(14,0)," +
                          " nzp_supp " + DBManager.sDecimalType + "(14,0)," +
                          " sum_money " + DBManager.sDecimalType + "(14,2)," +
                          " nzp_serv " + DBManager.sDecimalType + "(14,0)," +
                          " nzp_pack_ls " + DBManager.sDecimalType + "(14,0) ) " +
                          DBManager.sUnlogTempTable;
                    ret = ExecSQL(conDb, sql, true);
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    sql =
                            " INSERT INTO " + table +
                            " ( nzp_kvar, nzp_supp, sum_money, nzp_serv )" +
                            " SELECT DISTINCT fk.nzp_kvar, s.nzp_supp, fs.sum_money, fss.nzp_serv " +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services fss, " +
                            Points.Pref + DBManager.sUploadAliasRest + "file_dog s, " +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                            " WHERE  fs.ls_id = fk.id AND s.dog_id = fs.dog_id AND fss.nzp_serv = fs.nzp_serv " +
                            " AND fs.nzp_file = fss.nzp_file AND fs.nzp_file = s.nzp_file AND" +
                            " fs.nzp_file = fk.nzp_file AND" + " fs.nzp_file = " + finder.nzp_file +
                            " AND fs.sum_money > 0";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                           " insert into " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                           " ( pack_type, nzp_bank,  num_pack, dat_uchet,   dat_pack,   count_kv," +
                           " sum_pack, flag, dat_vvod,  sum_rasp, sum_nrasp, nzp_rs, dat_inp, file_name)" +
                           " SELECT  20,  1998, '1',   '15." + finder.month + "." + finder.year + "' " + sConvToDate + ", '" +
                           calcMonth + "'" + sConvToDate + ", count(*), sum(sum_money),  11, " + sCurDate + ", " +
                           "sum(sum_money), '0', 1, " + sCurDate + ",  'Суммарная пачка по файлу " + finder.nzp_file + "' " +
                           " FROM " + table;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    nzp_pack = GetSerialValue(conDb);


                    //кладем отдельные оплаты

                    sql =
                        " INSERT INTO " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                        "( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod, dat_uchet, " +
                        " anketa, info_num, inbasket, alg, unl,  nzp_user, incase, nzp_rs)" +
                        " SELECT " + nzp_pack + ", nzp_kvar, sum(sum_money), '" + calcMonth + "', 40, 1,  '" + calcMonth + "'," +
                        "'15" + calcMonth.Substring(2, 8) + "', " +
                        "  nzp_kvar, 1," + "0, '1',  id,  " + finder.nzp_user + ", 0, 1" +
                        " FROM " + table +" group by 1,2,13 ";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);


                    sql =
                        " UPDATE " + table + " set nzp_pack_ls =" +
                        " (SELECT nzp_pack_ls FROM " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls" +
                        " WHERE  unl = " + table + ".id AND nzp_pack = " + nzp_pack + " )";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);


                    //кладем в from_suppplier

                    sql =
                        " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter + "from_supplier" +
                        " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge,  num_ls, alias_ls," +
                        " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_plat) " +
                        " SELECT nzp_serv, nzp_supp, nzp_pack_ls, -7, nzp_kvar, nzp_kvar," +
                        " sum_money, 40, '" + calcMonth + "' ,  '" + calcMonth + "', '15" + calcMonth.Substring(2, 8) + "','" + calcMonth + "'" +
                        " FROM " + table;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                       " UPDATE " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls" +
                       " SET unl = 0 " +
                       " WHERE nzp_pack = " + nzp_pack;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    MonitorLog.WriteLog("имеется только 6 секция закончили ", MonitorLog.typelog.Info, true);
                    return ret;
                }
                #endregion
                MonitorLog.WriteLog("имеется не только 6 секция  ", MonitorLog.typelog.Info, true);
                sql = 
                        " SELECT * " +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_pack" +
                        " WHERE nzp_file = " + finder.nzp_file;
                DataTable dtpacks = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow r in dtpacks.Rows)
                {
                    MonitorLog.WriteLog(" Пачка оплат  ", MonitorLog.typelog.Info, true);
                    string id_pack = r["id"].ToString(); 
                    string num_pack = r["num_plat"].ToString();
                    int pack_type = r["kod_type_opl"].ToInt();
                    int is_raspr = r["is_raspr"].ToInt();
                    int alg = is_raspr == 1 ? 1 : 0;
                    string dat_uchet = is_raspr == 1 ? ",'15." + finder.month + "." + finder.year + "' " + sConvToDate : "";
                    string dat_uchet_field = is_raspr == 1 ? "dat_uchet," : "";
                    int kol_plat = r["kol_plat"].ToInt();
                    string sum_pack = r["sum_plat"].ToString();
                    string sum_raspr = is_raspr == 1 ? sum_pack : "0";
                    string sum_nraspr = is_raspr == 1 ? "0" : sum_pack;
                    int flag = is_raspr == 1 ? 21 : 23;
                    
                    sql =
                           " insert into " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                           " ( pack_type, nzp_bank,  num_pack, " + dat_uchet_field + "  dat_pack,  count_kv," +
                           " sum_pack, flag, dat_vvod,  sum_rasp, sum_nrasp, nzp_rs, dat_inp, file_name)" +
                           " VALUES" +
                           " ( " + pack_type + ",  1998, '" + num_pack + "' " + dat_uchet + ", '" +
                           calcMonth + "'" + sConvToDate + ", " + kol_plat + ", " + sum_pack + ",  " + flag + ", " + sCurDate + ", " +
                           sum_raspr + ", '" + sum_nraspr + "', 1, " + sCurDate + ",  'Суммарная пачка по файлу " + finder.nzp_file + "') ";
                    ret = ExecSQL(conDb, sql, true);

                    int nzp_pack = GetSerialValue(conDb);

                    //кладем отдельные оплаты
                    MonitorLog.WriteLog(" оплаты по лс  ", MonitorLog.typelog.Info, true);
                    sql =
                        " INSERT INTO " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                        "( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod," + dat_uchet_field + " unl, " +
                        " anketa, info_num, inbasket, alg,   nzp_user, incase, nzp_rs)" +
                        " SELECT " + nzp_pack + ", b.nzp_kvar, a.sum_oplat, '" + calcMonth + "', " + pack_type + ", 1," +
                        " a.dat_opl " + dat_uchet + ", a.kod_oplat, " +
                        "  b.nzp_kvar, 1," + "0, '" + alg + "',  " + finder.nzp_user + ", 0, 1" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats a," +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar b" +
                        " WHERE a.nzp_file =b.nzp_file and a.nzp_file = " + finder.nzp_file +
                        " and a.ls_id = b.id  AND a.nzp_pack = " + id_pack;
                    ret = ExecSQL(conDb, sql, true);

                    if (sec32exists && pack_type == 33 && is_raspr == 1)
                    {
                        MonitorLog.WriteLog(" распределенная пачка оплат 33   ", MonitorLog.typelog.Info, true);
                        //кладем в fn_suppplier
                        sql =
                            " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter + "fn_supplier" + calcMonth.Substring(3, 2) +
                            " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                            " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_user, s_dolg, s_forw ) " +
                            " SELECT s.nzp_serv, d.nzp_supp, p.nzp_pack_ls, p.nzp_pack, k.nzp_kvar, " +
                            " r.sum_money, " + pack_type + ", '" + calcMonth + "'" + sConvToDate + ", o.dat_opl" +
                            dat_uchet +  ", 0, 0, 0" +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr r," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_oplats o, " +
                            Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                            " WHERE r.id_dog = d.dog_id AND r.id_serv = s.id_serv AND r.kod_oplat = o.kod_oplat" +
                            " AND o.kod_oplat = p.unl AND o.ls_id = k.id AND o.nzp_pack = " + id_pack +
                            " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                            " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(conDb, sql, true);
                    }
                    else if (sec32exists && pack_type != 33 && is_raspr == 1)
                    {
                        //кладем в from_suppplier
                        MonitorLog.WriteLog(" Распределенная пачка оплат 40 или 50  ", MonitorLog.typelog.Info, true);
                        sql =
                            " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter + "from_supplier" +
                            " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                            " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_plat) " +
                            " SELECT s.nzp_serv, d.nzp_supp, p.nzp_pack_ls, p.nzp_pack, k.nzp_kvar, " +
                            " r.sum_money, " + pack_type + ", '" + calcMonth + "'" + sConvToDate + ", o.dat_opl " + 
                            dat_uchet +  ", o.dat_opl" +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr r," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_oplats o, " +
                            Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                            " WHERE r.id_dog = d.dog_id AND r.id_serv = s.id_serv AND r.kod_oplat = o.kod_oplat" +
                            " AND o.kod_oplat = p.unl AND o.ls_id = k.id AND o.nzp_pack = " + id_pack +
                            " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                            " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(conDb, sql, true);
                    }
                    else if (!sec32exists && pack_type == 33 && is_raspr == 1)
                    {
                        MonitorLog.WriteLog(" Распределенная пачка оплат 33 но распределение берем из начислений  ", MonitorLog.typelog.Info, true);
                        //кладем в fn_suppplier
                        sql =
                            " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter + "fn_supplier" + calcMonth.Substring(3, 2) +
                            " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                            " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_user, s_dolg, s_forw ) " +
                            " SELECT s.nzp_serv, d.nzp_supp, p.nzp_pack_ls, p.nzp_pack, k.nzp_kvar, " +
                            " r.sum_money, " + pack_type + ", '" + calcMonth + "'" + sConvToDate + ", o.dat_opl" +
                            dat_uchet  + ", 0, 0, 0" +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv r," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_oplats o, " +
                            Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                            " WHERE r.dog_id = d.dog_id AND r.nzp_serv = s.id_serv AND r.nzp_serv = o.id_serv  " +
                            " AND o.kod_oplat = p.unl AND o.ls_id = k.id AND o.nzp_pack = " + id_pack +
                            " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                            " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(conDb, sql, true);
                        
                    }
                    else if (!sec32exists && pack_type != 33 && is_raspr == 1)
                    {
                        //кладем в from_suppplier
                        MonitorLog.WriteLog(" Распределенная пачка оплат не 33(40 50) но распределение берем из начислений  ", MonitorLog.typelog.Info, true);
                        sql =
                            " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter + "from_supplier" +
                            " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                            " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_plat) " +
                            " SELECT s.nzp_serv, d.nzp_supp, p.nzp_pack_ls, p.nzp_pack, k.nzp_kvar, " +
                            " r.sum_money, " + pack_type + ", '" + calcMonth + "'" + sConvToDate + ", o.dat_opl " +
                            dat_uchet +  ", o.dat_opl" +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv r," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_oplats o, " +
                            Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                            " WHERE r.dog_id = d.dog_id AND r.nzp_serv = s.id_serv AND r.nzp_serv = o.id_serv  " +
                            " AND o.kod_oplat = p.unl AND o.ls_id = k.id AND o.nzp_pack = " + id_pack +
                            " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                            " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                        ret = ExecSQL(conDb, sql, true);
                        
                    }

                    sql =
                       " UPDATE " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls" +
                       " SET unl = 0 " +
                       " WHERE nzp_pack = " + nzp_pack;
                    ret = ExecSQL(conDb, sql, true);
                    MonitorLog.WriteLog(" по полатам все закончено  ", MonitorLog.typelog.Info, true);
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
        public Returns Run(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);

            #region Пока не случилась ошибка (мне очень не нравится но написано именно так)
            try
            {
                #region Выбрать вариант загрузки sec6exists sec22exists sec32exists
                // вставка домовых параметров 
                string sql = "";

                //изменение статуса загрузки 
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка оплат' where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                MonitorLog.WriteLog("AddOplatServ: начало  ", MonitorLog.typelog.Info, true);
                // надо убрать count -не рационально
                sql =
                    " SELECT count(*) as num6 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec6exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                sql =
                    " SELECT count(*) as num22 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec22exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                sql =
                    " SELECT count(*) as num32 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr " +
                    " WHERE nzp_file = " + finder.nzp_file;
                bool sec32exists = (Convert.ToInt32(ExecScalar(conDb, sql, out ret, true)) > 0);

                string calcMonth = "01." + finder.month.ToString("00") + "." + finder.year;

                int packType = 20;


                #endregion Выбрать вариант загрузки


                if (sec6exists && !sec22exists)
                {
                    #region имеется только 6 секция поэтому производится генерация нужных данных на основе начисления   sec6exists && !sec22exists

                    #region удалить предыдущие загрузки
                    sql =
                        "delete from " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                            "from_supplier" + " where nzp_pack_ls in (select nzp_pack_ls from " +
                              Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                             " where nzp_pack in (select nzp_pack from " +
                             Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                             "where file_name ='Суммарная пачка по файлу " + finder.nzp_file + "'  ) ) ";
                    MonitorLog.WriteLog("удаление пред загрузок7  " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        "delete from " +
                              Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                            " where nzp_pack in (select nzp_pack from " +
                             Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                             "where file_name ='Суммарная пачка по файлу " + finder.nzp_file + "'  )  ";
                    MonitorLog.WriteLog("удаление пред загрузок8 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                          "delete from " +
                                Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                               "where file_name ='Суммарная пачка по файлу " + finder.nzp_file + "'  ";
                    MonitorLog.WriteLog("удаление пред загрузок9 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    #endregion удалить предыдущие загрузки


                    MonitorLog.WriteLog("имеется только 6 секция ", MonitorLog.typelog.Info, true);

                    string table = "_temp_table_info_pack";

                    sql = "DROP TABLE IF EXISTS " + table;
                    ret = ExecSQL(conDb, sql, false);

                    sql = " CREATE TEMP TABLE " + table + "( " +
                          " id serial," +
                          " nzp_kvar " + DBManager.sDecimalType + "(14,0)," +
                          " nzp_supp " + DBManager.sDecimalType + "(14,0)," +
                          " sum_money " + DBManager.sDecimalType + "(14,2)," +
                          " nzp_serv " + DBManager.sDecimalType + "(14,0)," +
                          " nzp_pack_ls " + DBManager.sDecimalType + "(14,0) ) " +
                          DBManager.sUnlogTempTable;
                    ret = ExecSQL(conDb, sql, true);
                    MonitorLog.WriteLog("подготовка временной таблицы оплат  " + sql, MonitorLog.typelog.Info, true);

                    sql =
                        " INSERT INTO " + table +
                        " ( nzp_kvar, nzp_supp, sum_money, nzp_serv )" +
                        " SELECT DISTINCT fk.nzp_kvar, s.nzp_supp, fs.sum_money, fss.nzp_serv " +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_services fss, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_dog s, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                        " WHERE  fs.ls_id = fk.id AND s.dog_id = fs.dog_id AND fss.nzp_serv = fs.nzp_serv " +
                        " AND fs.nzp_file = fss.nzp_file AND fs.nzp_file = s.nzp_file AND" +
                        " fs.nzp_file = fk.nzp_file AND" + " fs.nzp_file = " + finder.nzp_file +
                        " AND fs.sum_money > 0";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);


                    sql =
                        " insert into " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                        " ( pack_type, nzp_bank,  num_pack, dat_uchet,   dat_pack,   count_kv," +
                        " sum_pack, flag, dat_vvod,  sum_rasp, sum_nrasp, nzp_rs, dat_inp, file_name)" +
                        " SELECT  " + packType.ToString() + ",  1998, '1',   '15." + finder.month + "." + finder.year + "' " + sConvToDate +
                        ", '" +
                        calcMonth + "'" + sConvToDate + ", count(*), sum(sum_money),  11, " + sCurDate + ", " +
                        "sum(sum_money), '0', 1, " + sCurDate + ",  'Суммарная пачка по файлу " + finder.nzp_file + "' " +
                        " FROM " + table;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    decimal nzpPack = GetSerialValue(conDb);

                    //кладем отдельные оплаты
                    sql =
                        " INSERT INTO " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter +
                        "pack_ls " +
                        "( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod, dat_uchet, " +
                        " anketa, info_num, inbasket, alg, unl,  nzp_user, incase, nzp_rs)" +
                        " SELECT " + nzpPack + ", nzp_kvar, sum(sum_money), '" + calcMonth + "', 40, 1,  '" + calcMonth +
                        "'," +
                        "'15" + calcMonth.Substring(2, 8) + "', " +
                        "  nzp_kvar, 1," + "0, '1',  id,  " + finder.nzp_user + ", 0, 1" +
                        " FROM " + table + " group by 1,2,13 ";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        " UPDATE " + table + " set nzp_pack_ls =" +
                        " (SELECT nzp_pack_ls FROM " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) +
                        tableDelimiter + "pack_ls" +
                        " WHERE  unl = " + table + ".id AND nzp_pack = " + nzpPack + " )";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    //кладем в from_suppplier

                    sql =
                        " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                        "from_supplier" +
                        " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge,  num_ls, alias_ls," +
                        " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_plat) " +
                        " SELECT nzp_serv, nzp_supp, nzp_pack_ls, -7, nzp_kvar, nzp_kvar," +
                        " sum_money, 40, '" + calcMonth + "' ,  '" + calcMonth + "', '15" + calcMonth.Substring(2, 8) +
                        "','" + calcMonth + "'" +
                        " FROM " + table;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        " UPDATE " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls" +
                        " SET unl = 0 " +
                        " WHERE nzp_pack = " + nzpPack;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    MonitorLog.WriteLog("имеется только 6 секция закончили ", MonitorLog.typelog.Info, true);
                    return ret;

                    #endregion
                }
                else
                {
                    #region не только 6 секция обработка пачек оплат их может быть много поэтому обрабатываем пачки последовательно по типам

                    #region удалить предыдущие загрузки
                    sql =
                        "delete from " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                            "from_supplier" + " where nzp_pack_ls in (select nzp_pack_ls from " +
                              Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                             " where transaction_id like '" + finder.nzp_file + "_%') ";
                    MonitorLog.WriteLog("удаление пред загрузок1  " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        "delete from " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                           "fn_supplier" + calcMonth.Substring(3, 2) + " where nzp_pack_ls in (select nzp_pack_ls from " +
                              Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                             " where transaction_id like '" + finder.nzp_file + "_%') ";
                    MonitorLog.WriteLog("удаление пред загрузок2 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        "delete from " +
                              Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                             " where transaction_id like '" + finder.nzp_file + "_%' ";
                    MonitorLog.WriteLog("удаление пред загрузок4 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                        "update " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats set id_serv=null where nzp_file="
                               + finder.nzp_file + " ";
                    MonitorLog.WriteLog("удаление пред загрузок5 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    sql =
                         "delete from " +
                               Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                              " where file_name like '" + finder.nzp_file + "_%' ";
                    MonitorLog.WriteLog("удаление пред загрузок6 " + sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    #endregion удалить предыдущие загрузки

                    // приходит из секции packType=40
                    // file_oplats -pack_ls  file_pack pack   file_raspr fn_supplier from_supplier 

                    #region формирование пачки

                    // положить все пачки которые пришли в базу данных 
                    sql =
                        " insert into " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack " +
                        " ( pack_type, nzp_bank,  num_pack, dat_uchet,   dat_pack,   count_kv," +
                        " sum_pack, flag, dat_vvod,  sum_rasp, sum_nrasp, nzp_rs, dat_inp, file_name)" +

                        " SELECT  " + packType.ToString() + ",  1998, coalesce(num_plat,'0') ,   dat_plat  ," +

                        "dat_plat , kol_plat, sum_plat, case when is_raspr=1 then 21 else 11 end , dat_plat , " +
                     "sum_plat, '0', " + finder.nzp_file + ", " + sCurDate + ",  " + finder.nzp_file + "||'_'||id " +
                     " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_pack where nzp_file =" + finder.nzp_file;

                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    // выставить коды получившихся пачек в загружаемые таблицы , теперь можно вставить все pack_ls 
                    sql = "update " + Points.Pref + DBManager.sUploadAliasRest + " file_oplats set id_serv= " +
                            " (select min(nzp_pack) from " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack a" +
                            "   where a.file_name ='" + finder.nzp_file +
                            "_'||" + Points.Pref + DBManager.sUploadAliasRest + " file_oplats.nzp_pack " +
                           " and a.nzp_rs =" + finder.nzp_file + ") where nzp_file =" + finder.nzp_file;

                    //  update fbill_upload.file_oplats set id_serv= (select min(nzp_pack) from
                    // fbill_fin_15.pack a   where a.file_name ='214_'||fbill_upload.file_oplats.nzp_pack ) where nzp_file =214; 
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    //выставить актуальный код оплаты (40 50 и 333 и другие)

                    sql = "update " + Points.Pref + DBManager.sUploadAliasRest + " file_oplats set type_oplat= " +
                        " (select kod_type_opl from " + Points.Pref + DBManager.sUploadAliasRest + "file_pack a" +
                        "   where a.id =" + Points.Pref + DBManager.sUploadAliasRest + " file_oplats.nzp_pack " +
                        " and nzp_file =" + finder.nzp_file +
                        ") where nzp_file =" + finder.nzp_file;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    //                 alter table fbill_upload. file_oplats add nzp_pack_ls_real integer
                    //                 create index isdfsd on fbill_fin_15.pack_ls(transaction_id, nzp_pack_ls);
                    //                 alter table fbill_upload. file_raspr add nzp_pack_ls_real integer;
                    //                 alter table fbill_upload. file_raspr add nzp_pack integer;  

                    //кладем отдельные оплаты  pack_ls - согласно пачек 
                    sql =
                        " INSERT INTO " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls " +
                        "( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod, dat_uchet, " +
                        " transaction_id, info_num, inbasket, alg, unl,  nzp_user, incase, nzp_rs)" +

                        "select distinct o.id_serv, k.nzp_kvar , o.sum_oplat , o.mes_oplat " +
                        " ,  type_oplat,   1, o.dat_opl,  o.dat_uchet,  " +
                         " '" + finder.nzp_file + "_'||o.id , 1, 0, 1, o.id , " + finder.nzp_user + ",  0,  1  " +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_oplats o , "
                        + Points.Pref + DBManager.sUploadAliasRest + "file_kvar k where o.nzp_file =k.nzp_file and o.nzp_file = "
                        + finder.nzp_file + " and o.ls_id=k.id and o.id_serv>0 ";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);
                    //   INSERT INTO fbill_fin_15.pack_ls ( nzp_pack,  num_ls,    g_sum_ls,  dat_month, kod_sum, paysource,dat_vvod,dat_uchet,  transaction_id, info_num, inbasket, alg, unl,  nzp_user, incase, nzp_rs) 
                    // select distinct o.id_serv, k.nzp_kvar , o.sum_oplat , o.mes_oplat  ,  type_oplat,   1, o.dat_opl,  o.dat_uchet, '214_'||o.id , 1, 0, 1, o.id , -214,  0,  1   
                    // FROM fbill_upload. file_oplats o , fbill_upload. file_kvar k where o.nzp_file =k.nzp_file and o.nzp_file = 214 and o.ls_id=k.id and o.id_serv >0 group by 1,2,13 



                    // положили также реальные ссылки на оплаты , теперь пора переходить к распределениям 
                    sql =
                        " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + " file_oplats " +
                        " set nzp_pack_ls_real = (SELECT nzp_pack_ls " +
                        " from " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls a" +
                        " where a.transaction_id ='" + finder.nzp_file + "_'||" + Points.Pref + DBManager.sUploadAliasRest + " file_oplats.id " +
                        " and a.nzp_pack =" + Points.Pref + DBManager.sUploadAliasRest + "file_oplats.id_serv " +
                        " ) " +
                        " where nzp_file =" + finder.nzp_file +
                        " and id_serv>0 " +
                        " and  exists (select 1 from " + Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter +
                        "pack where file_name like'" + finder.nzp_file + "_%') ";
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    //UPDATE fbill_upload. file_oplats set nzp_pack_ls_real = (SELECT nzp_pack_ls  from fbill_fin_15.pack_ls a 
                    //where a.transaction_id ='214_'||fbill_upload. file_oplats.id  and a.nzp_pack =fbill_upload.file_oplats.id_serv  )  where nzp_file =214
                    // and id_serv>0 and  exists (select 1 from fbill_fin_15.pack where file_name like'214_%');

                    #endregion

                    //if (sec32exists && packType == 40 || packType == 50)  // from_supplier
                    //{
                    // from_supplier
                    #region 6 секция 40||50 распределенная

                    MonitorLog.WriteLog(" Распределенная пачка оплат 40 или 50  ", MonitorLog.typelog.Info, true);

                    sql =
                                    " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                                    "from_supplier" +
                                    " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                                    " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_plat) " +

                                        " SELECT s.nzp_serv, d.nzp_supp, o.nzp_pack_ls_real, o.id_serv, k.nzp_kvar, " +
                                        " r.sum_money, o.type_oplat, o.mes_oplat, o.dat_opl, o.dat_uchet,o.dat_opl " +

                                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr r," +
                                        Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                                        Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                                        Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +

                                        " WHERE r.id_dog = d.dog_id AND r.id_serv = s.id_serv AND r.kod_oplat = o.kod_oplat" +
                                        " AND o.ls_id = k.id " +
                                        " and o.type_oplat in (40,50) " +
                                        " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                                        " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);




                    #endregion 6 секция 40 ||50 распределенная


                    //}

                    //if (sec32exists && (packType != 40 || packType != 50)) // fn_supplier 
                    //{
                    // fn_supplier 
                    #region 6 секция 33 распределенная
                    MonitorLog.WriteLog(" распределенная пачка оплат 33   ", MonitorLog.typelog.Info, true);

                    sql =
                        " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                        "fn_supplier" + calcMonth.Substring(3, 2) +
                        " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                        " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_user, s_dolg, s_forw ) " +
                        " SELECT s.nzp_serv, d.nzp_supp, o.nzp_pack_ls_real, o.id_serv, k.nzp_kvar, " +
                        " r.sum_money, o.type_oplat, o.mes_oplat, o.dat_opl, o.dat_uchet, " +
                        " 0, 0, 0" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr r," +
                        Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                        Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                        Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +
                        //Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                        " WHERE r.id_dog = d.dog_id AND r.id_serv = s.id_serv AND r.kod_oplat = o.kod_oplat" +
                        " AND o.ls_id = k.id " +
                        " and o.type_oplat not in (40,50) " +
                        " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                        " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);

                    #endregion 6 секция 33 распределенная


                    //}

                    // заполнить распределение по пачкам которые распределены по секции оплаты но распределения по нему не представлено 
                    // тогда по charge_xx
                    sql =
                               " INSERT INTO " + finder.bank + "_charge_" + calcMonth.Substring(8, 2) + tableDelimiter +
                               "fn_supplier" + calcMonth.Substring(3, 2) +
                               " ( nzp_serv, nzp_supp, nzp_pack_ls, nzp_charge, num_ls, " +
                               " sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, s_user, s_dolg, s_forw ) " +
                               " SELECT s.nzp_serv, d.nzp_supp, o.nzp_pack_ls_real, o.id_serv, k.nzp_kvar, " +
                               " r.sum_money, o.type_oplat , o.mes_oplat, o.dat_opl, " +
                               " o.dat_uchet, 0, 0, 0" +
                               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv r," +
                               Points.Pref + DBManager.sUploadAliasRest + "file_services s," +
                               Points.Pref + DBManager.sUploadAliasRest + "file_dog d," +
                               Points.Pref + DBManager.sUploadAliasRest + "file_kvar k," +
                               Points.Pref + DBManager.sUploadAliasRest + "file_oplats o, " +
                               Points.Pref + "_fin_" + calcMonth.Substring(8, 2) + tableDelimiter + "pack_ls p " +
                               " WHERE r.dog_id = d.dog_id AND r.nzp_serv = s.id_serv AND r.nzp_serv = o.id_serv  " +
                               " AND o.kod_oplat = p.unl AND o.ls_id = k.id " +
                               " AND o.kod_oplat not in (select kod_oplat from " + Points.Pref + DBManager.sUploadAliasRest +
                               "file_raspr where nzp_file =" + finder.nzp_file + " )" +
                               " AND r.nzp_file = s.nzp_file AND r.nzp_file = d.nzp_file AND r.nzp_file = k.nzp_file" +
                               " AND r.nzp_file = o.nzp_file AND r.nzp_file = " + finder.nzp_file;
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                    ret = ExecSQL(conDb, sql, true);



                    #endregion не только 6 секция обработка пачек оплат их может быть много
                }

            }
            #endregion Пока не случилась ошибка (мне очень не нравится но написано именно так)

            #region Обработка любой ошибки
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AddOplatServ: ошибка  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "AddOplatServ: ошибка";
                ret.result = false;
                return ret;
            }
            #endregion Обработка любой ошибки

            return ret;
        }
    }
}
