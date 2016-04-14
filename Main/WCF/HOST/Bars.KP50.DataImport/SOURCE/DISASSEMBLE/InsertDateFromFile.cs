using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using Bars.KP50.DataImport;
using Bars.KP50.DataImport.SOURCE;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase 
{
    public class InsertDateFromFile : DbAdminClient
    {
        /// <summary>
        /// Функция разбора 6 секции  file_serv
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsType Run(IDbConnection conDb, FilesDisassemble finder)
        {
            string sql;
            int commandTime = 3600;
            DbDisUtils du = new DbDisUtils(finder);          

            MonitorLog.WriteLog("Старт разбора начислений (ф-ция InsertDateFromFile)", MonitorLog.typelog.Info, true);

            //проверка: нет ли значений null в file_dog.nzp_supp
            Returns ret = du.CheckColumnOnEmptiness("nzp_supp", "file_dog", "пустые ссылки на поставщиков в таблице file_dog");
            if (!ret.result)
            {
                throw new Exception(ret.text);
            }           

            //изменение статуса загрузки
            sql = "UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка начислений' WHERE nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);

            #region Установить наш номер услуги 
            // сохранить номер услуги из файла , не портить при повторном разборе 
            //sql = "UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_serv set id_serv =nzp_serv  WHERE nzp_file = " + finder.nzp_file + " and id_serv is null ";
            
            //ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);

            // добавить индексы file_services -nzp_file , nzp_serv  - file_serv nzp_file id_serv nzp_serv 
            sql = "UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_serv set nzp_serv_real =(SELECT a.nzp_serv FROM " + Points.Pref + DBManager.sUploadAliasRest + 
                "file_services a WHERE a.id_serv ="+
                Points.Pref + DBManager.sUploadAliasRest + "file_serv.nzp_serv and a.nzp_file=" + finder.nzp_file + " GROUP BY 1)  WHERE nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);


            #endregion

            #region Выставить УКАС в загружаемом файле если его нет
            
            sql = 
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar  set ukas = nzp_kvar " +
                " WHERE  nzp_file =" + finder.nzp_file +" AND " + sNvlWord + "(ukas,0) = 0";
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            #region Создаем индексы
            
            //для таблицы тарифов
            ret = ExecSQL(conDb, " create index ixctarif on " + finder.bank + "_data" + tableDelimiter + "tarif(nzp_kvar,nzp_serv,nzp_supp,nzp_frm);", false);
            if (ret.result)
            {
                ExecSQL(conDb, DBManager.sUpdStat + "  " + finder.bank + "_data" + tableDelimiter + "tarif", true);
            }
            ret = ExecSQL(conDb, " create index ixchr09 on " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                           finder.month.ToString("00") + "(nzp_kvar,nzp_serv,nzp_supp,nzp_frm)", false);
            if (ret.result)
            {
                ExecSQL(conDb, DBManager.sUpdStat + " " + finder.bank + "_charge_" + (finder.year - 2000) + DBManager.tableDelimiter + "charge_" +finder.month.ToString("00"), true);
            }

            #endregion

            #endregion Выставить УКАС в загружаемом файле если его нет


            #region Загрузка в charge

            //// делим на куски по 5000 квартир , больше проблематично 

            sql = " select count(*) kol from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file = " + finder.nzp_file;

            DataTable dt3 = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            int kol_ = 0;
            foreach (DataRow rr3 in dt3.Rows)
            {
                kol_ = Convert.ToInt32(rr3["kol"]);
                MonitorLog.WriteLog("Кол-во ЛС в файле:" + kol_, MonitorLog.typelog.Info, true);
            }

            //по сколько квартир берем
            int kol_kvar = 5000;

            for (int i2 = 0; i2 < kol_; i2 = i2 + kol_kvar)
            {
                try
                {
                    sql = " drop table t_skip ";
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                }
                catch{}

                sql = " create temp table t_skip(nzp_kvar integer) ";
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                string pskip = i2.ToString();
#if PG
                sql = " insert into  t_skip(nzp_kvar ) select nzp_kvar from " +
                             Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file=" + finder.nzp_file + " limit " + kol_kvar + " offset " + pskip;
#else
                sql = " insert into  t_skip(nzp_kvar ) select  skip " + pskip + " first " + kol_kvar + " nzp_kvar from " +
                             Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file=" + finder.nzp_file + "  ";
#endif

                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                DataImportUtils d1 = new DataImportUtils();
                Dictionary<string, List<string>> index1 = new Dictionary<string, List<string>>();
                index1.Add("ixfort_skip1", new List<string>() { "nzp_kvar" });
                d1.CreateOneIndex(conDb, "t_skip", index1);

                index1.Clear();
                index1.Add("ixforfs1", new List<string>() { "nzp_file", "ls_id", "nzp_serv", "supp_id", "nzp_measure" });
                d1.CreateOneIndex(conDb, Points.Pref + DBManager.sUploadAliasRest + "file_serv", index1);

                try
                {
                    sql = " drop table t_skip_temp";
                    ExecSQL(conDb, sql, false);
                }
                catch { }

                //string supplier = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2")
                //    ? " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest +
                //      "file_supp ss ON ss.supp_id=fs.supp_id and ss.nzp_file=fs.nzp_file and ss.nzp_supp>0 "
                //    : ((finder.versionFull == "1.3.2" || finder.versionFull == "1.3.3" || finder.versionFull == "1.3.4"
                //    || finder.versionFull == "1.3.5" || finder.versionFull == "1.3.6" || finder.versionFull == "1.3.7" || finder.versionFull == "1.3.8")
                //        ? " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest +
                //          "file_dog ss ON ss.dog_id=fs.dog_id and ss.nzp_file=fs.nzp_file and ss.nzp_supp>0 "
                //        : "");
                string supplier = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2")
                    ? " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest +
                      "file_supp ss ON ss.supp_id=fs.supp_id and ss.nzp_file=fs.nzp_file and ss.nzp_supp>0 "
                    : " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest +
                          "file_dog ss ON ss.dog_id=fs.dog_id and ss.nzp_file=fs.nzp_file and ss.nzp_supp>0 ";


                sql = " SELECT DISTINCT  fs.id, fs.nzp_file, fs.ls_id, fs.supp_id, fs.nzp_serv_real," +
                      " fs.sum_insaldo, fs.eot, fs.reg_tarif_percent, fs.reg_tarif, m.nzp_measure," +
                      " fs.fact_rashod, fs.norm_rashod, fs.is_pu_calc, fs.sum_nach, fs.sum_reval," +
                      " fs.sum_subsidy as sum_subsidy, 0 as sum_subsidyp, fs.sum_lgota, fs.sum_lgotap, fs.sum_smo," +
                      " fs.sum_smop, fs.sum_money, fs.is_del, fs.sum_outsaldo, fs.servp_row_number," +
                      " fk.ukas as nzp_kvar, ss.nzp_supp as nzp_supp, sum_reval as reval, " +
                      " sum_nach+sum_reval as sum_charge, eot*fact_rashod  sum_t_e," +
                      " reg_tarif*norm_rashod sum_t_en, eot*norm_rashod sum_t_ene, fk.nzp_kvar as num_ls, fs.met_calc, fs.pkod " +
#if PG
                    " INTO TEMP t_skip_temp " +
#else
#endif
                      " FROM t_skip ts, " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                      " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs ON fs.ls_id= fk.id and fs.nzp_file = fk.nzp_file" +
                      // " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest + "file_services s ON s.id_serv = fs.nzp_serv and s.nzp_file = fs.nzp_file " +
                      //" LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest + "file_supp ss ON ss.supp_id=fs.supp_id and ss.nzp_file=fs.nzp_file and ss.nzp_supp>0 " +
                      supplier +
                      " LEFT OUTER JOIN " + Points.Pref + DBManager.sUploadAliasRest + "file_measures m ON fs.nzp_measure=m.id_measure and fs.nzp_file = m.nzp_file" +
                      //" LEFT OUTER JOIN t_skip ts ON  ts.nzp_kvar=fk.nzp_kvar " +
                      " WHERE ts.nzp_kvar=fk.nzp_kvar and fs.nzp_file=" + finder.nzp_file +
                      " ORDER BY fs.nzp_file, ls_id, fs.id "
#if PG
                    ;
#else
     + " INTO TEMP t_skip_temp ";
#endif

                ret = ExecSQL(conDb, sql, true, commandTime);

                string copy_ls = "0";
                int kol = 0;

                MonitorLog.WriteLog("Старт загрузки в charge (i2 = " + i2 + ")", MonitorLog.typelog.Info, 1, 2, true);

                DataImportUtils d2 = new DataImportUtils();
                Dictionary<string, List<string>> index2 = new Dictionary<string, List<string>>();
                index2.Add("ixfort_skip1", new List<string>() { "id", "met_calc" });
                d2.CreateOneIndex(conDb, "t_skip_temp", index2);

                sql = " UPDATE t_skip_temp SET met_calc = 100 WHERE met_calc not in (SELECT nzp_frm FROM " + finder.bank + 
                    DBManager.sKernelAliasRest + "formuls )";

                ret = ExecSQL(conDb, sql, true, commandTime);

                sql = 
                    " SELECT *  FROM t_skip_temp" +
                    " ORDER BY nzp_file,ls_id, id ";

                DataTable dtnote =  ClassDBUtils.OpenSQL( sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow rr in dtnote.Rows)
                {
                    // Перебор строк разбираемых файлов 

                    if (copy_ls == Convert.ToString(rr["id"]))
                    {
                        kol++;
                        continue;
                    }
                    else
                    {
                        copy_ls = Convert.ToString(rr["id"]);
                    }

                    string met_calc = String.IsNullOrEmpty(rr["met_calc"].ToString()) ? "100" : rr["met_calc"].ToString();
                    string nzp_measure = String.IsNullOrEmpty(rr["nzp_measure"].ToString()) ? "0" : rr["nzp_measure"].ToString();

                    #region добавление в charge
                    sql = "INSERT INTO " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                          tableDelimiter + "charge_" + finder.month.ToString("00") +
                          "( nzp_kvar, num_ls, nzp_serv," +
                          " nzp_supp, nzp_frm, dat_charge," +
                          " tarif, tarif_p, rsum_tarif," +
                          " gsum_tarif, rsum_lgota, sum_tarif," +
                          "  sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p," +
                          " sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p," +
                          " sum_lgota_p, sum_nedop, sum_nedop_p, " +
                          " sum_real, sum_charge, reval," +
                          " real_pere, sum_pere, real_charge," +
                          " sum_money, money_to, money_from," +
                          " money_del, sum_fakt, fakt_to," +
                          " fakt_from, fakt_del, sum_insaldo," +
                          " izm_saldo, sum_outsaldo, isblocked," +
                          " is_device, c_calc, c_sn," +
                          " c_okaz, c_nedop, isdel," +
                          " c_reval, reval_tarif, reval_lgota," +
                          " tarif_f, sum_tarif_eot, sum_tarif_sn_eot," +
                          " sum_tarif_sn_f, rsum_subsidy, sum_subsidy," +
                          " sum_subsidy_p,  sum_subsidy_reval, sum_subsidy_all," +
                          " sum_lgota_eot, sum_lgota_f, sum_smo," +
                          " tarif_f_p, sum_tarif_eot_p, sum_tarif_sn_eot_p," +
                          " sum_tarif_sn_f_p, sum_lgota_eot_p, sum_lgota_f_p," +
                          " sum_smo_p, sum_tarif_f, sum_tarif_f_p, " +
                          " order_print  )" +

                          " VALUES" +

                          " (" + Convert.ToString(rr["nzp_kvar"]) + ", " + Convert.ToString(rr["num_ls"]) + "," + Convert.ToString(rr["nzp_serv_real"]) + "," +
                          Convert.ToString(rr["nzp_supp"]) + "," + met_calc + ", null, " + 
//                          Convert.ToString(rr["eot"]) + ", 0,  " + Convert.ToString(rr["sum_nach"]) + "," +
                          Convert.ToString(rr["reg_tarif"]) + ", 0,  " + Convert.ToString(rr["sum_nach"]) + "," +
                          Convert.ToString(rr["sum_nach"]) + ",0, " + Convert.ToString(rr["sum_nach"]) + "," +
                          " 0, 0, 0," + 
                          Convert.ToString(rr["sum_lgota"]) + ",0 , 0," +
                          " 0, 0, 0," +
                          Convert.ToString(rr["sum_nach"]) + "," + Convert.ToString(rr["sum_charge"]) + "," + "0" +
                          "," + "0" + "," + "0" + "," + Convert.ToString(rr["sum_reval"]) + "," +
                          Convert.ToString(rr["sum_money"]) + "," + "0" + "," + Convert.ToString(rr["sum_money"]) + "," + 
                          "0" + "," + Convert.ToString(rr["sum_money"]) + "," + "0" + "," +
                          "0" + "," + "0" + "," + Convert.ToString(rr["sum_insaldo"]) + "," + 
                          "0" + "," + Convert.ToString(rr["sum_outsaldo"]) + "," + nzp_measure + "," + 
                          Convert.ToString(rr["is_pu_calc"]) + "," + Convert.ToString(rr["fact_rashod"]) + "," + Convert.ToString(rr["norm_rashod"]) +
                          ", 0 , " + Convert.ToString(rr["reg_tarif_percent"]) + "," + Convert.ToString(rr["is_del"]) + " , " +
//                          "   0" + "," + "0" + "," + "0" + "," + 
                          "   0" + "," + Convert.ToString(rr["eot"]) + "," + "0" + "," +
                          Convert.ToString(rr["reg_tarif"]) + "," + Convert.ToString(rr["sum_t_e"]) + "," + Convert.ToString(rr["sum_t_ene"]) + "," +
                          Convert.ToString(rr["sum_t_en"]) + "," + "0" + "," + Convert.ToString(rr["sum_subsidy"]) +
                          "," + "0" + "," + Convert.ToString(rr["sum_subsidyp"]) + "," + "0" + "," + 
                          "0" + "," + "0" + "," + Convert.ToString(rr["sum_smo"]) + "," + 
                          "0" + "," + "0" + "," + "0" + "," +
                          "0" + "," + "0" + "," + "0" + "," + 
                          "0" + "," + "0" + "," + "0" + "," + 
                          finder.nzp_file +")";

                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                    #endregion
                }

                //вставить pkod в kvar
                sql = " DROP TABLE t_pkod; ";
                ExecSQL(conDb, sql);

                sql = " CREATE TEMP TABLE t_pkod (nzp_kvar INTEGER, pkod " + sDecimalType + " (13, 0)); ";
                ExecSQL(conDb, sql);

                sql = " INSERT INTO t_pkod SELECT nzp_kvar, max(pkod) AS pkod FROM t_skip_temp GROUP BY 1; ";
                ExecSQL(conDb, sql);

                //центральный банк
                sql = " UPDATE " + Points.Pref + sDataAliasRest + "kvar SET pkod = " +
                      " (SELECT max(pkod) FROM t_pkod t WHERE t.nzp_kvar = " + Points.Pref + sDataAliasRest + "kvar.nzp_kvar) " +
                      " WHERE EXISTS (SELECT 1 from t_pkod s WHERE s.nzp_kvar = " + Points.Pref + sDataAliasRest + "kvar.nzp_kvar" +
                      " AND " + Points.Pref + sDataAliasRest + "kvar.pkod is null); ";
                ExecSQL(conDb, sql);
                //локальный банк
                sql = " UPDATE " + finder.bank + sDataAliasRest + "kvar SET pkod = " +
                      " (SELECT max(pkod) FROM t_pkod t WHERE t.nzp_kvar = " + finder.bank + sDataAliasRest + "kvar.nzp_kvar) " +
                      " WHERE EXISTS (SELECT 1 from t_pkod s WHERE s.nzp_kvar = " + finder.bank + sDataAliasRest + "kvar.nzp_kvar" +
                      " AND " + finder.bank + sDataAliasRest + "kvar.pkod is null); ";
                ExecSQL(conDb, sql);

                // исправить формулы для услуг 
                if (kol > 0)
                {
                    // Встретились двойники загружаемых записей
                    var res = new ReturnsType(false, "Данные не сохранены - Встретились двойники загружаемых записей ",
                        -1);
                    MonitorLog.WriteLog("Ошибка при разборе начислений (ф-ция insertDateFromFile) : " + res.text,
                        MonitorLog.typelog.Error, true);
                    res.result = false;
                    return res;
                }
            }
            #region Единица измерения и формула
            // Нужно выставить nzp_frm , согласно файла prm_tarifs Желательно учесть единицу измерения
            // 2 шаг выставляем если единица измерения выставлена неверно 


            sql = 
                " UPDATE " + finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter + "charge_" +finder.month.ToString("00") + 
                " SET c_calc = (case when tarif>0 then trunc(sum_tarif/tarif,4) else 0 end) " +
                " WHERE isblocked>0 and c_calc=0 AND order_print =" + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);
            
            sql =
                " UPDATE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                " SET nzp_frm = 1040" + 
                " WHERE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") + ".nzp_serv = 9" +
                " AND nzp_frm = 100 " +
                " AND order_print =" + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);

            sql =
                " UPDATE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                " SET nzp_frm = 1814" +
                " WHERE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") + ".nzp_serv = 8" +
                " AND nzp_frm = 100 " +
                " AND order_print =" + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);

            sql = " UPDATE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                  " SET nzp_frm = " + 
                     "(SELECT " + sNvlWord + "(MIN(b.nzp_frm),100)" +
                     " FROM " + Points.Pref + "_kernel" + tableDelimiter + "prm_tarifs b ," +
                     Points.Pref + "_kernel" + tableDelimiter + "formuls f " +
                     " WHERE b.nzp_serv=" + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") + ".nzp_serv " +
                     " AND f.nzp_measure =" + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") + ".isblocked " +
                     " AND f.nzp_frm = b.nzp_frm ) " +
                  " WHERE nzp_frm = 100 AND isblocked > 0 AND order_print =" + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);


            sql = " UPDATE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                  " SET nzp_frm = " +
                     "(SELECT " + sNvlWord + "(MIN(b.nzp_frm),100)" +
                     " FROM " + Points.Pref + "_kernel" + tableDelimiter + "prm_tarifs b " +
                     " WHERE b.nzp_serv=" + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") + ".nzp_serv ) " +
                  " WHERE nzp_frm = 100 AND isblocked > 1 AND order_print =" + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);

            #endregion Единица измерения

            #endregion Загрузка в charge

            #region Загрузка тарифа

            #region создаем временную таблицу
                sql = "drop table t_for_tarif_table";
                DBManager.ExecSQL(conDb, null, sql, false, commandTime);
            
            sql = "create temp table t_for_tarif_table (" +
                  " nzp_kvar INTEGER," +
                  " num_ls INTEGER," +
                  " nzp_supp INTEGER," +
                  " nzp_serv INTEGER," +
                  " nzp_frm INTEGER," +
                  " tarif " + DBManager.sDecimalType + "(14,3), " +
                  " dat_po DATE," +
                  " dat_s DATE," +
                  " is_actual INTEGER, " +
                  " nzp_user INTEGER," +
                  " dat_when DATE, " +
                  " cur_unl INTEGER)"
                   + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conDb, null, sql, false, commandTime);
            #endregion

            MonitorLog.WriteLog("Старт загрузка тарифа ", MonitorLog.typelog.Info, true);

            //изменение статуса загрузки
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                  " set diss_status = 'Загрузка тарифов' where nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);

            #region записываем данные в табличку
            //из Charge
            sql = " insert into t_for_tarif_table " +
                        " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, " +
                        " dat_when, cur_unl) " +
                        " select distinct nzp_kvar, num_ls , nzp_serv , nzp_supp , nzp_frm  , tarif,  " + "cast('01." + finder.month.ToString("00") + "." + finder.year.ToString("0000") + "' as date )," +
                        "cast('" + finder.dat_po + "' as date) ,1,1," + sCurDate + ",0  from " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                       finder.month.ToString("00") + " where order_print = " + finder.nzp_file+ " group by 1,2,3,4,5,6 ";
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            LoadTarif lt = new LoadTarif(conDb);
            ret = lt.SetTarif("t_for_tarif_table", finder.bank);
            if (!ret.result)
            {
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
            }
            #endregion


            #region Учитываем показания из 25 секции

            MonitorLog.WriteLog("Старт загрузки показаний 25 секции", MonitorLog.typelog.Info, true);
            //изменение статуса загрузки
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка показаний 25 секции' where nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);
            

            sql = DBManager.sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar";
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            //создаем индексы
            DataImportUtils d = new DataImportUtils();
            Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();
            index.Add("ixforservls1", new List<string>() { "ls_id", "id_serv", "supp_id", "dat_start", "dat_stop" });
            d.CreateOneIndex(conDb, Points.Pref + DBManager.sUploadAliasRest + "file_servls", index);

            #region создаем временную таблицу t_for_servls


            sql = "drop table t_for_servls";
                DBManager.ExecSQL(conDb, null, sql, false, commandTime);
            sql = "create temp table t_for_servls (" +
                  " nzp_kvar INTEGER," +
                  " num_ls INTEGER," +
                  " nzp_supp INTEGER," +
                  " nzp_serv INTEGER," +
                  " nzp_frm INTEGER," +
                  " tarif " + DBManager.sDecimalType + "(14,3), " +
                  " dat_po DATE," +
                  " dat_s DATE," +
                  " is_actual INTEGER, " +
                  " nzp_user INTEGER," +
                  " dat_when DATE, " +
                  " cur_unl INTEGER)"
                   + DBManager.sUnlogTempTable; ;
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);
            #endregion


            //string supp = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2")
            //    ? "file_supp "
            //    : ((finder.versionFull == "1.3.2" || finder.versionFull == "1.3.3" || finder.versionFull == "1.3.4" ||
            //        finder.versionFull == "1.3.5" || finder.versionFull == "1.3.6" || finder.versionFull == "1.3.7" || finder.versionFull == "1.3.8")
            //        ? "file_dog"
            //        : "");
            string supp = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2")
                ? "file_supp " : "file_dog";
         
#warning "fs.supp_id = su.dog_id"
            //string whereSuppServ = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2") ? " fs.supp_id = su.supp_id "
            //    : ((finder.versionFull == "1.3.2" || finder.versionFull == "1.3.3" || finder.versionFull == "1.3.4" || finder.versionFull == "1.3.5"
            //    || finder.versionFull == "1.3.6" || finder.versionFull == "1.3.7" || finder.versionFull == "1.3.8") ? "fs.supp_id = su.dog_id" : "");
            string whereSuppServ = (finder.versionFull == "1.2.1" || finder.versionFull == "1.2.2") ? " fs.supp_id = su.supp_id " : "fs.supp_id = su.dog_id";

            sql =
                "insert into  t_for_servls (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, dat_s, dat_po, is_actual, nzp_user, " +
                        " dat_when, cur_unl)" +
                " select distinct fk.nzp_kvar as nzp_kvar, fk.nzp_kvar as num_ls, se.nzp_serv as nzp_serv," +
                " su.nzp_supp as nzp_supp, t.nzp_frm, t.tarif, fs.dat_start as dat_s, " +
                " fs.dat_stop as dat_po, 1, " + finder.nzp_user + ",  " + DBManager.sCurDate + ", 0 " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_servls fs, " +
                Points.Pref + DBManager.sUploadAliasRest + supp + " su, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_services se, " +
                finder.bank + DBManager.sDataAliasRest + "tarif t " +
                " where fk.id = fs.ls_id and " + whereSuppServ + //"fs.supp_id = su.supp_id" +
                " and se.id_serv = (fs.id_serv" + DBManager.sConvToInt + ")  and " +
                " fk.nzp_file = fs.nzp_file and fs.nzp_file = su.nzp_file" +
                " and se.nzp_file = fs.nzp_file and fk.nzp_file = " +finder.nzp_file +
                " and fs.dat_stop < '01.01.3000'  " +
                " and t.nzp_serv = se.nzp_serv and t.nzp_supp = su.nzp_supp and t.is_actual<>100 " +
                " and t.nzp_kvar = fk.nzp_kvar and  t.dat_po = '" + finder.dat_po + "'" +
                " and t.dat_s = " + "'01." + finder.month.ToString("00") + "." + finder.year.ToString("0000") + "' " +
                " group by 	fk.nzp_kvar, su.nzp_supp, se.nzp_serv, fs.dat_stop, fs.dat_start, t.tarif, t.nzp_frm ";

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);


            ret = lt.SetTarif("t_for_servls", finder.bank);

            if (!ret.result)
            {
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
            }

            #endregion

            sql =
                "update " + finder.bank + "_data" + tableDelimiter + "tarif  " +
                "set num_ls =(select num_ls from " + finder.bank + "_data" + tableDelimiter +
                "kvar a where a.nzp_kvar =" + finder.bank + "_data" + tableDelimiter + "tarif.nzp_kvar) where " +
                " nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file =" + finder.nzp_file + " )";

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            #endregion Загрузка тарифа



            MonitorLog.WriteLog("Продолжение загрузки показаний 25 секции ", MonitorLog.typelog.Info, true);
            //изменение статуса загрузки
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Загрузка начислений(продолжение)' where nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);


            MonitorLog.WriteLog("Старт кладем в параметры ", MonitorLog.typelog.Info, 1, 2, true);


            #region кладем в prm_2, prm_11
            //sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_11 set is_actual=100 where user_del =" + finder.nzp_file;

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_11" + " set dat_po =" +

                        " '" + finder.dat_po + "'" + sConvToDate + " - interval '1 day'  where dat_po>  " +
                " '" + finder.dat_po + "'" + sConvToDate + " and exists " +
                " (select 1 " +
              " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c," +
                " " + finder.bank + "_kernel" +tableDelimiter + "supplier b " +
                " WHERE c.nzp_supp =b.nzp_supp " +
                " AND c.nzp_frm IN" +
                    " (SELECT nzp_frm" +
                    " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                    " WHERE nzp_prm_tarif_su>0 ) " +
                " AND c.tarif>0   " +
                " and " + finder.bank + "_data" + tableDelimiter + "prm_11.nzp=b.nzp_supp " +
                " and " + finder.bank + "_data" + tableDelimiter + "prm_11.nzp_prm= " +
                " (SELECT nzp_prm_tarif_su" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                " WHERE a.nzp_frm=c.nzp_frm))  and is_actual=1 "+
                "  and dat_s > '" + finder.dat_po + "'" + sConvToDate + "" +
                " and nzp in (select nzp_supp from " 
                + finder.bank + "_data" + tableDelimiter + "tarif c where c.nzp_kvar"+
                " in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file =" + finder.nzp_file + "))" 
                ;
            

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            //
            // надо посмотреть все лс и услуги с данным поставщиком и если тариф 1 то только тогда добавлять
            //


            sql =
                "INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_11" +
                " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual,  nzp_user, dat_when, user_del)" +
                "  SELECT b.nzp_supp, " +
                " (SELECT nzp_prm_tarif_su" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                " WHERE a.nzp_frm = c.nzp_frm) ," +
                DBManager.MDY(finder.month, 1, finder.year) + "," +
                " '" + finder.dat_po + "'" + sConvToDate + ", " +
                " MAX(ROUND(c.tarif " + sConvToNum + ",5)) , 1 ," + 
                finder.nzp_user + "," + sCurDate + "," + finder.nzp_file +
                " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c," +
                " " + finder.bank + "_kernel" +tableDelimiter + "supplier b " +
                " WHERE c.nzp_supp =b.nzp_supp " +
                " AND c.nzp_frm IN" +
                    " (SELECT nzp_frm" +
                    " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                    " WHERE nzp_prm_tarif_su>0 ) " +
                " and not exists (select 1 from " + finder.bank + "_data" + tableDelimiter + "prm_11 f "+
                " where f.nzp=b.nzp_supp " + "" +
                " and f.nzp_prm =(SELECT nzp_prm_tarif_su FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a " +
                " WHERE a.nzp_frm=c.nzp_frm) and f.dat_s=" + DBManager.MDY(finder.month, 1, finder.year) + " " +
                " ) "+
                " and c.nzp_kvar in (select nzp_kvar from "+ Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file ="+finder.nzp_file+")" +
                " GROUP BY 1,2,3,4";

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_2" +" set dat_po =" +
                
                " '" + finder.dat_po + "'" + sConvToDate + " - interval '1 day'  where dat_po>  "+
                 " '" + finder.dat_po + "'" + sConvToDate+ " and exists "+
                 " (select 1 "+
                 " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c, " + 
                finder.bank + "_data" + tableDelimiter + "kvar b " +
                " WHERE c.nzp_kvar =b.nzp_kvar" +
                " AND c.nzp_frm in" +
                " (SELECT nzp_frm" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                " WHERE nzp_prm_tarif_dm>0 )" +
                " and c.tarif>0  " +
                " and " + finder.bank + "_data" + tableDelimiter + "prm_2.nzp=b.nzp_dom "+
                " and " + finder.bank + "_data" + tableDelimiter + "prm_2.nzp_prm= "+
                " (SELECT nzp_prm_tarif_dm" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                " WHERE a.nzp_frm=c.nzp_frm) " +
                " and c.nzp_kvar in (select nzp_kvar from "+ Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file ="+finder.nzp_file+")"+
                ")" 
                ;
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            sql =
                "INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_2" +
                " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, " +
                " cur_unl, nzp_wp, nzp_user, dat_when, user_del) " +
                " SELECT  b.nzp_dom," +
                " (SELECT nzp_prm_tarif_dm" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                " WHERE a.nzp_frm=c.nzp_frm) ," +
                DBManager.MDY(finder.month, 1, finder.year) + "," +
                " '" + finder.dat_po + "'" + sConvToDate + ", " +
                "TRUNC(MAX(c.tarif " + sConvToNum + "),5), 1 , 1 , 1 ,"
                + finder.nzp_user + "," + sCurDate + "," + finder.nzp_file + "" +
                " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c, " + 
                finder.bank + "_data" + tableDelimiter + "kvar b " +
                " WHERE c.nzp_kvar =b.nzp_kvar" +
                " AND c.nzp_frm in" +
                " (SELECT nzp_frm" +
                " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                " WHERE nzp_prm_tarif_dm>0 )" +
                " and c.tarif>0  " +
                " and not exists (select 1 from " + finder.bank + "_data" + tableDelimiter + "prm_2 f where f.nzp=b.nzp_dom "+"" +
                " and f.nzp_prm =(SELECT nzp_prm_tarif_dm FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a "+
                " WHERE a.nzp_frm=c.nzp_frm) and f.dat_s="+DBManager.MDY(finder.month, 1, finder.year) + " " +
                " ) "+
                " and c.nzp_kvar in (select nzp_kvar from "+ Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file ="+finder.nzp_file+")" +
                " group by 1,2,3,4";

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);
            #endregion

            #region Тариф на л.с 

            sql = "with tcur1 as (" +
                  " SELECT  b.nzp_dom," +
                  " (SELECT nzp_prm_tarif_ls" +
                  " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                  " WHERE a.nzp_frm=c.nzp_frm) nzp_prm ," +
                  DBManager.MDY(finder.month, 1, finder.year) + "," +
                  " '" + finder.dat_po + "'" + sConvToDate + ", " +
                  "TRUNC(c.tarif " + sConvToNum + ",5) val_prm, 1 is_actual , 1 cur_unl , 1 nzp_wp ,"
                  + finder.nzp_user + " nzp_user," + sCurDate + " dat_when," + finder.nzp_file + " user_del " +
                  " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c, " +
                  finder.bank + "_data" + tableDelimiter + "kvar b " +
                  " WHERE c.nzp_kvar =b.nzp_kvar" +
                  " AND c.nzp_frm in" +
                  " (SELECT nzp_frm" +
                  " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                  " WHERE nzp_prm_tarif_ls>0 )" +
                  " and c.tarif>0  " +
                  " and c.nzp_kvar in (select nzp_kvar from "+ Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file ="+finder.nzp_file+")" +
                  " group by 1,2,3,4,5 ) , tcur2 as (select nzp_dom from tcur1 group by 1 having count(*)>1), " +
                  
                  " tcur3 as (" +

                  " SELECT distinct  b.nzp_kvar nzp," +
                  " (SELECT nzp_prm_tarif_ls" +
                  " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis a" +
                  " WHERE a.nzp_frm=c.nzp_frm) nzp_prm ," +
                  DBManager.MDY(finder.month, 1, finder.year) + " dat_s," +
                  " '" + finder.dat_po + "'" + sConvToDate + " dat_po, " +
                  "TRUNC(c.tarif " + sConvToNum + ",5) val_prm, 1 is_actual , 1 cur_unl , 1 nzp_wp ,"
                  + finder.nzp_user + " nzp_user," + sCurDate + " dat_when," + finder.nzp_file + " user_del " +
                  " FROM  " + finder.bank + "_data" + tableDelimiter + "tarif c, " +
                  finder.bank + "_data" + tableDelimiter + "kvar b " +
                  " WHERE c.nzp_kvar =b.nzp_kvar" +
                  " and c.nzp_kvar in (select nzp_kvar from "+ Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file ="+finder.nzp_file+")" +
                  " AND c.nzp_frm in" +
                  " (SELECT nzp_frm" +
                  " FROM " + finder.bank + "_kernel" + tableDelimiter + "formuls_opis" +
                  " WHERE nzp_prm_tarif_ls>0 )" +
                  " and c.tarif>0  " +
                  " and b.nzp_dom in (select nzp_dom from tcur2) " +
                  ")" +
                  " , tcur4 as (update " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " set is_actual=100 where exists (select 1 from tcur3 where tcur3.nzp_prm=" + finder.bank + "_data" + tableDelimiter + "prm_1" +".nzp_prm"+
                  "  and tcur3.nzp=" + finder.bank + "_data" + tableDelimiter + "prm_1" +".nzp and "+
                   finder.bank + "_data" + tableDelimiter + "prm_1" +".is_actual=1 and "+
                   finder.bank + "_data" + tableDelimiter + "prm_1" +".dat_s<=tcur3.dat_po and "+
                   finder.bank + "_data" + tableDelimiter + "prm_1" +".dat_po>=tcur3.dat_s) "+
                  ")" +
                  "INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, " +
                  " cur_unl, nzp_wp, nzp_user, dat_when, user_del) "+
                  " select nzp ,  nzp_prm, dat_s, dat_po,max( val_prm), is_actual,  cur_unl, nzp_wp, nzp_user, dat_when, user_del  from tcur3  " +
                  "  where not exists (select 1 from "+finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " f where f.nzp=tcur3.nzp and f.nzp_prm =tcur3.nzp_prm and f.dat_s =tcur3.dat_s and f.is_actual =1 )  group by 1,2,3,4,6,7,8,9,10,11  ";
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);
            #endregion



            sql =
                "update " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +finder.month.ToString("00") +
                " set ( nzp_supp )=(" +
                " (select max( nzp_supp ) from " + finder.bank + "_data" + tableDelimiter + "tarif a where a.nzp_kvar =" +
                finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                ".nzp_kvar and " +
                " a.nzp_serv =" + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                finder.month.ToString("00") + ".nzp_serv))" +
                " where nzp_supp is null "+
                " and "+ finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +finder.month.ToString("00") +
                ".nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar where nzp_file =" + finder.nzp_file + ")" 
                ;
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            sql =
                "INSERT INTO  " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "perekidka " +
                " ( nzp_kvar, num_ls, nzp_serv, nzp_supp," +
                " type_rcl, date_rcl, tarif, volum, sum_rcl," +
                " month_, comment, nzp_user, nzp_reestr)" +
                " SELECT distinct nzp_kvar, num_ls, nzp_serv, nzp_supp, -1," +
                DBManager.MDY(finder.month, 1, finder.year) + "," +
                " tarif, 0 c_calc , real_charge as sum_rcl, "
                + finder.month.ToString("00") + ", 'перекидка ' , " + finder.nzp_user + ", " + finder.nzp_file +
                " FROM " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                finder.month.ToString("00") +
                " WHERE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                finder.month.ToString("00") + ".real_charge > 0 AND " +
                finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                ".order_print = " + finder.nzp_file;

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);


            sql =
                "INSERT INTO  " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "perekidka " +
                " ( nzp_kvar, num_ls, nzp_serv, nzp_supp," +
                " type_rcl, date_rcl, tarif, volum, sum_rcl," +
                " month_, comment, nzp_user, nzp_reestr)" +
                " SELECT  nzp_kvar, num_ls, nzp_serv, nzp_supp, 21," +
                DBManager.MDY(finder.month, 1, finder.year) + "," +
                " tarif, 0 c_calc , sum_subsidy as sum_rcl, "
                + finder.month.ToString("00") + ", 'перекидка ' , " + finder.nzp_user + ", " + finder.nzp_file +
                " FROM " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                finder.month.ToString("00") +
                " WHERE " + finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" +
                finder.month.ToString("00") + ".sum_subsidy > 0 AND " +
                finder.bank + "_charge_" + (finder.year - 2000).ToString() + tableDelimiter + "charge_" + finder.month.ToString("00") +
                ".order_print = " + finder.nzp_file;

            DBManager.ExecSQL(conDb, null, sql, true, commandTime);
   
            #region Загрузка параметра для тарифа
            // 
            /*
            sql = "insert into  " + finder.bank + DBManager.sKernelAliasRest + "supplier(nzp_supp,name_supp) select nzp_supp,name_supp from " +
                        Points.Pref + DBManager.sKernelAliasRest + "supplier a where a.nzp_supp not in (select nzp_supp from " + finder.bank + DBManager.sKernelAliasRest + "supplier) ";


            //ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            sql = "SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv_tuning a, " + 
                finder.bank + DBManager.sKernelAliasRest + "formuls_opis b" +
                  " WHERE a.nzp_frm =b.nzp_frm";

            DataTable dtserv = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dtserv.Rows)
            {
                //     string psql = "update " + finder.bank + "_charge_"+ (finder.year - 2000).ToString() +":charge_"+ finder.month.ToString("00") +" set nzp_frm="+
                //                 " (установить  тяз_акь )
            }

            */
            #endregion Загрузка параметра для тарифа


            MonitorLog.WriteLog("Старт формирование фиктивной пачки ", MonitorLog.typelog.Info, 1, 2, true);


            //изменение статуса загрузки
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Формирование пачки' where nzp_file = " + finder.nzp_file;
            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);

            #region Формирование фиктивной пачки 

            
            //sql = " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats a " +
            //      " WHERE a.nzp_file =" + finder.nzp_file;

            //DataTable dt1 = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            //if (dt1.Rows.Count == 0)
            //{
            //    var db = new DBMakePacks(conDb);

            //    if (
            //        !db.MakePacks(new FilesImported()
            //        {
            //            bank = finder.bank,
            //            nzp_user = finder.nzp_user,
            //            format_version = finder.versionFull
            //        }).result)
            //    {
            //        MonitorLog.WriteLog("Ошибка формирования фиктивной пачки", MonitorLog.typelog.Error, true);
            //        //disLog.Append("Ошибка формирования фиктивной пачки");
            //    }
            //    else
            //    {
            //        //скидываем в архив файл с пачками
            //    }
            //}

            #endregion

            #region копируем из charge_XX в charge_XX_t

            //создаем таблицу
            try
            {

#if PG
                sql = " SET search_path TO  '" + finder.bank + "_charge_" + (finder.year - 2000).ToString() + "'";
#else
            sql = "DATABASE " +  finder.bank + "_charge_" + (finder.year - 2000).ToString();
#endif
                
                DBManager.ExecSQL(conDb, null, sql, true, commandTime);

                //проверка на существование колонки reval_tarif в charge_XX и charge_XX_t
                
                sql = " CREATE TABLE charge_" + finder.month.ToString("00") + "_t (" +
                      " nzp_charge SERIAL NOT NULL," +
                      " nzp_kvar INTEGER," +
                      " num_ls INTEGER," +
                      " nzp_serv INTEGER," +
                      " nzp_supp INTEGER," +
                      " nzp_frm INTEGER," +
                      " dat_charge DATE," +
                      " tarif " + sDecimalType + "(14,3)," +
                      " tarif_p " + sDecimalType + "(14,3)," +
                      " rsum_tarif " + sDecimalType + "(14,2)," +
                      " rsum_lgota " + sDecimalType + "(14,2)," +
                      " sum_tarif " + sDecimalType + "(14,2)," +
                      " sum_dlt_tarif " + sDecimalType + "(14,2)," +
                      " sum_dlt_tarif_p " + sDecimalType + "(14,2)," +
                      " sum_tarif_p " + sDecimalType + "(14,2)," +
                      " sum_lgota " + sDecimalType + "(14,2)," +
                      " sum_dlt_lgota " + sDecimalType + "(14,2)," +
                      " sum_dlt_lgota_p " + sDecimalType + "(14,2)," +
                      " sum_lgota_p " + sDecimalType + "(14,2)," +
                      " sum_nedop " + sDecimalType + "(14,2)," +
                      " sum_nedop_p " + sDecimalType + "(14,2)," +
                      " sum_real " + sDecimalType + "(14,2)," +
                      " sum_charge " + sDecimalType + "(14,2)," +
                      " reval " + sDecimalType + "(14,2)," +
                      " real_pere " + sDecimalType + "(14,2)," +
                      " sum_pere " + sDecimalType + "(14,2)," +
                      " real_charge " + sDecimalType + "(14,2)," +
                      " sum_money " + sDecimalType + "(14,2)," +
                      " money_to " + sDecimalType + "(14,2)," +
                      " money_from " + sDecimalType + "(14,2)," +
                      " money_del " + sDecimalType + "(14,2)," +
                      " sum_fakt " + sDecimalType + "(14,2)," +
                      " fakt_to " + sDecimalType + "(14,2)," +
                      " fakt_from " + sDecimalType + "(14,2)," +
                      " fakt_del " + sDecimalType + "(14,2)," +
                      " sum_insaldo " + sDecimalType + "(14,2)," +
                      " izm_saldo " + sDecimalType + "(14,2)," +
                      " sum_outsaldo " + sDecimalType + "(14,2)," +
                      " isblocked INTEGER," +
                      " is_device INTEGER default 0," +
                      " c_calc " + sDecimalType + "(14,2)," +
                      " c_sn " + sDecimalType + "(14,2)," +
                      " c_okaz " + sDecimalType + "(14,2)," +
                      " c_nedop " + sDecimalType + "(14,2)," +
                      " isdel INTEGER," +
                      " c_reval " + sDecimalType + "(14,2)," +
                      " reval_tarif " + sDecimalType + "(14,2)," +
                      " reval_lgota " + sDecimalType + "(14,2)," +
                      " tarif_f " + sDecimalType + "(14,3)," +
                      " sum_tarif_eot " + sDecimalType + "(14,2)," +
                      " sum_tarif_sn_eot " + sDecimalType + "(14,2)," +
                      " sum_tarif_sn_f " + sDecimalType + "(14,2)," +
                      " rsum_subsidy " + sDecimalType + "(14,2)," +
                      " sum_subsidy " + sDecimalType + "(14,2)," +
                      " sum_subsidy_p " + sDecimalType + "(14,2)," +
                      " sum_subsidy_reval " + sDecimalType + "(14,2)," +
                      " sum_subsidy_all " + sDecimalType + "(14,2)," +
                      " sum_lgota_eot " + sDecimalType + "(14,2)," +
                      " sum_lgota_f " + sDecimalType + "(14,2)," +
                      " sum_smo " + sDecimalType + "(14,2)," +
                      " tarif_f_p " + sDecimalType + "(14,3)," +
                      " sum_tarif_eot_p " + sDecimalType + "(14,2)," +
                      " sum_tarif_sn_eot_p " + sDecimalType + "(14,2)," +
                      " sum_tarif_sn_f_p " + sDecimalType + "(14,2)," +
                      " sum_lgota_eot_p " + sDecimalType + "(14,2)," +
                      " sum_lgota_f_p " + sDecimalType + "(14,2)," +
                      " sum_smo_p " + sDecimalType + "(14,2)," +
                      " order_print INTEGER," +
                      " sum_tarif_f " + sDecimalType + "(14,2)," +
                      " sum_tarif_f_p " + sDecimalType + "(14,2)," +
                      " gsum_tarif " + sDecimalType + "(14,2)) ";
                DBManager.ExecSQL(conDb, null, sql, false, commandTime);

                #region Проверка на существование колонок в таблице charge_XX_t
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00"), "reval_tarif"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + " add column reval_tarif "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "reval_tarif"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column reval_tarif "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00"), "reval_lgota"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + " add column reval_lgota "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "reval_lgota"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column reval_lgota "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00"), "sum_tarif_eot"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + " add column sum_tarif_eot "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_tarif_eot"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_tarif_eot "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_tarif_eot_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_tarif_eot_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "rsum_subsidy"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column rsum_subsidy "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_lgota_eot"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_lgota_eot "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_lgota_eot_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_lgota_eot_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_lgota_f"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_lgota_f "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_lgota_f_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_lgota_f_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_lgota_f_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_lgota_f_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_smo"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_smo "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_smo_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_smo_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_tarif_f_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_tarif_f_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                if (!DBManager.TempColumnInWebCashe(conDb, "charge_" + finder.month.ToString("00") + "_t", "sum_tarif_sn_eot_p"))
                {
                    sql = "alter table charge_" + finder.month.ToString("00") + "_t add column sum_tarif_sn_eot_p "
                          + DBManager.sDecimalType + "(14, 2) DEFAULT 0;";
                    DBManager.ExecSQL(conDb, null, sql, true, commandTime);
                }
                #endregion
            }
            catch { }

            MonitorLog.WriteLog("Старт чистки из charge_" + finder.month.ToString("00") + "_t по файлу" + finder.nzp_file, MonitorLog.typelog.Info, true);
            //чистим из charge_XX_t по этому файлу
            sql = " DELETE FROM " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                 tableDelimiter + "charge_" + finder.month.ToString("00") + "_t " +
                 " WHERE order_print = " + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);

            MonitorLog.WriteLog("Старт добавления в charge_" + finder.month.ToString("00") + "_t по файлу " + finder.nzp_file, MonitorLog.typelog.Info, true);
            //добавляем в charge_XX_t  
            sql = "INSERT INTO " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                  tableDelimiter + "charge_" + finder.month.ToString("00") + "_t " +
                  "( nzp_kvar, num_ls, nzp_serv," +
                  " nzp_supp, nzp_frm, dat_charge," +
                  " tarif, tarif_p, rsum_tarif," +
                  " gsum_tarif, rsum_lgota, sum_tarif," +
                  "  sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p," +
                  " sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p," +
                  " sum_lgota_p, sum_nedop, sum_nedop_p, " +
                  " sum_real, sum_charge, reval," +
                  " real_pere, sum_pere, real_charge," +
                  " sum_money, money_to, money_from," +
                  " money_del, sum_fakt, fakt_to," +
                  " fakt_from, fakt_del, sum_insaldo," +
                  " izm_saldo, sum_outsaldo, isblocked," +
                  " is_device, c_calc, c_sn," +
                  " c_okaz, c_nedop, isdel," +
                  " c_reval, reval_tarif, reval_lgota," +
                  " tarif_f, sum_tarif_eot, sum_tarif_sn_eot," +
                  " sum_tarif_sn_f, rsum_subsidy, sum_subsidy," +
                  " sum_subsidy_p,  sum_subsidy_reval, sum_subsidy_all," +
                  " sum_lgota_eot, sum_lgota_f, sum_smo," +
                  " tarif_f_p, sum_tarif_eot_p, sum_tarif_sn_eot_p," +
                  " sum_tarif_sn_f_p, sum_lgota_eot_p, sum_lgota_f_p," +
                  " sum_smo_p, sum_tarif_f, sum_tarif_f_p, " +
                  " order_print  )" +

                  " SELECT" +
                  " nzp_kvar, num_ls, nzp_serv," +
                  " nzp_supp, nzp_frm, dat_charge," +
                  " tarif, tarif_p, rsum_tarif," +
                  " gsum_tarif, rsum_lgota, sum_tarif," +
                  "  sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p," +
                  " sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p," +
                  " sum_lgota_p, sum_nedop, sum_nedop_p, " +
                  " sum_real, sum_charge, reval," +
                  " real_pere, sum_pere, real_charge," +
                  " sum_money, money_to, money_from," +
                  " money_del, sum_fakt, fakt_to," +
                  " fakt_from, fakt_del, sum_insaldo," +
                  " izm_saldo, sum_outsaldo, isblocked," +
                  " is_device, c_calc, c_sn," +
                  " c_okaz, c_nedop, isdel," +
                  " c_reval, reval_tarif, reval_lgota," +
                  " tarif_f, sum_tarif_eot, sum_tarif_sn_eot," +
                  " sum_tarif_sn_f, rsum_subsidy, sum_subsidy," +
                  " sum_subsidy_p,  sum_subsidy_reval, sum_subsidy_all," +
                  " sum_lgota_eot, sum_lgota_f, sum_smo," +
                  " tarif_f_p, sum_tarif_eot_p, sum_tarif_sn_eot_p," +
                  " sum_tarif_sn_f_p, sum_lgota_eot_p, sum_lgota_f_p," +
                  " sum_smo_p, sum_tarif_f, sum_tarif_f_p, " +
                  " order_print " +
                  " FROM  " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                  tableDelimiter + "charge_" + finder.month.ToString("00") +
                  " WHERE order_print = " + finder.nzp_file;
            DBManager.ExecSQL(conDb, null, sql, true, commandTime);
            #endregion

            #region Проверка двойников

            #region  таблица CHARGE_XX 
            sql = " SELECT nzp_kvar, nzp_serv , nzp_supp, count(*) " +
                  " FROM " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                  tableDelimiter + "charge_" + finder.month.ToString("00") +
                  " GROUP BY 1,2,3" +
                  " HAVING count(*)>1;";
            DataTable dtdoubleCharge = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtdoubleCharge.Rows.Count > 0)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры DisassembleFile, InsertDateFromFile : имеются дубли в таблице " +
                    finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                    tableDelimiter + "charge_" + finder.month.ToString("00") + " в количестве " +
                    dtdoubleCharge.Rows.Count,
                    MonitorLog.typelog.Error, true);
            }

            #endregion

            #region таблица TARIF
            sql = " SELECT nzp_kvar , nzp_serv , nzp_supp , dat_s, count(*) " +
                  " FROM " + finder.bank + "_data" + tableDelimiter + "tarif " +
                  " WHERE is_actual <> 100" +
                  " GROUP BY 1,2,3, 4" +
                  " HAVING count(*)>1;";
            DataTable dtdoubleTarif = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtdoubleTarif.Rows.Count > 0)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры DisassembleFile, InsertDateFromFile : имеются дубли в таблице " +
                    finder.bank + "_data" + tableDelimiter + "tarif " + " в количестве " + dtdoubleTarif.Rows.Count,
                    MonitorLog.typelog.Error, true);

                MonitorLog.WriteLog("Ошибка выполнения процедуры DisassembleFile, InsertDateFromFile : имеются дубли в таблице " +
                    finder.bank + "_data" + tableDelimiter + "tarif " + " в количестве " + dtdoubleTarif.Rows.Count, MonitorLog.typelog.Error, true);
            }
            #endregion

            #endregion

            
            #region Раппортуем что все хорошо загрузилось
            return new ReturnsType(true, "Данные сохранены ", 1);
            #endregion Раппортуем что все хорошо загрузилось
        }
    }
}
