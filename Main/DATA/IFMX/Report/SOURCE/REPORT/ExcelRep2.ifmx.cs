using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;

using STCLINE.KP50.Interfaces;
using Globals.SOURCE.Utility;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;
using STCLINE.KP50.Utility.ArchiveProviders;
using STCLINE.KP50.IFMX.Report.SOURCE.REPORT.BankUnload;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {


        public void GetSumNedopVinovnik(IDbConnection con_db, int year_, int month_,
            int nzp_serv, string selTable, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDataReader reader;
            IDataReader reader2;
            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();

            try
            {

                ExecSQL(con_db, " Drop table t_nedop_sum ", false);
                #region Получение списка префиксов

                ExecRead(con_db, out reader, "select pref from " + selTable + " group by 1 ", true);

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                reader.Close();

                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе

                ret = ExecSQL(con_db, " Drop table t_sprav_otkl_usl; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create temp table t_sprav_otkl_usl (" +
                             " nzp_dom           INTEGER, " +
                             " nzp_kvar          INTEGER, " +
                             " nzp_supp          INTEGER, " +
                             " nzp_vinovnik      INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " dat_month         DATE, " +
                             " countKvar         INTEGER, " +
                             " count_daynedo     NUMERIC(14,2), " +
                             " count_day_all     NUMERIC(14,2), " +
                             " count_kvarchas    INTEGER DEFAULT 0, " +
                             " col_gil           INTEGER, " +
                             " sum_nedop         NUMERIC(14,2), " +
                             " sum_nedop_all     NUMERIC(14,2) " +
                             " )   "
                          );
#else
                sql.Append(" create temp table t_sprav_otkl_usl (" +
                             " nzp_dom           INTEGER, " +
                             " nzp_kvar          INTEGER, " +
                             " nzp_supp          INTEGER, " +
                             " nzp_vinovnik      INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " dat_month         DATE, " +
                             " countKvar         INTEGER, " +
                             " count_daynedo     DECIMAL(14,2), " +
                             " count_day_all     DECIMAL(14,2), " +
                             " count_kvarchas    INTEGER DEFAULT 0, " +
                             " col_gil           INTEGER, " +
                             " sum_nedop         DECIMAL(14,2), " +
                             " sum_nedop_all     DECIMAL(14,2) " +
                             " ) With no log; "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                    return;
                }

                string last_day_m = year_.ToString() + "-" + month_.ToString("00") + "-" + System.DateTime.DaysInMonth(year_, month_) + " 23:59";
                string first_day_m = year_.ToString() + "-" + month_.ToString("00") + "-01 00:00";

                foreach (string pref in prefix)
                {


                    sql.Remove(0, sql.Length);

                    string dat_month = "01." + month_.ToString("00") + "." + year_;

                    //количество дней в месяце
                    int dayInMonth = DateTime.DaysInMonth(year_, month_);
                    string dbcharge = pref + "_charge_" + year_.ToString().Substring(year_.ToString().Length - 2);

                    #region Выборка текущих начислений недопоставки
                    sql.Append(
                               " select  s.nzp_dom, s.nzp_kvar, nzp_supp, nzp_serv, Date('01." + month_.ToString("00") +
                               "." + year_.ToString() + "') as dat_month,  " +
                               " date('01." + month_.ToString("00") +
                               "." + year_.ToString() + "') as dat_month_end,  " +
#if PG
 " sum(c.sum_nedop) as sum_nedop  into temp t_sum_nedo " +
                               " from " + selTable + " s , " +
                               " " + dbcharge + ".charge_" + month_.ToString("00") + " c " +
#else
 " sum(c.sum_nedop) as sum_nedop " +
                               " from " + selTable + " s , " +
                               " " + dbcharge + ":charge_" + month_.ToString("00") + " c " +
#endif
 " where s.nzp_kvar = c.nzp_kvar " +
                               " and  c.nzp_serv > 1  " +
                               " and dat_charge is null " +
                               "  and  c.sum_nedop > 0.001 ");
                    //добавляем фильтр
                    if (nzp_serv > 0)
                    {
                        sql.Append(" and c.nzp_serv = " + nzp_serv + " ");
                    }
#if PG
                    sql.Append(" group by 1,2,3,4,5  ");
#else
                    sql.Append(" group by 1,2,3,4,5 into temp t_sum_nedo with no log ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }
                    #endregion


                    #region Выборка перерасчетов прошлого периода
                    sql.Remove(0, sql.Length);
                    sql.Append(" select month_, year_ ");
#if PG
                    sql.Append(" from " + dbcharge + ".lnk_charge_" + month_.ToString("00") + " b, " + selTable + " d ");
#else
                    sql.Append(" from " + dbcharge + ":lnk_charge_" + month_.ToString("00") + " b, " + selTable + " d ");
#endif
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" group by 1,2");
                    if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        ret.result = false;
                        return;
                    }
                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_sum_nedo (nzp_dom,nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" dat_month, dat_month_end, sum_nedop) ");
                        sql.Append(" select d.nzp_dom, d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" sum(sum_nedop-sum_nedop_p)  ");
#if PG
                        sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#else
                        sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#endif
                        sql.Append(" b, " + selTable + " d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(month_.ToString("00") + "." + year_.ToString() + "') and abs(sum_nedop-sum_nedop_p)>0.001");
                        //добавляем фильтр
                        if (nzp_serv > 0)
                        {
                            sql.Append(" and nzp_serv = " + nzp_serv + " ");
                        }
                        sql.Append(" group by 1,2,3, 4,5,6");
                        if (!ExecSQL(con_db, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            ret.result = false;
                            return;
                        }

                    }
                    reader2.Close();


                    #endregion



#if PG
                    ExecSQL(con_db, "update t_sum_nedo set dat_month_end =  date(dat_month_end) + interval '1 month' - interval '1 day' ", true);
                    ExecSQL(con_db, "create index ix_tmp753 on t_sum_nedo(nzp_kvar, nzp_serv) ", true);
                    ExecSQL(con_db, "analyze t_sum_nedo ", true);
#else
                    ExecSQL(con_db, "update t_sum_nedo set dat_month_end =  date(dat_month_end) + 1 units month - 1 units day ", true);
                    ExecSQL(con_db, "create index ix_tmp753 on t_sum_nedo(nzp_kvar, nzp_serv) ", true);
                    ExecSQL(con_db, "update statistics for table t_sum_nedo ", true);
#endif

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(
                             " select  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, " +
                             " MDY(date_part('month',dat_s)::int,01,date_part('year',dat_s)::int) as dat_month," +
                             " round((((date_part('day',dat_po-dat_s)*24)+date_part('hour',dat_po-dat_s))*60+" +
                             " date_part('minute',dat_po-dat_s))::numeric/1440,2) as day_nedo " +
                             " into temp  t_vinovnik  from   " + pref + "_data.nedop_kvar a, " + selTable + " d " +
                             " where a.nzp_kvar=d.nzp_kvar " +
                             " and a.month_calc = date('" + dat_month + "') ");

#else
                    sql.Append(
                             " select  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, " +
                             " MDY(month(dat_s),01,year(dat_s)) as dat_month," +
                             " Round(((cast( dat_po  - dat_s " +
                             "  as interval minute(6) to minute)||'')+0)/1440,2) as day_nedo " +
                             " from   " + pref + "_data:nedop_kvar a, " + selTable + " d " +
                             " where a.nzp_kvar=d.nzp_kvar " +
                             " and a.month_calc = date('" + dat_month + "') ");
                    sql.Append(" into temp t_vinovnik with no log ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_vinovnik : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }

                    //Вычисляем общее число дней недопоставки
                    sql.Remove(0, sql.Length);
                    sql.Append(
                             " select  nzp_kvar,nzp_serv, dat_month," +
                             " sum(day_nedo) as count_daynedo " +
#if PG
 " into temp t_alldaynedo from  t_vinovnik group by 1,2,3 ");
#else
 " from  t_vinovnik group by 1,2,3 into temp t_alldaynedo with no log");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);




                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_sprav_otkl_usl (nzp_dom, nzp_kvar, nzp_supp, nzp_vinovnik, nzp_serv,  " +
                               " count_daynedo, sum_nedop, sum_nedop_all, count_day_all, dat_month) " +
                               " select nzp_dom, a.nzp_kvar, nzp_supp, vinovnik, a.nzp_serv, day_nedo,  " +
                               " (case when count_daynedo>0 then sum_nedop*day_nedo/count_daynedo else 0 end), sum_nedop, " +
                               " count_daynedo, a.dat_month  " +
                               " from t_sum_nedo a, t_vinovnik b, t_alldaynedo d" +
                               " where a.nzp_kvar=b.nzp_kvar and a.nzp_kvar=d.nzp_kvar and a.nzp_serv=d.nzp_serv " +
                               " and a.dat_month=d.dat_month and a.dat_month=b.dat_month " +
                               " and a.nzp_serv=b.nzp_serv ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }


                    ExecSQL(con_db, "drop index ix_tmp756 ", false);
                    ExecSQL(con_db, "create index ix_tmp756 on t_sprav_otkl_usl(nzp_kvar) ", true);
#if PG
                    ExecSQL(con_db, "analyze t_sprav_otkl_usl ", true);
#else
                    ExecSQL(con_db, "update statistics for table t_sprav_otkl_usl ", true);
#endif


                    //вставка количества жильцов
                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" update  t_sprav_otkl_usl set col_gil = (   " +
                                " select  max(p.val_prm)::int  " +
                                " from  " + pref + "_data. prm_1 p " +
                                " where t_sprav_otkl_usl.nzp_kvar = p.nzp  " +
                                " and p.nzp_prm = 5  " +
                                " and p.is_actual = 1  " +
                                " and p.dat_s <= '" + "01." + month_.ToString("00") + "." + year_.ToString() + "' " +
                                " and p. dat_po >= '" + "01." + month_.ToString("00") + "." + year_.ToString() + "' ) ");
#else
                    sql.Append(" update  t_sprav_otkl_usl set col_gil = (   " +
                                " select  max(p.val_prm)  " +
                                " from  " + pref + "_data: prm_1 p " +
                                " where t_sprav_otkl_usl.nzp_kvar = p.nzp  " +
                                " and p.nzp_prm = 5  " +
                                " and p.is_actual = 1  " +
                                " and p.dat_s <= '" + "01." + month_.ToString("00") + "." + year_.ToString() + "' " +
                                " and p. dat_po >= '" + "01." + month_.ToString("00") + "." + year_.ToString() + "' ) ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select nzp_kvar, nzp_serv, dat_month, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                               " max(count_day_all)-sum(count_daynedo) as day_nedo " +
                               "  into temp  t_corr from t_sprav_otkl_usl " +
                               " group by 1,2,3 " +
                               "  ");
#else
                    sql.Append(" select nzp_kvar, nzp_serv, dat_month, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                               " max(count_day_all)-sum(count_daynedo) as day_nedo " +
                               " from t_sprav_otkl_usl " +
                               " group by 1,2,3 " +
                               " into temp t_corr with no log    ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_corr : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" update t_sprav_otkl_usl set sum_nedop = sum_nedop+ coalesce((select sum(sum_nedop) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month),0), " +
                               " count_daynedo = count_daynedo + coalesce((select sum(day_nedo) " +
#else
                    sql.Append(" update t_sprav_otkl_usl set sum_nedop = sum_nedop+ nvl((select sum(sum_nedop) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month),0), " +
                               " count_daynedo = count_daynedo + nvl((select sum(day_nedo) " +
#endif
 " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv),0) " +
                               " where sum_nedop>0 " +
                               " and 1=(select 1 from t_corr " +
                               " where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month " +
                               " and t_sprav_otkl_usl.nzp_vinovnik=t_corr.max_vin)  ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_corr : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }

                    ret = ExecSQL(con_db, "drop table t_corr", true);
                    ret = ExecSQL(con_db, "drop table t_sum_nedo", true);
                    ret = ExecSQL(con_db, "drop table t_vinovnik", true);
                    ret = ExecSQL(con_db, "drop table t_alldaynedo", false);

                }
                #endregion

                #region Выборка из  sprav_otkl_usl


                sql.Remove(0, sql.Length);
                sql.Append(" select nzp_dom, nzp_supp, nzp_vinovnik,  ");
                sql.Append(" count(distinct nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
#if PG
                sql.Append(" into temp t_nedop_sum from  t_sprav_otkl_usl a ");
                sql.Append(" group by 1,2,3 ");
#else
                sql.Append(" from  t_sprav_otkl_usl a ");
                sql.Append(" group by 1,2,3 ");
                sql.Append(" into temp t_nedop_sum with no log");
#endif

                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return;
                }



                return;

                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return;
            }
        }






        /// <summary>
        /// Процедура формирования отчета РСО по холодной воде для Самары
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        public DataTable GetSpravSoderg2HvWater(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            IDataReader reader;
            IDataReader reader2;

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();


            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();


#if PG
            string tXX_spls = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif

            conn_web.Close();

            ExecSQL(conn_db, "drop table sel_kvar_water", false);
            sql.Remove(0, sql.Length);
            //sql.Append(" select nzp_dom,nzp_kvar,pref from " + tXX_spls + " where sostls='открыт' into temp sel_kvar_water with no log ");
#if PG
            sql.Append(" select nzp_dom,nzp_kvar,pref into temp sel_kvar_water from " + tXX_spls);
#else
            sql.Append(" select nzp_dom,nzp_kvar,pref from " + tXX_spls + " into temp sel_kvar_water with no log ");
#endif
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "drop table t_svod_water", false);

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" create temp table t_svod_water( " +
                  " nzp_dom integer, " +
                  " nzp_dom_base integer, " +
                  " count_gil_ipu integer, " + //количество граждан в ЛС с приборами учета
                  " count_gil_npu integer, " +//количество граждан в ЛС без приборов учета
                  " volume_all_kub numeric(14,4) , " + //объем в кубометрах по дому
                  " vol_ipu_kub numeric(14,4), " + //Объем по ЛС с ИПУ в куб.м.
                  " vol_npu_kub numeric(14,4), " +//Объем по ЛС без ИПУ в куб.м.
                  " vol_odn_kub numeric(14,4), " +//объем ОДН в куб.м.
                  " rsum_tarif  numeric(14,4), " +//начислено по тарифу по основной услуге
                  " rsum_tarif_odn  numeric(14,4), " +//начислено по тарифу по ОДН
                  " vozv_kub numeric(14,4), " + //Недопоставки в куб.м.
                  " vozv_odn_kub numeric(14,4), " +//Возвраты по недопоставкам по ОДН в куб.м.
                  " sum_nedop  numeric(14,4) default 0, " +//Сумма недопоставки
                  " sum_nedop_odn  numeric(14,4), " +//Сумма недопоставки по ОДН
                  " reval_kub numeric(14,4), " + //объем перерасчета в куб.м.
                  " reval  numeric(14,4), " + //Сумма перерасчета
                  " vol_charge_kub numeric(14,4), " +//Объем начислено к оплате в куб.м.
                  " sum_charge  numeric(14,4), " + //Сумма начислено к оплате
                  " tarif_kub numeric(14,4) default 0, " +//Тариф на дом на Куб.
                  " odpu_kub numeric(14,4) default 0, " + //Объем предъявленный жильцам в куб. 
                  " koef_gv numeric(14,4) default 0, " +
                  " norma numeric(14,4))");
#else
            sql.Append(" create temp table t_svod_water( " +
                  " nzp_dom integer, " +
                  " nzp_dom_base integer, " +
                  " count_gil_ipu integer, " + //количество граждан в ЛС с приборами учета
                  " count_gil_npu integer, " +//количество граждан в ЛС без приборов учета
                  " volume_all_kub Decimal(14,4) , " + //объем в кубометрах по дому
                  " vol_ipu_kub Decimal(14,4), " + //Объем по ЛС с ИПУ в куб.м.
                  " vol_npu_kub Decimal(14,4), " +//Объем по ЛС без ИПУ в куб.м.
                  " vol_odn_kub Decimal(14,4), " +//объем ОДН в куб.м.
                  " rsum_tarif  Decimal(14,4), " +//начислено по тарифу по основной услуге
                  " rsum_tarif_odn  Decimal(14,4), " +//начислено по тарифу по ОДН
                  " vozv_kub Decimal(14,4), " + //Недопоставки в куб.м.
                  " vozv_odn_kub Decimal(14,4), " +//Возвраты по недопоставкам по ОДН в куб.м.
                  " sum_nedop  Decimal(14,4) default 0, " +//Сумма недопоставки
                  " sum_nedop_odn  Decimal(14,4), " +//Сумма недопоставки по ОДН
                  " reval_kub Decimal(14,4), " + //объем перерасчета в куб.м.
                  " reval  Decimal(14,4), " + //Сумма перерасчета
                  " vol_charge_kub Decimal(14,4), " +//Объем начислено к оплате в куб.м.
                  " sum_charge  Decimal(14,4), " + //Сумма начислено к оплате
                  " tarif_kub Decimal(14,4) default 0, " +//Тариф на дом на Куб.
                  " odpu_kub Decimal(14,4) default 0, " + //Объем предъявленный жильцам в куб. 
                  " koef_gv Decimal(14,4) default 0, " +
                  " norma decimal(14,4)) with no log ");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                return null;

            ExecSQL(conn_db, "create index ix_tmpww_01 on t_svod_water(nzp_dom)", true);


            sql.Remove(0, sql.Length);
            sql.Append(" select pref from sel_kvar_water group by 1 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            while (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string sChargeAlias = pref + "_charge_" + prm.year_.ToString().Substring(2, 2);
                string sChargeTable = "charge_" + prm.month_.ToString("00");


                ExecSQL(conn_db, "drop table t_local_water", false);
#if PG
                ExecSQL(conn_db, "select * into temp t_local_water from t_svod_water where nzp_dom=-1", true);
#else
                ExecSQL(conn_db, "select * from t_svod_water where nzp_dom=-1 into temp t_local_water with no log", true);
#endif

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_local_water(nzp_dom) ");
                sql.Append(" select nzp_dom from sel_kvar_water ");
                sql.Append(" where pref = '" + pref + "'");
                if (prm.has_pu == "2")
                    sql.Append(" and 0 < (select count(*)  " +
#if PG
 " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6) ");
                if (prm.has_pu == "3")
                    sql.Append(" and 0 = (select count(*)  " +
                        " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6) ");
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and 0<(select count(*) from " + sChargeAlias + "." + sChargeTable + " a ");
#else
 " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " d " +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6 and sel_kvar_water.nzp_dom=d.nzp_dom) ");
                if (prm.has_pu == "3")
                    sql.Append(" and 0 = (select count(*)  " +
                        " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " d " +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6 and sel_kvar_water.nzp_dom=d.nzp_dom) ");
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and 0<(select count(*) from " + sChargeAlias + ":" + sChargeTable + " a ");
#endif
                    sql.Append(" where a.nzp_kvar=sel_kvar_water.nzp_kvar and ");
                    sql.Append(" dat_charge is null ");
                    sql.Append(" and a.nzp_serv in (6,510) and a.nzp_supp=" + prm.nzp_key + ")");
                    //sql.Append(" and a.nzp_serv in (6) and a.nzp_supp=" + prm.nzp_key + ")");вата
                }

                sql.Append(" group by 1 ");
                ExecSQL(conn_db, sql.ToString(), true);

                /* sql.Remove(0, sql.Length);
                 sql.Append(" update t_local_water set litera = (select max(val_prm) from " + pref + "_data:prm_1 a, ");
                 sql.Append("sel_kvar_water k  where nzp_prm=2002 and k.nzp_dom=t_local_water.nzp_dom  ");
                 sql.Append(" and is_actual=1 and k.nzp_kvar=a.nzp ");
                 sql.Append(" and dat_s<=today and dat_po>=today)");
                 if (!ExecSQL(conn_db, sql.ToString(), true).result)
                     return null;*/


                ExecSQL(conn_db, "drop table t_kvars", false);

                sql.Remove(0, sql.Length);
                sql.Append(" select skw.nzp_kvar , d.nzp_dom, d.norma " +
#if PG
 " into temp t_kvars from t_local_water d, sel_kvar_water skw " +
                           " where d.nzp_dom=skw.nzp_dom  ");
#else
 " from t_local_water d, sel_kvar_water skw " +
                           " where d.nzp_dom=skw.nzp_dom  " +
                           " into temp t_kvars with no log ");
#endif
                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append("  create index ix_t_kvars on t_kvars (nzp_dom , nzp_kvar)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table t_charges", false);

                sql.Remove(0, sql.Length);
                sql.Append(" select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
#if PG
 " coalesce(f.nzp_measure,3) as nzp_measure, a.nzp_frm , " +
                        " a.tarif,coalesce(f.is_device,0) as is_device," +
                        " sum(a.rsum_tarif) as rsum_tarif, " +
                        " sum(a.sum_real) as sum_real, sum(cast (0 as Decimal(14,4))) as c_calc, sum(a.reval+real_charge) as reval, " +
                        " sum(a.sum_nedop) as sum_nedop   , " +
                        " sum(a.sum_charge) as sum_charge " +
                        " into temp t_charges from t_kvars k ," + sChargeAlias + "." + sChargeTable + " a  " +
                        " left outer join " + pref + "_kernel.formuls f on (a.nzp_frm =f.nzp_frm) " +
                        " where k.nzp_kvar =a.nzp_kvar and a.nzp_serv in (6,510) and dat_charge is null ");
                        //" where k.nzp_kvar =a.nzp_kvar and a.nzp_serv in (6) and dat_charge is null ");вата
#else
 " nvl(f.nzp_measure,3) as nzp_measure, a.nzp_frm , " +
                        " a.tarif,nvl(f.is_device,0) as is_device," +
                        " sum(a.rsum_tarif) as rsum_tarif, " +
                        " sum(a.sum_real) as sum_real, sum(cast (0 as Decimal(14,4))) as c_calc, sum(a.reval+real_charge) as reval, " +
                        " sum(a.sum_nedop) as sum_nedop   , " +
                        " sum(a.sum_charge) as sum_charge " +
                        " from " + sChargeAlias + ":" + sChargeTable + " a,  " +
                        " t_kvars k ,outer " + pref + "_kernel:formuls f " +
                        " where k.nzp_kvar =a.nzp_kvar and a.nzp_serv in (6,510) and dat_charge is null " +
                        " and a.nzp_frm =f.nzp_frm ");
#endif

                if (prm.nzp_key > -1)
                {
                    sql.Append(" and a.nzp_supp=" + prm.nzp_key);
                }
#if PG
                sql.Append("  group by 1, 2, 3 ,4 ,5  ,6, 7,8 ");
#else
                sql.Append("  group by 1, 2, 3 ,4 ,5  ,6, 7,8 into temp t_charges with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_charges set is_device = coalesce((SELECT case when val_prm = '1' THEN 1 ELSE 0 END FROM " +
                    pref + "_data.prm_1 p where nzp_prm =102 and is_actual <> 100 and (extract(year from dat_po) = 3000) and p.nzp = t_charges.nzp_kvar), 0)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table tis_dev", false);
                sql.Remove(0, sql.Length);
                sql.Append(" select nzp_kvar, max(is_device) as is_device " +
#if PG
 " into temp tis_dev from t_charges  " +

                           " where nzp_serv = 6 group by 1 ");
#else
 " from t_charges where nzp_serv = 6 group by 1  into temp tis_dev with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_charges set is_device = coalesce((select is_device " +
#else
                sql.Append(" update t_charges set is_device = nvl((select is_device " +
#endif
 " from tis_dev where tis_dev.nzp_kvar = t_charges.nzp_kvar ),is_device)" +
                           " where nzp_serv > 6 ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;




                #region Выборка перерасчетов прошлого периода
                ExecSQL(conn_db, "drop table t_nedop", false);
                ExecSQL(conn_db, "drop table t_sum_nedop", false);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create temp table t_sum_nedop( nzp_dom integer, nzp_kvar integer, is_device integer, ");
                sql.Append(" tarif numeric(14,4),nzp_serv integer, rsum_tarif numeric(14,2), sum_nedop numeric(14,2)) ");
#else
                sql.Append(" create temp table t_sum_nedop( nzp_dom integer, nzp_kvar integer, is_device integer, ");
                sql.Append(" tarif Decimal(14,4),nzp_serv integer, rsum_tarif Decimal(14,2), sum_nedop decimal(14,2)) with no log");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select b.nzp_dom, a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
                sql.Append(" into temp t_nedop from " + pref + "_data.nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                    + prm.month_.ToString("00") + "." +
                    prm.year_.ToString("0000") + "'  and a.nzp_serv in (6,510) ");
                    //prm.year_.ToString("0000") + "'  and a.nzp_serv in (6) ");вата
                sql.Append(" group by 1,2 ");
#else
                sql.Append(" select b.nzp_dom, a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
                sql.Append(" from " + pref + "_data:nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                    + prm.month_.ToString("00") + "." +
                    prm.year_.ToString("0000") + "'  and a.nzp_serv in (6,510) ");
                sql.Append(" group by 1,2 into temp t_nedop with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }


                sql.Remove(0, sql.Length);
                sql.Append(" select month_, year_ ");
#if PG
                sql.Append(" from " + sChargeAlias + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#else
                sql.Append(" from " + sChargeAlias + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#endif
                sql.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                sql.Append(" group by 1,2");
                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                while (reader2.Read())
                {
                    string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_sum_nedop (nzp_kvar,is_device,tarif, nzp_dom, ");
                    sql.Append("nzp_serv, rsum_tarif, sum_nedop) ");
                    sql.Append(" select b.nzp_kvar,0,0,nzp_dom, nzp_serv, sum(0), ");
                    sql.Append(" sum(sum_nedop-sum_nedop_p) ");
#if PG
                    sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#else
                    sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#endif
                    sql.Append(" b, t_nedop d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                    sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                    sql.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                    sql.Append(" and nzp_serv in (6,510) ");
                    //sql.Append(" and nzp_serv in (6) ");вата
                    if (prm.nzp_key > -1)
                    {
                        sql.Append(" and nzp_supp=" + prm.nzp_key);
                    }
                    sql.Append(" group by 1,2,3,4,5");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                }
                reader2.Close();
                ExecSQL(conn_db, "drop table t_nedop", true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_charges set sum_nedop=coalesce(sum_nedop,0)+coalesce((select sum(sum_nedop) ");
#else
                sql.Append(" update t_charges set sum_nedop=nvl(sum_nedop,0)+nvl((select sum(sum_nedop) ");
#endif
                sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
                sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_charges set reval=coalesce(reval,0)+coalesce((select sum(sum_nedop) ");
#else
                sql.Append(" update t_charges set reval=nvl(reval,0)+nvl((select sum(sum_nedop) ");
#endif
                sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
                sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "drop table t_sum_nedop", true);
                #endregion


                //Проставляем расход по основным услугам
                sql.Remove(0, sql.Length);
                sql.Append(" update t_charges set (c_calc) = ((select sum(valm+dlt_reval) ");
#if PG
                sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
#else
                sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_"
#endif
 + prm.month_.ToString("00") + " d ");
                sql.Append(" where d.nzp_kvar=t_charges.nzp_kvar  and d.tarif>0 and t_charges.nzp_frm=d.nzp_frm");
                sql.Append(" and 6 = d.nzp_serv ");

                if (prm.nzp_key > -1) //Добавляем поставщика
                {
                    sql.Append(" and d.nzp_supp = " + prm.nzp_key);
                }
                sql.Append(" )) where nzp_serv=6  and tarif>0  ");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" update t_charges set ( c_calc) = ((select ");
                sql.Append(" sum(case when valm+dlt_reval>=0 and dop87<0 and ");
                sql.Append(" dop87< -valm-dlt_reval then -valm-dlt_reval  ");
                sql.Append("          else dop87 end) ");
#if PG
                sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
#else
                sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_"
#endif
 + prm.month_.ToString("00") + " d ");
                sql.Append(" where d.nzp_kvar=t_charges.nzp_kvar  and d.tarif>0 ");
                sql.Append(" and 6 = d.nzp_serv ");

                if (prm.nzp_key > -1) //Добавляем поставщика
                {
                    sql.Append(" and d.nzp_supp = " + prm.nzp_key);
                }
                //sql.Append(" )) where nzp_serv=510  and tarif>0  ");
                sql.Append(" )) where nzp_serv=0  and tarif>0  ");
                ExecSQL(conn_db, sql.ToString(), true);



                #region Устанавливаем тарифы


                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set tarif_kub = (" +
                         " select max(tarif) from t_charges a " +
                         " where nzp_measure=3 and nzp_serv<500 and t_local_water.nzp_dom=a.nzp_dom  )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set tarif_kub = coalesce(" +
                         " (select max(tarif_hv_gv) from t_charges a," +
                         Points.Pref + "_data.a_trf_smr b " +
#else
                sql.Append(" update t_local_water set tarif_kub = nvl(" +
                         " (select max(tarif_hv_gv) from t_charges a," +
                         Points.Pref + "_data:a_trf_smr b " +
#endif
 " where a.nzp_frm=b.nzp_frm and a.nzp_serv=6 and is_priv = 1 ),0)" +
                         " where tarif_kub is null");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                #endregion




                //Общий объем
                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set volume_all_kub=" +
#if PG
 " coalesce((select  sum(case when a.nzp_measure=2 " +
#else
 " nvl((select  sum(case when a.nzp_measure=2 " +
#endif
 " then norma*a.c_calc else a.c_calc end)  " +
                           " from t_charges a " +
                           " where t_local_water.nzp_dom=a.nzp_dom " +
                           "  and nzp_serv in (6,510) ),0)");
                           //"  and nzp_serv in (6) ),0)");вата
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set vol_odn_kub = " +
#if PG
 " coalesce((select  sum(a.c_calc)  " +
#else
 " nvl((select  sum(a.c_calc)  " +
#endif
 " from t_charges a " +
                           " where t_local_water.nzp_dom=a.nzp_dom  " +
                           " and nzp_serv = 510),0)");
                           //" and nzp_serv = 0),0)");вата
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                //объем по лицевым счетам без ОДН
                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set vol_ipu_kub= " +
                           " (select  sum(a.c_calc ) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and a.is_device = 1 and nzp_serv = 6 " +
                           " )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water  set vol_npu_kub=" +
                           " (select  sum(a.c_calc ) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and a.is_device=0  and nzp_serv = 6)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water  set vol_odn_kub=" +
                           " (select  sum(case when a.nzp_measure=2 " +
                           " then norma*a.c_calc else a.c_calc end ) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and nzp_serv = 510 )");
                          // " and nzp_serv = 0 )");вата
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append("  update t_local_water set vozv_kub= " +
                           "  (select  sum(a.sum_nedop/tarif_kub) " +
                           "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           "  and tarif_kub>0 and nzp_serv = 6 )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append("  update t_local_water set vozv_odn_kub= " +
                           "  (select  sum(a.sum_nedop/tarif_kub) " +
                           "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           "  and tarif_kub>0 and nzp_serv = 510 )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set reval_kub = " +
                           " (select  sum(a.reval/tarif_kub) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and tarif_kub > 0 )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water   set vol_charge_kub= coalesce(volume_all_kub,0) - " +
                           " coalesce(vozv_kub,0) + coalesce(reval_kub,0)  ");
#else
                sql.Append(" update t_local_water   set vol_charge_kub= nvl(volume_all_kub,0) - " +
                           " nvl(vozv_kub,0) + nvl(reval_kub,0)  ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;




                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_local_water set " +
                           " rsum_tarif  = (select  sum(a.rsum_tarif)+sum(case when nzp_serv=510 and c_calc<0 then " +
                           " Round(c_calc*tarif,2) else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " rsum_tarif_odn  = (select  sum(case when nzp_serv>500 then Round(c_calc*tarif,2) else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " sum_nedop  = (select  sum(a.sum_nedop) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " sum_nedop_odn  = (select  sum(case when nzp_serv>500 then a.sum_nedop else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " reval  = (select  sum(a.reval) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " sum_charge  = (select  sum(a.sum_charge) from t_charges a where t_local_water.nzp_dom=a.nzp_dom) " +
                           " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#else
                sql.Append("  update t_local_water set (rsum_tarif, rsum_tarif_odn, sum_nedop, " +
                           " sum_nedop_odn, reval, sum_charge)" +
                           " =((select  sum(a.rsum_tarif), " +
                           " sum(case when nzp_serv>500 then Round(c_calc*tarif,2) else 0 end), " +
                           " sum(a.sum_nedop), " +
                           " sum(case when nzp_serv>500 then a.sum_nedop else 0 end), " +
                           " sum(a.reval),sum(a.sum_charge) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom))" +
                           " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;
                ///////////////////
                //sql.Remove(0, sql.Length);
                //sql.Append("  update t_local_water set (count_gil_ipu, count_gil_npu)= " +
                //           " ((select  sum(case when cnt_stage=1 then gil1 else 0 end), " +
                //           "  sum(case when cnt_stage=1 then 0 else gil1 end)  " +
                //           " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " a" +
                //           " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 "+
                //           " and nzp_serv=9))");
                //if (!ExecSQL(conn_db, sql.ToString(), true).result)
                //    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_local_water set " +
                           " count_gil_ipu = (select  sum(case when is_device=1 then gil1 else 0 end) " +
                           " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") + " a, tis_dev t" +
                           " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                           " and nzp_serv=6 and a.nzp_kvar=t.nzp_kvar) , " +
                           " count_gil_npu = (select  sum(case when is_device=1 then 0 else gil1 end)" +
                           " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") + " a, tis_dev t" +
                           " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                           " and nzp_serv=6 and a.nzp_kvar=t.nzp_kvar) ");
#else
                sql.Append("  update t_local_water set (count_gil_ipu, count_gil_npu)= " +
                           " ((select  sum(case when is_device=1 then gil1 else 0 end), " +
                           "  sum(case when is_device=1 then 0 else gil1 end)  " +
                           " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " a, tis_dev t" +
                           " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                           " and nzp_serv=6 and a.nzp_kvar=t.nzp_kvar ))");
#endif


                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                ExecSQL(conn_db, "drop table tis_dev", true);

                sql.Remove(0, sql.Length);
                sql.Append(" select a.nzp_dom, s.nzp_measure, sum(case when stek=1 then val1 else 0 end) as dpu1, ");
                sql.Append(" sum(case when stek=2 then val1 else 0 end) as dpu2 ");
#if PG
                sql.Append(" into temp t_dom_pok from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") + " a, ");
                sql.Append(pref + "_data.counters_spis cs,  " + pref + "_kernel.s_counts s, t_local_water t ");
                sql.Append(" where  dat_charge is null and a.nzp_dom=t.nzp_dom ");
                sql.Append(" and stek in (1,2) and a.nzp_type=1 and a.nzp_serv=s.nzp_serv and a.nzp_serv=6 ");
                sql.Append(" and a.nzp_counter=cs.nzp_counter and cs.nzp_cnt=s.nzp_cnt ");
                sql.Append(" group by 1,2 ");
#else
                sql.Append(" from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " a, ");
                sql.Append(pref + "_data:counters_spis cs,  " + pref + "_kernel:s_counts s, t_local_water t ");
                sql.Append(" where  dat_charge is null and a.nzp_dom=t.nzp_dom ");
                sql.Append(" and stek in (1,2) and a.nzp_type=1 and a.nzp_serv=s.nzp_serv and a.nzp_serv=6 ");
                sql.Append(" and a.nzp_counter=cs.nzp_counter and cs.nzp_cnt=s.nzp_cnt ");
                sql.Append(" group by 1,2 into temp t_dom_pok with no log   ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set odpu_kub = (select sum(case when dpu1>0 then dpu1 else dpu2 end) ");
                sql.Append(" from t_dom_pok where t_local_water.nzp_dom=t_dom_pok.nzp_dom");
                sql.Append(" and nzp_measure=3) ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table t_dom_pok", true);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set nzp_dom = (select nzp_dom_base from " + pref + "_data.link_dom_lit a");
                sql.Append(" where a.nzp_dom=t_local_water.nzp_dom) ");
                sql.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                sql.Append(" update t_local_water set nzp_dom = (select nzp_dom_base from " + pref + "_data:link_dom_lit a");
                sql.Append(" where a.nzp_dom=t_local_water.nzp_dom) ");
                sql.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append("  insert into t_svod_water(nzp_dom, nzp_dom_base,  count_gil_ipu , count_gil_npu , volume_all_kub  , " +
                  "  vol_ipu_kub , vol_npu_kub , " +
                  "  vol_odn_kub , rsum_tarif  , " +
                  " rsum_tarif_odn  ,vozv_kub , vozv_odn_kub , " +
                  " sum_nedop , sum_nedop_odn  , " +
                  " reval_kub , reval  , vol_charge_kub , " +
                  " sum_charge  , odpu_kub  )" +
                  " select   nzp_dom , nzp_dom, " +
                  " sum(count_gil_ipu) , " + //количество граждан в ЛС с приборами учета
                  " sum(count_gil_npu) , " +//количество граждан в ЛС без приборов учета
                  " sum(volume_all_kub)  , " + //объем в кубометрах по дому
                  " sum(vol_ipu_kub) , " + //Объем по ЛС с ИПУ в куб.м.
                  " sum(vol_npu_kub) , " +//Объем по ЛС без ИПУ в куб.м.
                  " sum(vol_odn_kub) , " +//объем ОДН в куб.м.
                  " sum(rsum_tarif)  , " +//начислено по тарифу по основной услуге
                  " sum(rsum_tarif_odn)  , " +//начислено по тарифу по ОДН

#if PG
                  " sum(coalesce(vozv_kub,0)+coalesce(vozv_odn_kub,0)) , " + //Недопоставки в куб.м.
#else
                  " sum(nvl(vozv_kub,0)+nvl(vozv_odn_kub,0)) , " + //Недопоставки в куб.м.
#endif
                  " sum(vozv_odn_kub) , " +//Возвраты по недопоставкам по ОДН в куб.м.
                  " sum(sum_nedop), " +//Сумма недопоставки
                  " sum(sum_nedop_odn)  , " +//Сумма недопоставки по ОДН

                  " sum(reval_kub) , " + //объем перерасчета в куб.м.
                  " sum(reval)  , " + //Сумма перерасчета

                  " sum(vol_charge_kub) , " +//Объем начислено к оплате в куб.м.
                  " sum(sum_charge)  , " + //Сумма начислено к оплате

                  " sum(odpu_kub)  " + //Объем предъявленный жильцам в куб. 
                  " from t_local_water " +
#if PG
 " where abs(coalesce(rsum_tarif,0))+abs(coalesce(sum_nedop,0))+abs(coalesce(reval,0))+" +
                  " abs(coalesce(sum_charge,0))>0.001 group by 1,2 ");
#else
 " where abs(nvl(rsum_tarif,0))+abs(nvl(sum_nedop,0))+abs(nvl(reval,0))+" +
                  " abs(nvl(sum_charge,0))>0.001 group by 1,2 ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table t_local_water", true);
            }




            sql.Remove(0, sql.Length);
            sql.Append(" select ulica, ndom, idom, nkor, a.*" +
#if PG
 "  from t_svod_water a, " + Points.Pref + "_data.dom d, " +
                Points.Pref + "_data.s_ulica s where a.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul " +
#else
 "  from t_svod_water a, " + Points.Pref + "_data:dom d, " +
                Points.Pref + "_data:s_ulica s where a.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul " +
#endif
 " order by ulica, idom, ndom, nkor");
            ExecRead(conn_db, out reader2, sql.ToString(), true);

            Utils.setCulture();
            if (reader2 != null)
            {
                try
                {

                    LocalTable.Load(reader2, LoadOption.PreserveChanges);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!! Отчет по ГВС " + ex.Message, MonitorLog.typelog.Error, true);
                    conn_web.Close();
                    return null;
                }
            }
            if (reader2 != null) reader2.Close();
            if (reader != null) reader.Close();
            sql.Remove(0, sql.Length);
            ExecSQL(conn_db, "drop table t_svod_water", true);
            ExecSQL(conn_db, "drop table sel_kvar_water", true);
            #endregion



            conn_db.Close();
            return LocalTable;
        }


        /// <summary>
        /// Процедура формирования отчета РСО по водоотведению для Самары
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        public DataTable GetSpravSoderg2Kan(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            IDataReader reader;
            IDataReader reader2;

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();


            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();

#if PG
            string tXX_spls = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif

            conn_web.Close();

            ExecSQL(conn_db, "drop table sel_kvar_water", false);
            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select nzp_dom,nzp_kvar,pref into temp sel_kvar_water from " + tXX_spls);
#else
            sql.Append(" select nzp_dom,nzp_kvar,pref from " + tXX_spls + " into temp sel_kvar_water with no log ");
#endif
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "drop table t_svod_water", false);

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" create temp table t_svod_water( " +
                  " nzp_dom integer, " +
                  " nzp_dom_base integer, " +
                  " count_gil_ipu integer, " + //количество граждан в ЛС с приборами учета
                  " count_gil_npu integer, " +//количество граждан в ЛС без приборов учета
                  " volume_all_kub numeric(14,4) , " + //объем в кубометрах по дому
                  " vol_ipu_kub numeric(14,4), " + //Объем по ЛС с ИПУ в куб.м.
                  " vol_npu_kub numeric(14,4), " +//Объем по ЛС без ИПУ в куб.м.
                  " rsum_tarif  numeric(14,4), " +//начислено по тарифу по основной услуге
                  " vozv_kub numeric(14,4), " + //Недопоставки в куб.м.
                  " sum_nedop  numeric(14,4) default 0, " +//Сумма недопоставки
                  " reval_kub numeric(14,4), " + //объем перерасчета в куб.м.
                  " reval  numeric(14,4), " + //Сумма перерасчета
                  " vol_charge_kub numeric(14,4), " +//Объем начислено к оплате в куб.м.
                  " sum_charge  numeric(14,4), " + //Сумма начислено к оплате
                  " tarif_kub numeric(14,4) default 0, " +//Тариф на дом на Куб.
                  " norma numeric(14,2) )  ");
#else
            sql.Append(" create temp table t_svod_water( " +
                  " nzp_dom integer, " +
                  " nzp_dom_base integer, " +
                  " count_gil_ipu integer, " + //количество граждан в ЛС с приборами учета
                  " count_gil_npu integer, " +//количество граждан в ЛС без приборов учета
                  " volume_all_kub Decimal(14,4) , " + //объем в кубометрах по дому
                  " vol_ipu_kub Decimal(14,4), " + //Объем по ЛС с ИПУ в куб.м.
                  " vol_npu_kub Decimal(14,4), " +//Объем по ЛС без ИПУ в куб.м.
                  " rsum_tarif  Decimal(14,4), " +//начислено по тарифу по основной услуге
                  " vozv_kub Decimal(14,4), " + //Недопоставки в куб.м.
                  " sum_nedop  Decimal(14,4) default 0, " +//Сумма недопоставки
                  " reval_kub Decimal(14,4), " + //объем перерасчета в куб.м.
                  " reval  Decimal(14,4), " + //Сумма перерасчета
                  " vol_charge_kub Decimal(14,4), " +//Объем начислено к оплате в куб.м.
                  " sum_charge  Decimal(14,4), " + //Сумма начислено к оплате
                  " tarif_kub Decimal(14,4) default 0, " +//Тариф на дом на Куб.
                  " norma decimal(14,2) ) with no log ");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                return null;

            ExecSQL(conn_db, "create index ix_tmpww_01 on t_svod_water(nzp_dom)", true);


            sql.Remove(0, sql.Length);
            sql.Append(" select pref from sel_kvar_water group by 1 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            while (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string sChargeAlias = pref + "_charge_" + prm.year_.ToString().Substring(2, 2);
                string sChargeTable = "charge_" + prm.month_.ToString("00");


                ExecSQL(conn_db, "drop table t_local_water", false);
#if PG
                ExecSQL(conn_db, "select * into temp t_local_water from t_svod_water where nzp_dom=-1 ", true);
#else
                ExecSQL(conn_db, "select * from t_svod_water where nzp_dom=-1 into temp t_local_water with no log", true);
#endif

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_local_water(nzp_dom) ");
                sql.Append(" select nzp_dom from sel_kvar_water ");
                sql.Append(" where pref = '" + pref + "'");
                if (prm.has_pu == "2")
                    sql.Append(" and 0 < (select count(*)  " +
#if PG
 " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6) ");
                if (prm.has_pu == "3")
                    sql.Append(" and 0 = (select count(*)  " +
                        " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
#else
 " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") +
                        " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6) ");
                if (prm.has_pu == "3")
                    sql.Append(" and 0 = (select count(*)  " +
                        " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") +
#endif
 " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=6) ");
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and 0<(select count(*) from " + sChargeAlias + ":" + sChargeTable + " a ");
                    sql.Append(" where a.nzp_kvar=sel_kvar_water.nzp_kvar and ");
                    sql.Append(" dat_charge is null ");
                    sql.Append(" and a.nzp_serv = 7 and a.nzp_supp=" + prm.nzp_key + ")");
                }

                sql.Append(" group by 1 ");
                ExecSQL(conn_db, sql.ToString(), true);


                ExecSQL(conn_db, "drop table t_kvars", false);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select skw.nzp_kvar , d.nzp_dom, d.norma into temp t_kvars " +
                           " from t_local_water d, sel_kvar_water skw " +
                           " where d.nzp_dom=skw.nzp_dom  ");
#else
                sql.Append(" select skw.nzp_kvar , d.nzp_dom, d.norma " +
                           " from t_local_water d, sel_kvar_water skw " +
                           " where d.nzp_dom=skw.nzp_dom  " +
                           " into temp t_kvars with no log ");
#endif
                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append("  create index ix_t_kvars on t_kvars (nzp_dom , nzp_kvar)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                #region Устанавливаем норматив на горяую воду
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("update t_local_water set norma = " +
                        " (select distinct max(value)::numeric from " + pref + "_kernel.res_values where nzp_res = " +
                        " (select distinct a.val_prm::int from " + pref + "_data.prm_13 a, " + pref + "_kernel.prm_name b " +
                        " where a.nzp_prm =172 and a.nzp_prm=b.nzp_prm " +
                        " and dat_s <cast('01." + prm.month_.ToString("00") + "." + prm.year_.ToString() + "'  as timestamp)+ interval '1 month' " +
                        " and dat_po>=cast('01." + prm.month_.ToString("00") + "." + prm.year_.ToString() + "' as timestamp)) " +
                        " and  nzp_y= (select distinct max(val_prm)::int from " + pref + "_data.prm_1 p, t_kvars tk" +
                        " where nzp_prm=7 and tk.nzp_kvar=nzp and  tk.nzp_dom=t_local_water.nzp_dom) and nzp_x=3  ) ");
#else
                sql.Append("update t_local_water set norma = " +
                        " (select max(value) from " + pref + "_kernel:res_values where nzp_res = " +
                        " (select  a.val_prm from " + pref + "_data:prm_13 a, " + pref + "_kernel:prm_name b " +
                        " where a.nzp_prm =172 and a.nzp_prm=b.nzp_prm " +
                        " and dat_s <cast('01." + prm.month_.ToString("00") + "." + prm.year_.ToString() + "'  as date)+1 units month " +
                        " and dat_po>=cast('01." + prm.month_.ToString("00") + "." + prm.year_.ToString() + "' as date)) " +
                        " and  nzp_y= (select  max(val_prm) from " + pref + "_data:prm_1 p, t_kvars tk" +
                        " where nzp_prm=7 and tk.nzp_kvar=nzp and  tk.nzp_dom=t_local_water.nzp_dom) and nzp_x=3  ) ");
#endif
                sql.Append(" where  norma is null ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;





                #endregion

                ExecSQL(conn_db, "drop table t_charges", false);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
                        " coalesce(f.nzp_measure,3) as nzp_measure, a.nzp_frm , " +
                        " a.tarif,coalesce(f.is_device,0) as is_device," +
                        " sum(a.rsum_tarif) as rsum_tarif, " +
                        " sum(a.sum_real) as sum_real, sum(a.c_calc) as c_calc, sum(a.reval+real_charge) as reval, " +
                        " sum(a.sum_nedop) as sum_nedop   , " +
                        " sum(a.sum_charge) as sum_charge into temp t_charges " +
                        " from t_kvars k , " + sChargeAlias + "." + sChargeTable + " a  " +
                        " left outer join " + pref + "_kernel.formuls f on (a.nzp_frm =f.nzp_frm)" +
                        " where k.nzp_kvar =a.nzp_kvar and a.nzp_serv = 7 and dat_charge is null ");
#else
                sql.Append(" select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
                        " nvl(f.nzp_measure,3) as nzp_measure, a.nzp_frm , " +
                        " a.tarif,nvl(f.is_device,0) as is_device," +
                        " sum(a.rsum_tarif) as rsum_tarif, " +
                        " sum(a.sum_real) as sum_real, sum(a.c_calc) as c_calc, sum(a.reval+real_charge) as reval, " +
                        " sum(a.sum_nedop) as sum_nedop   , " +
                        " sum(a.sum_charge) as sum_charge " +
                        " from " + sChargeAlias + ":" + sChargeTable + " a,  " +
                        " t_kvars k ,outer " + pref + "_kernel:formuls f " +
                        " where k.nzp_kvar =a.nzp_kvar and a.nzp_serv = 7 " +
                        " and a.nzp_frm =f.nzp_frm and dat_charge is null ");
#endif
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and a.nzp_supp=" + prm.nzp_key);
                }
#if PG
                sql.Append("  group by 1, 2, 3 ,4 ,5  ,6, 7, 8 ");
#else
                sql.Append("  group by 1, 2, 3 ,4 ,5  ,6, 7, 8 into temp t_charges with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table tis_dev", false);
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select nzp_kvar, max(is_device) as is_device " +
                           " into temp tis_dev  from t_charges  " +
                           " where nzp_serv = 6 group by 1 ");
#else
                sql.Append(" select nzp_kvar, max(is_device) as is_device " +
                           " from t_charges  " +
                           " where nzp_serv = 6 group by 1  into temp tis_dev with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                #region Выборка перерасчетов прошлого периода
                ExecSQL(conn_db, "drop table t_nedop", false);
                ExecSQL(conn_db, "drop table t_sum_nedop", false);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create temp  table t_sum_nedop( nzp_dom integer, nzp_kvar integer, is_device integer, ");
                sql.Append(" tarif numeric(14,4),nzp_serv integer, rsum_tarif numeric(14,2), sum_nedop numeric(14,2)) ");
#else
                sql.Append(" create temp table t_sum_nedop( nzp_dom integer, nzp_kvar integer, is_device integer, ");
                sql.Append(" tarif Decimal(14,4),nzp_serv integer, rsum_tarif Decimal(14,2), sum_nedop decimal(14,2)) with no log");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select b.nzp_dom, a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
                sql.Append(" into temp t_nedop from " + pref + "_data.nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                    + prm.month_.ToString("00") + "." +
                    prm.year_.ToString("0000") + "'  and a.nzp_serv = 7 ");
                sql.Append(" group by 1,2 ");
#else
                sql.Append(" select b.nzp_dom, a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
                sql.Append(" from " + pref + "_data:nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                    + prm.month_.ToString("00") + "." +
                    prm.year_.ToString("0000") + "'  and a.nzp_serv = 7 ");
                sql.Append(" group by 1,2 into temp t_nedop with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }


                sql.Remove(0, sql.Length);
                sql.Append(" select month_, year_ ");
#if PG
                sql.Append(" from " + sChargeAlias + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#else
                sql.Append(" from " + sChargeAlias + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#endif
                sql.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                sql.Append(" group by 1,2");
                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                while (reader2.Read())
                {
                    string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_sum_nedop (nzp_kvar,is_device,tarif, nzp_dom, nzp_serv, rsum_tarif, sum_nedop) ");
                    sql.Append(" select b.nzp_kvar,0,0,nzp_dom, nzp_serv, sum(0), ");
                    sql.Append(" sum(sum_nedop-sum_nedop_p) ");
#if PG
                    sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#else
                    sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#endif
                    sql.Append(" b, t_nedop d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                    sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                    sql.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                    sql.Append(" and nzp_serv = 7 ");
                    if (prm.nzp_key > -1)
                    {
                        sql.Append(" and nzp_supp=" + prm.nzp_key);
                    }
                    sql.Append(" group by 1,2,3,4,5");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                }
                reader2.Close();
                ExecSQL(conn_db, "drop table t_nedop", true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_charges set sum_nedop=coalesce(sum_nedop,0)+coalesce((select sum(sum_nedop) ");
#else
                sql.Append(" update t_charges set sum_nedop=nvl(sum_nedop,0)+nvl((select sum(sum_nedop) ");
#endif
                sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
                sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_charges set reval=coalesce(reval,0)+coalesce((select sum(sum_nedop) ");
#else
                sql.Append(" update t_charges set reval=nvl(reval,0)+nvl((select sum(sum_nedop) ");
#endif
                sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
                sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "drop table t_sum_nedop", true);
                #endregion


                #region Устанавливаем тарифы


                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set tarif_kub = (" +
                         " select max(tarif) from t_charges a " +
                         " where nzp_measure=3 and nzp_serv<500 and t_local_water.nzp_dom=a.nzp_dom  )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set tarif_kub = coalesce(" +
                           " (select max(tarif_hv_gv) from t_charges a," +
                           Points.Pref + "_data.a_trf_smr b " +
#else
                sql.Append(" update t_local_water set tarif_kub = nvl(" +
                " (select max(tarif_hv_gv) from t_charges a," +
                Points.Pref + "_data:a_trf_smr b " +
#endif
 " where a.nzp_frm=b.nzp_frm and a.nzp_serv=7 and is_priv = 1 ),0)" +
                " where tarif_kub is null");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                #endregion




                //Общий объем
                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set volume_all_kub=" +
#if PG
 " coalesce((select  sum(case when a.nzp_measure=2 " +
#else
 " nvl((select  sum(case when a.nzp_measure=2 " +
#endif
 " then round(norma*a.c_calc,2) else a.c_calc end)  " +
                           " from t_charges a " +
                           " where t_local_water.nzp_dom=a.nzp_dom " +
                           " and abs(rsum_tarif)>0.001 and nzp_serv = 7 ),0)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                //объем по лицевым счетам без ОДН
                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set vol_ipu_kub= " +
                           " (select  sum(case when a.nzp_measure=2 " +
                           " then round(a.norma*a.c_calc,2) else a.c_calc end ) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and a.is_device = 1 and nzp_serv = 7 " +
                           " and abs(rsum_tarif)>0.001)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water  set vol_npu_kub=" +
                           " (select  sum(case when a.nzp_measure=2 " +
                           " then round(norma*a.c_calc,2) else a.c_calc end ) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and a.is_device=0 and abs(rsum_tarif)>0.001 and nzp_serv = 7)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                sql.Remove(0, sql.Length);
                sql.Append("  update t_local_water set vozv_kub= " +
                           "  (select  sum(a.sum_nedop/tarif_kub) " +
                           "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           "  and tarif_kub>0 and nzp_serv = 6 )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" update t_local_water set reval_kub = " +
                           " (select  sum(a.reval/tarif_kub) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                           " and tarif_kub > 0 )");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water   set vol_charge_kub= coalesce(volume_all_kub,0) + " +
                           " coalesce(vozv_kub,0) + coalesce(reval_kub,0)  ");
#else
                sql.Append(" update t_local_water   set vol_charge_kub= nvl(volume_all_kub,0) + " +
                           " nvl(vozv_kub,0) + nvl(reval_kub,0)  ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;




                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set " +
                           " rsum_tarif = (select  sum(a.rsum_tarif) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " sum_nedop = (select sum(a.sum_nedop) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " reval = (select sum(a.reval) from t_charges a where t_local_water.nzp_dom=a.nzp_dom), " +
                           " sum_charge = (select sum(a.sum_charge) from t_charges a where t_local_water.nzp_dom=a.nzp_dom) " +
                           " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#else
                sql.Append("  update t_local_water set (rsum_tarif, sum_nedop, " +
                           "  reval, sum_charge)" +
                           " =((select  sum(a.rsum_tarif)," +
                           " sum(a.sum_nedop), " +
                           " sum(a.reval),sum(a.sum_charge) " +
                           " from t_charges a where t_local_water.nzp_dom=a.nzp_dom))" +
                           " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;
                ///////////////////
                //sql.Remove(0, sql.Length);
                //sql.Append("  update t_local_water set (count_gil_ipu, count_gil_npu)= " +
                //           " ((select  sum(case when cnt_stage=1 then gil1 else 0 end), " +
                //           "  sum(case when cnt_stage=1 then 0 else gil1 end)  " +
                //           " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " a" +
                //           " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 "+
                //           " and nzp_serv=9))");
                //if (!ExecSQL(conn_db, sql.ToString(), true).result)
                //    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set  " +
                           " count_gil_ipu = (select  sum(case when t.is_device=1 then gil else 0 end) " +
                           " from " + sChargeAlias + ".calc_gku_" + prm.month_.ToString("00") + " a, " +
                             pref + "_kernel.formuls t" +
                           " where a.nzp_dom =t_local_water.nzp_dom " +
                           " and nzp_serv=7 and a.nzp_frm=t.nzp_frm and a.tarif>0), " +
                           " count_gil_npu = (select  sum(case when t.is_device=1 then 0 else gil end)" +
                           " from " + sChargeAlias + ".calc_gku_" + prm.month_.ToString("00") + " a, " +
                             pref + "_kernel.formuls t" +
                           " where a.nzp_dom =t_local_water.nzp_dom " +
                           " and nzp_serv=7 and a.nzp_frm=t.nzp_frm and a.tarif>0)");

#else
                sql.Append("  update t_local_water set (count_gil_ipu, count_gil_npu)= " +
                           " ((select  sum(case when t.is_device=1 then gil else 0 end), " +
                           "  sum(case when t.is_device=1 then 0 else gil end)  " +
                           " from " + sChargeAlias + ":calc_gku_" + prm.month_.ToString("00") + " a, " +
                           pref + "_kernel:formuls t" +
                           " where a.nzp_dom =t_local_water.nzp_dom " +
                           " and nzp_serv=7 and a.nzp_frm=t.nzp_frm and a.tarif>0))");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                ExecSQL(conn_db, "drop table tis_dev", true);





                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_water set nzp_dom = (select nzp_dom_base from " + pref + "_data.link_dom_lit a");
                sql.Append(" where a.nzp_dom=t_local_water.nzp_dom) ");
                sql.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                sql.Append(" update t_local_water set nzp_dom = (select nzp_dom_base from " + pref + "_data:link_dom_lit a");
                sql.Append(" where a.nzp_dom=t_local_water.nzp_dom) ");
                sql.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append("  insert into t_svod_water(nzp_dom, nzp_dom_base, " +
                  " count_gil_ipu , count_gil_npu , volume_all_kub  , " +
                  " vol_ipu_kub , vol_npu_kub , " +
                  " rsum_tarif  , vozv_kub ,  sum_nedop ,  " +
                  " reval_kub , reval  , vol_charge_kub , " +
                  " sum_charge     )" +
                  " select   nzp_dom , nzp_dom, " +
                  " sum(count_gil_ipu) , " + //количество граждан в ЛС с приборами учета
                  " sum(count_gil_npu) , " +//количество граждан в ЛС без приборов учета
                  " sum(volume_all_kub)  , " + //объем в кубометрах по дому
                  " sum(vol_ipu_kub) , " + //Объем по ЛС с ИПУ в куб.м.
                  " sum(vol_npu_kub) , " +//Объем по ЛС без ИПУ в куб.м.
                  " sum(rsum_tarif)  , " +//начислено по тарифу по основной услуге
#if PG
                  " sum(coalesce(vozv_kub,0)) , " + //Недопоставки в куб.м.
#else
                  " sum(nvl(vozv_kub,0)) , " + //Недопоставки в куб.м.
#endif
                  " sum(sum_nedop), " +//Сумма недопоставки
                  " sum(reval_kub) , " + //объем перерасчета в куб.м.
                  " sum(reval)  , " + //Сумма перерасчета
                  " sum(vol_charge_kub) , " +//Объем начислено к оплате в куб.м.
                  " sum(sum_charge)   " + //Сумма начислено к оплате
                  " from t_local_water " +
#if PG
 " where abs(coalesce(rsum_tarif,0))+abs(coalesce(sum_nedop,0))+abs(coalesce(reval,0))+" +
                  " abs(coalesce(sum_charge,0))>0.001 group by 1,2 ");
#else
 " where abs(nvl(rsum_tarif,0))+abs(nvl(sum_nedop,0))+abs(nvl(reval,0))+" +
                  " abs(nvl(sum_charge,0))>0.001 group by 1,2 ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                ExecSQL(conn_db, "drop table t_local_water", true);
            }




            sql.Remove(0, sql.Length);
            sql.Append(" select ulica, ndom, idom, nkor, a.*" +
#if PG
 "  from t_svod_water a, " + Points.Pref + "_data.dom d, " +
                Points.Pref + "_data.s_ulica s where a.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul " +
#else
 "  from t_svod_water a, " + Points.Pref + "_data:dom d, " +
                Points.Pref + "_data:s_ulica s where a.nzp_dom=d.nzp_dom and d.nzp_ul=s.nzp_ul " +
#endif
 " order by ulica, idom, ndom, nkor");
            ExecRead(conn_db, out reader2, sql.ToString(), true);

            Utils.setCulture();
            if (reader2 != null)
            {
                try
                {

                    LocalTable.Load(reader2, LoadOption.PreserveChanges);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!! Отчет по Водоотведению " + ex.Message, MonitorLog.typelog.Error, true);
                    conn_web.Close();
                    return null;
                }
            }
            if (reader2 != null) reader2.Close();
            if (reader != null) reader.Close();
            sql.Remove(0, sql.Length);
            ExecSQL(conn_db, "drop table t_svod_water", true);
            ExecSQL(conn_db, "drop table sel_kvar_water", true);
            #endregion



            conn_db.Close();
            return LocalTable;
        }



        public DataTable GetSverkaDay(ExcelSverkaPeriod prm_, out Returns ret, string Nzp_user)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт ExcelRep.GetSverkaDay");

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();
            string str = "";

            try
            {
                ExecSQL(conn_db, " Drop table t_spis ", false);
                ret = ExecSQL(conn_db,
                        " Create temp table t_spis " +
                        " ( payer  char(200), " +
                        "   geu    char(60), " +
                        "   area    char(40), " +
                        "   nzp_area  char(60), " +
                        "   is_post  char(60), " +
                        "   prefix_ls  char(3), " +
                        "   count_kvit  char(60), " +
                        "   g_sum_ls char(60), " +
                        "   sum_ur char(60), " +
                        "   penya char(60), " +
                        "   komiss char(60), " +
                        "   tot char(60)," +
                        "   dat_pack char(60), " +
                        "   dat_uchet char(60) " +
                        " ) " + sUnlogTempTable, true);
                if (Constants.Trace) Utility.ClassLog.WriteLog("создание:\n" + " Create temp table t_spis " +
                        " ( payer  char(200), " +
                        "   geu    char(60), " +
                        "   area    char(40), " +
                        "   nzp_area  char(60), " +
                        "   is_post  char(60), " +
                        "   prefix_ls  char(3), " +
                        "   count_kvit  char(60), " +
                        "   g_sum_ls char(60), " +
                        "   sum_ur char(60), " +
                        "   penya char(60), " +
                        "   komiss char(60), " +
                        "   tot char(60)," +
                        "   dat_pack char(60), " +
                        "   dat_uchet char(60) " +
                        " ) " + sUnlogTempTable);

                if (!ret.result)
                {
                    return null;
                }

                string where_adr = "";
                if (prm_.RolesVal != null)
                {
                    if (prm_.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in prm_.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and g.nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }
                string field_dat = "";
                if (prm_.search_date == 1) field_dat = "dat_uchet";
                else field_dat = "dat_pack";

                string swhere = "";
                DateTime d1, d2;
                DateTime.TryParse(prm_.dat_s, out d1);

#if PG
                string table_name = " left outer join " + Points.Pref + "_kernel.s_bank sb inner join " + Points.Pref + "_kernel.s_payer p on sb.nzp_payer = p.nzp_payer on b.nzp_bank = sb.nzp_bank ";
#else
                string table_name = ", outer (" + Points.Pref + "_kernel:s_bank sb, " + Points.Pref + "_kernel:s_payer p) ";
#endif
                if (prm_.dat_po != "")
                {
                    DateTime.TryParse(prm_.dat_po, out d2);
                    swhere = " b." + field_dat + " between mdy(" + d1.Month + "," + d1.Day + "," + d1.Year + ")" + " and mdy(" + d2.Month + "," + d2.Day + "," + d2.Year + ") ";

                    if (prm_.rschet != "")//Расчетный счет
                    {
#if PG
                        table_name = ", " + Points.Pref + "_kernel.s_bank sb, " + Points.Pref + "_kernel.s_payer p ";
#else
                        table_name = ", " + Points.Pref + "_kernel:s_bank sb, " + Points.Pref + "_kernel:s_payer p ";
#endif
                        //  swhere += " and substr(k.pkod,1,3) = " + prm_.rschet + " ";
                        if (prm_.rschet == "0") swhere += " and (" + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'0') = '0' or " + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'') = '') ";
                        else swhere += " and " + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'') = " + Utils.EStrNull(prm_.rschet) + " ";

                    }

                    if (prm_.nzp_payer != "")//Место формирования
                    {
#if PG
                        table_name = ", " + Points.Pref + "_kernel.s_bank sb, " + Points.Pref + "_kernel.s_payer p ";
#else
                        table_name = ", " + Points.Pref + "_kernel:s_bank sb, " + Points.Pref + "_kernel:s_payer p ";
#endif
                        swhere += " and p.nzp_payer = " + prm_.nzp_payer + " ";
                    }
                }
                else
                {
                    swhere = " b." + field_dat + " = mdy(" + d1.Month + "," + d1.Day + "," + d1.Year + ")";
                }
                DateTime dt;
                DateTime.TryParse(prm_.dat_s, out dt);
                for (var i = dt.Year - 1; i <= dt.Year + 1; i++)
                {
                    string pref = Points.Pref + "_fin_" + (i - 2000).ToString("00");
#if PG
                    if (!TempTableInWebCashe(conn_db, pref + ".pack_ls")) continue;
                    if (!TempTableInWebCashe(conn_db, pref + ".pack")) continue;
#else
                    if (!TempTableInWebCashe(conn_db, pref + ":pack_ls")) continue;
                    if (!TempTableInWebCashe(conn_db, pref + ":pack")) continue;
#endif

                    #region Выборка
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_spis (is_post, payer,area,geu,prefix_ls, dat_pack,dat_uchet," +
                        " nzp_area, count_kvit,g_sum_ls,sum_ur,penya,komiss,tot) ");
#if PG
                    sql.Append("select (case when sb.nzp_payer = 80001 then 1 else 2 end) as is_post, (case when payer is null then ( case when b.nzp_bank is not null then 'Кассы' else (case when kod_sum=50 or kod_sum=49 then 'Сторонние оплаты на счет УК и ПУ' when kod_sum=33 then 'Сторонние оплаты на счет РЦ' end ) end)else payer end) as payer, ");
                    //sql.Append("area,geu, (case when sb.nzp_payer = 80001 then '408' else substring(k.pkod from 1 to 3) end) as prefix_ls,dat_pack,b.dat_uchet,k.nzp_area, count(*) as count_kvit, ");
                    sql.Append("area,geu, substr(a.pkod" + sConvToVarChar + ",1,3) as prefix_ls,dat_pack,b.dat_uchet,k.nzp_area, count(*) as count_kvit, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls, sum(0) as sum_ur, sum(0) as penya, sum(0) as komiss, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls  ");
                    sql.Append("from  " + pref + ".pack  b ");
                    sql.Append(table_name);
                    sql.Append(", " + pref + ".pack_ls a, " + Points.Pref + "_data.kvar k ");
                    sql.Append(", " + Points.Pref + "_data.s_geu g , " + Points.Pref + "_data.s_area ar ");
                    sql.Append("where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls and k.nzp_geu=g.nzp_geu and k.nzp_area=ar.nzp_area  ");
                    sql.Append("group by 1,2,3,4,5,6,7,8, sb.nzp_payer ");

#else
                    sql.Append("select (case when sb.nzp_payer = 80001 then 1 else 2 end)||'' as is_post, (case when payer is null then 'Не определен в центральном банке' else payer end)||'' as payer, ");
                    //   sql.Append("area||'',geu||'', (case when sb.nzp_payer = 80001 then '408' else substr(k.pkod,1,3) end)||'' as prefix_ls,dat_pack||'' dat_pack,b.dat_uchet||'' dat_uchet,k.nzp_area||'' nzp_area, count(*)||'' as count_kvit, ");
                    sql.Append("area||'',geu||'', substr(a.pkod,1,3) ||'' as prefix_ls,dat_pack||'' dat_pack,b.dat_uchet||'' dat_uchet,k.nzp_area||'' nzp_area, count(*)||'' as count_kvit, ");
                    sql.Append("sum(g_sum_ls)||'' as g_sum_ls, sum(0)||'' as sum_ur, sum(0)||'' as penya, sum(0)||'' as komiss, ");
                    sql.Append("sum(g_sum_ls)||'' as g_sum_ls  ");
                    sql.Append("from  " + pref + ":pack  b , " + pref + ":pack_ls a, " + Points.Pref + "_data:kvar k ");
                    sql.Append(", " + Points.Pref + "_data:s_geu g , " + Points.Pref + "_data:s_area ar ");
                    sql.Append(table_name);

                    sql.Append("where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls and k.nzp_geu=g.nzp_geu and k.nzp_area=ar.nzp_area  ");
                    sql.Append("and b.nzp_bank=sb.nzp_bank and sb.nzp_payer = p.nzp_payer ");
                    sql.Append("group by 1,2,3,4,5,6,7,8, sb.nzp_payer ");
#endif
                    if (Constants.Trace) Utility.ClassLog.WriteLog("!!! Запрос:\n" + sql.ToString());


                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    sql.Remove(0, sql.Length);
                    if (!ret.result)
                    {
                        return null;
                    }
                }
                ExecSQL(conn_db, " Create index ix_t_spis on t_spis (payer,geu) ", true);
#if PG
                ExecSQL(conn_db, " analyze t_spis ", true);
#else
                ExecSQL(conn_db, " Update statistics for table t_spis ", true);
#endif


                if (Constants.Trace) Utility.ClassLog.WriteLog("!!! Запрос:\n" + "select payer || '' payer, geu || '' geu, area || '' area,prefix_ls " +
                    "|| '' prefix_ls, dat_pack || '' dat_pack,dat_uchet || '' dat_uchet,nzp_area || '' nzp_area, " +
                    "count_kvit || '' count_kvit, g_sum_ls || '' g_sum_ls, sum_ur || '' sum_ur, penya || '' penya, " +
                    "komiss || '' komiss, tot || '' tot " +
 " from t_spis order by is_post, payer, area, geu, prefix_ls, " + field_dat);

                ret = ExecRead(conn_db, out reader,

#if PG
 "select payer, geu, coalesce(nzp_area,'0'), area, coalesce(prefix_ls,'0'), coalesce(count_kvit,'0'), coalesce(g_sum_ls,'0'), coalesce(sum_ur,'0'), coalesce(penya,'0'), coalesce(komiss,'0'), coalesce(tot,'0'),dat_pack,dat_uchet " +
#else
 "select payer || '' payer, geu || '' geu,'0' || nzp_area || '' nzp_area, area || '' area,prefix_ls || '' prefix_ls, '0' || round(count_kvit) || '' count_kvit, g_sum_ls || '' g_sum_ls, sum_ur || '' sum_ur, penya || '' penya, komiss || '' komiss, tot || '' tot, dat_pack || '' dat_pack, dat_uchet || '' dat_uchet " +
#endif

 " from t_spis order by is_post, payer, area, geu, prefix_ls, " + field_dat
                    , true);
                if (!ret.result)
                {
                    return null;
                }

                //заполнение DataTable

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                //   Data_Table.Load(reader, LoadOption.OverwriteChanges, FillErrorHandler);                    
                int ind = 0;
                List<Pack_ls> list = new List<Pack_ls>();
                while (reader.Read())
                {
                    Pack_ls p = new Pack_ls();
                    //str = reader.GetString(0) + "; ";
                    p.payer = !reader.IsDBNull(0) ? reader.GetString(0).Trim() : "";
                 //   str += reader.GetValue(1) + "; ";
                    p.geu = !reader.IsDBNull(1) ? reader.GetString(1).Trim() : "";
                  //  str += reader.GetString(2) + "; ";
                    p.nzp_area = !reader.IsDBNull(2) ? Convert.ToInt32(reader.GetString(2).Trim()) : 0;
                 //   str += reader.GetString(3) + "; ";
                    p.area = !reader.IsDBNull(3) ? reader.GetString(3).Trim() : "";
                //    str += reader.GetString(4) + "; ";
                    p.pref = !reader.IsDBNull(4) ? reader.GetString(4).Trim() : "";
                    if (p.pref == "") p.pref = "Р/с не определен";
              //      str += reader.GetString(5) + "; ";
                    p.count_kv = !reader.IsDBNull(5) ? Convert.ToInt32(reader.GetString(5).Trim()) : 0;
             //       str += reader.GetValue(6) + "; ";
                    p.g_sum_ls = !reader.IsDBNull(6) ? Convert.ToDecimal(reader.GetString(6).Trim()) : 0;
              //      str += reader.GetString(7) + "; ";
                    p.sum_ls = !reader.IsDBNull(7) ? Convert.ToDecimal(reader.GetString(7).Trim()) : 0;
            //        str += reader.GetString(8) + "; ";
                    p.sum_peni = !reader.IsDBNull(8) ? Convert.ToDecimal(reader.GetString(8).Trim()) : 0;
             //       str += reader.GetString(9) + "; ";
                    p.sum_not_distr = !reader.IsDBNull(9) ? Convert.ToDecimal(reader.GetString(9).Trim()) : 0;
           //         str += reader.GetString(10) + "; ";
                    p.sum_nach = !reader.IsDBNull(10) ? Convert.ToDecimal(reader.GetString(10).Trim()) : 0;
          //          str += reader.GetString(11) + "; ";
                    p.dat_pack = !reader.IsDBNull(11) ? Convert.ToDateTime(reader.GetString(11).Trim()).ToShortDateString() : "";
          //          str += reader.GetString(12) + "; ";
                    p.dat_uchet = !reader.IsDBNull(12) ? Convert.ToDateTime(reader.GetString(12).Trim()).ToShortDateString() : "";
                    ind++;
                    if (Constants.Trace) Utility.ClassLog.WriteLog("index: " + ind + " " + p.ToString());
                    list.Add(p);
                }

                if (Constants.Trace) Utility.ClassLog.WriteLog("!!! Data_Table:\n");
                Data_Table = new DataTable();
                Data_Table.Columns.Add("payer", typeof(string));
                Data_Table.Columns.Add("geu", typeof(string));
                Data_Table.Columns.Add("nzp_area", typeof(int));
                Data_Table.Columns.Add("area", typeof(string));
                Data_Table.Columns.Add("prefix_ls", typeof(string));
                Data_Table.Columns.Add("count_kvit", typeof(int));
                Data_Table.Columns.Add("g_sum_ls", typeof(decimal));
                Data_Table.Columns.Add("sum_ur", typeof(decimal));
                Data_Table.Columns.Add("penya", typeof(decimal));
                Data_Table.Columns.Add("komiss", typeof(decimal));
                Data_Table.Columns.Add("tot", typeof(decimal));
                Data_Table.Columns.Add("dat_pack", typeof(string));
                Data_Table.Columns.Add("dat_uchet", typeof(string));


                foreach (Pack_ls p in list)
                {
                    DataRow row = Data_Table.NewRow();

                    row["payer"] = p.payer;
                    row["geu"] = p.geu;
                    row["nzp_area"] = p.nzp_area;
                    row["area"] = p.area;
                    row["prefix_ls"] = p.pref;
                    row["count_kvit"] = p.count_kv;
                    row["g_sum_ls"] = p.g_sum_ls;
                    row["sum_ur"] = p.sum_ls;
                    row["penya"] = p.sum_peni;
                    row["komiss"] = p.sum_not_distr;
                    row["tot"] = p.sum_nach;
                    row["dat_pack"] = p.dat_pack;
                    row["dat_uchet"] = p.dat_uchet;

                    Data_Table.Rows.Add(row);
                }

                if (Constants.Trace) Utility.ClassLog.WriteLog("!!! end Data_Table:\n");

            }
            catch (Exception ex)
            {
                if (Constants.Trace) Utility.ClassLog.WriteLog(str);
                MonitorLog.WriteLog("!!! GetSverkaDay:\n" + ex.ToString() + "\n" + str, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                conn_db.Close();
            }
                    #endregion
            if (Constants.Trace) Utility.ClassLog.WriteLog("end ExcelRep.GetSverkaDay");
            return Data_Table;
        }

        public DataTable GetDataSaldoPoPerechisl(MoneyDistrib finder, out Returns ret, string Nzp_user)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт ExcelRep.GetDataSaldoPoPerechisl");

            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();



            string str = "";

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            DbCharge db = new DbCharge();
            try
            {

                finder.rows = 100;
                List<MoneyDistrib> list = db.GetMoneyDistribDom(finder, conn_web, out ret);
                if (ret.result)
                {
                    finder.skip += finder.rows;
                    List<MoneyDistrib> spis2 = new List<MoneyDistrib>();
                    while (finder.skip < ret.tag && ret.result)
                    {
                        spis2 = db.GetMoneyDistribDom(finder, conn_web, out ret);
                        if (ret.result) list.AddRange(spis2);
                        finder.skip += finder.rows;
                    }
                }

                if (list == null) if (!ret.result) return null;
                //заполнение DataTable

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                //   Data_Table.Load(reader, LoadOption.OverwriteChanges, FillErrorHandler);                    


                if (Constants.Trace) Utility.ClassLog.WriteLog("!!! Data_Table:\n");
                Data_Table = new DataTable();
                Data_Table.TableName = "Q_master";
                Data_Table.Columns.Add("payer", typeof(string));
                Data_Table.Columns.Add("nzp_payer", typeof(string));
                Data_Table.Columns.Add("bank", typeof(string));
                Data_Table.Columns.Add("nzp_bank", typeof(string));
                Data_Table.Columns.Add("nzp_area", typeof(string));
                Data_Table.Columns.Add("area", typeof(string));
                Data_Table.Columns.Add("service", typeof(string));
                Data_Table.Columns.Add("nzp_serv", typeof(string));
                Data_Table.Columns.Add("num", typeof(string));
                Data_Table.Columns.Add("sum_in", typeof(string));
                Data_Table.Columns.Add("sum_rasp", typeof(string));
                Data_Table.Columns.Add("sum_ud", typeof(string));
                Data_Table.Columns.Add("sum_naud", typeof(string));
                Data_Table.Columns.Add("sum_reval", typeof(string));
                Data_Table.Columns.Add("sum_charge", typeof(string));
                Data_Table.Columns.Add("sum_send", typeof(string));
                Data_Table.Columns.Add("sum_out", typeof(string));
                Data_Table.Columns.Add("dat_oper", typeof(string));
                Data_Table.Columns.Add("adr", typeof(string));
                Data_Table.Columns.Add("nzp_dom", typeof(string));


                foreach (MoneyDistrib p in list)
                {
                    DataRow row = Data_Table.NewRow();

                    row["payer"] = p.payer.ToString();
                    row["nzp_payer"] = p.nzp_payer.ToString();
                    row["bank"] = p.bank.ToString();
                    row["nzp_bank"] = p.nzp_bank.ToString();
                    row["nzp_area"] = p.nzp_area.ToString();
                    row["area"] = p.area.ToString();
                    row["service"] = p.service.ToString();
                    row["nzp_serv"] = p.nzp_serv.ToString();
                    row["num"] = p.num.ToString();
                    row["sum_in"] = p.sum_in.ToString();
                    row["sum_rasp"] = p.sum_rasp.ToString();
                    row["sum_ud"] = p.sum_ud.ToString();
                    row["sum_naud"] = p.sum_naud.ToString();
                    row["sum_reval"] = p.sum_reval.ToString();
                    row["sum_charge"] = p.sum_charge.ToString();
                    row["sum_send"] = p.sum_send.ToString();
                    row["sum_out"] = p.sum_out.ToString();
                    row["dat_oper"] = p.dat_oper.ToString();
                    row["adr"] = p.adr.ToString();
                    row["nzp_dom"] = p.nzp_dom.ToString();

                    Data_Table.Rows.Add(row);
                }

                if (Constants.Trace) Utility.ClassLog.WriteLog("!!! end Data_Table:\n");

            }
            catch (Exception ex)
            {
                if (Constants.Trace) Utility.ClassLog.WriteLog(str);
                MonitorLog.WriteLog("!!! GetDataSaldoPoPerechisl:\n" + ex.ToString() + "\n" + str, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                db.Close();
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                conn_web.Close();
            }

            if (Constants.Trace) Utility.ClassLog.WriteLog("end ExcelRep.GetDataSaldoPoPerechisl");
            return Data_Table;
        }

        static void FillErrorHandler(object sender, FillErrorEventArgs e)
        {
            // You can use the e.Errors value to determine exactly what
            // went wrong.
            if (e.Errors.GetType() == typeof(System.FormatException))
            {
                MonitorLog.WriteLog("Error when attempting to update the value: " + e.Values[0] + " \n " + e.ToString(), MonitorLog.typelog.Error, true);
            }

            // Setting e.Continue to True tells the Load
            // method to continue trying. Setting it to False
            // indicates that an error has occurred, and the 
            // Load method raises the exception that got 
            // you here.
            e.Continue = false;
        }

        public DataTable GetSverkaMonth(ExcelSverkaPeriod prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

            ret = Utils.InitReturns();

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            MyDataReader reader;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            ExecSQL(conn_db, " Drop table t_spis_sm ", false);
            ret = ExecSQL(conn_db,
                    " Create temp table t_spis_sm " +
                    " ( payer  char(200), " +
                    "   geu    char(60), " +
                    "   prefix_ls  char(3), " +
                    "   count_kvit  char(60), " +
                    "   g_sum_ls char(60), " +
                    "   sum_ur char(60)," +
                    "   penya char(60), " +
                    "   komiss char(60), " +
                    "   tot char(60)," +
                    "   dat_ char(60) " +
                     " )" + sUnlogTempTable, true);
            if (!ret.result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            string where_adr = "";
            if (prm_.add_geu_column == 1)
            {
                if (prm_.RolesVal != null)
                {
                    if (prm_.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in prm_.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and g.nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }
            }

            string field_dat = "";
            if (prm_.search_date == 1) field_dat = "dat_uchet";
            else field_dat = "dat_pack";

            string swhere = "";
            DateTime d1;
            DateTime.TryParse(prm_.dat_s, out d1);
            swhere = " b." + field_dat + " between mdy(" + d1.Month + "," + 1 + "," + d1.Year +
                ") and mdy(" + d1.Month + "," + DateTime.DaysInMonth(d1.Year, d1.Month) + "," + d1.Year + ")";

            if (prm_.rschet != "")//Расчетный счет
            {
#if PG
                if (prm_.rschet == "0") swhere += " and (" + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'0') = '0' or " + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'') = '') ";
                else swhere += " and " + sNvlWord + "(substr(a.pkod" + sConvToVarChar + ",1,3),'') = " + Utils.EStrNull(prm_.rschet) + " ";
#else
                if (prm_.rschet == "0") swhere += " and (" + sNvlWord + "(substr(a.pkod,1,3),0) = 0 or " + sNvlWord + "(substr(a.pkod,1,3),'') = '') ";
                else swhere += " and " + sNvlWord + "(substr(a.pkod,1,3),'') = " + prm_.rschet + " ";
#endif
            }
#if PG
            string table_name = " left outer join " + Points.Pref + "_kernel.s_bank sb inner join " + Points.Pref + "_kernel.s_payer p on p.nzp_payer = sb.nzp_payer on b.nzp_bank = sb.nzp_bank ";
#else
            string table_name = ", outer (" + Points.Pref + "_kernel:s_bank sb, " + Points.Pref + "_kernel:s_payer p) ";
#endif
            if (prm_.nzp_payer != "")//Место формирования
            {
                table_name = ", " + Points.Pref + "_kernel" + tableDelimiter + "s_bank sb, " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p ";
                swhere += " and p.nzp_payer = " + prm_.nzp_payer + " ";
#if PG
                swhere += " and b.nzp_bank=sb.nzp_bank and sb.nzp_payer = p.nzp_payer ";
#endif
            }

            DateTime dt;
            DateTime.TryParse(prm_.dat_s, out dt);
            for (var i = dt.Year - 1; i <= dt.Year + 1; i++)
            {
                string pref = Points.Pref + "_fin_" + (i - 2000).ToString("00");
                if (!TempTableInWebCashe(conn_db, pref + tableDelimiter + "pack_ls")) continue;
                if (!TempTableInWebCashe(conn_db, pref + tableDelimiter + "pack")) continue;

                #region Выборка
                sql.Remove(0, sql.Length);

                if (prm_.add_geu_column == 1)
                {
#if PG
                    sql.Append("insert into t_spis_sm (payer,prefix_ls,dat_,geu," +
                        " count_kvit,g_sum_ls,sum_ur,penya,komiss,tot) ");
                    sql.Append("select (case when payer is null then ( case when (a.kod_sum=49 or a.kod_sum=50) then 'Стороннике оплаты на счет РЦ' when a.kod_sum=33 then'Сторонние оплаты на счет УК и ПУ' else 'Кассы' end) else payer end) as payer, ");
                    //sql.Append("select (case when payer is null then 'Не определен в центральном банке' else payer end) as payer, ");
                    //   sql.Append("(case when sb.nzp_payer = 80001 then '408' else substr(a.pkod,1,3) end) as prefix_ls,b." + field_dat + ",geu, count(*) as count_kvit, ");
                    sql.Append("substr(CAST(a.pkod as varchar),1,3) as prefix_ls,b.dat_pack,geu, count(*) as count_kvit, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls, sum(0) as sum_ur, sum(0) as penya, sum(0) as komiss, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls  ");
                    sql.Append("from  " + Points.Pref + "_data.s_geu g , " + pref + ".pack_ls a, " + Points.Pref + "_data.kvar k ");
                    sql.Append(", " + pref + ".pack  b  ");
                    sql.Append(table_name);
                    sql.Append(" where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls and k.nzp_geu=g.nzp_geu  ");
                    sql.Append("group by 1,2,3,4  ");
#else
                    sql.Append("insert into t_spis_sm (payer,prefix_ls,dat_,geu," +
                        " count_kvit,g_sum_ls,sum_ur,penya,komiss,tot) ");
                    sql.Append("select (case when payer is null then ( case when (a.kod_sum=49 or a.kod_sum=50) then 'Стороннике оплаты на счет РЦ' when a.kod_sum=33 then'Сторонние оплаты на счет УК и ПУ' else 'Кассы' end) else payer end) as payer, ");
                    // sql.Append("select (case when payer is null then (case when pack_type=10 then 'Стороннике оплаты на счет РЦ' else 'Сторонние оплаты на счет УК и ПУ' end) else payer end) as payer, ");
                    //sql.Append("select (case when payer is null then 'Не определен в центральном банке' else payer end) as payer, ");
                    //     sql.Append("(case when sb.nzp_payer = 80001 then '408' else substr(a.pkod,1,3) end) as prefix_ls,b." + field_dat + ",geu, count(*) as count_kvit, ");
                    sql.Append("substr(a.pkod,1,3) as prefix_ls,b.dat_pack,geu, count(*) as count_kvit, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls, sum(0) as sum_ur, sum(0) as penya, sum(0) as komiss, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls  ");
                    sql.Append("from  " + pref + ":pack  b , " + pref + ":pack_ls a, " + Points.Pref + "_data:kvar k ");
                    sql.Append(", " + Points.Pref + "_data:s_geu g  ");
                    sql.Append(table_name);
                    sql.Append("where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls and k.nzp_geu=g.nzp_geu  ");
                    sql.Append("and b.nzp_bank=sb.nzp_bank and sb.nzp_payer = p.nzp_payer ");
                    sql.Append("group by 1,2,3,4  ");
#endif
                }
                else
                {
                    sql.Append("insert into t_spis_sm (payer,prefix_ls,dat_," +
                       " count_kvit,g_sum_ls,sum_ur,penya,komiss,tot) ");
#if PG                
                    sql.Append("select (case when payer is null then ( case when a.kod_sum=50 then 'Стороннике оплаты на счет РЦ' when a.kod_sum=33 then'Сторонние оплаты на счет УК и ПУ' else 'Кассы' end) else payer end) as payer, ");
                    //sql.Append("select (case when payer is null then 'Не определен в центральном банке' else payer end) as payer, ");
                    //   sql.Append("(case when sb.nzp_payer = 80001 then '408' else substr(a.pkod,1,3) end) as prefix_ls,b." + field_dat + ", count(*) as count_kvit, ");
                    sql.Append("substr(CAST(a.pkod as varchar),1,3) as prefix_ls,b.dat_pack, count(*) as count_kvit, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls, sum(0) as sum_ur, sum(0) as penya, sum(0) as komiss, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls  ");
                    sql.Append("from  " + pref + ".pack_ls a, " + Points.Pref + "_data.kvar k , " + pref + ".pack  b  ");
                    sql.Append(table_name);
                    sql.Append(" where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls   ");
#else
                   // sql.Append("select (case when payer is null then 'Не определен в центральном банке' else payer end) as payer, ");
                    sql.Append("select (case when payer is null then ( case when a.kod_sum=50 then 'Стороннике оплаты на счет РЦ' when a.kod_sum=33 then'Сторонние оплаты на счет УК и ПУ' else 'Кассы' end) else payer end) as payer, ");
                    //   sql.Append("(case when sb.nzp_payer = 80001 then '408' else substr(a.pkod,1,3) end) as prefix_ls,b." + field_dat + ", count(*) as count_kvit, ");
                    sql.Append("substr(a.pkod,1,3) as prefix_ls,b.dat_pack, count(*) as count_kvit, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls, sum(0) as sum_ur, sum(0) as penya, sum(0) as komiss, ");
                    sql.Append("sum(g_sum_ls) as g_sum_ls  ");
                    sql.Append("from  " + pref + ":pack  b , " + pref + ":pack_ls a, " + Points.Pref + "_data:kvar k ");
                    sql.Append(table_name);
                    sql.Append("where " + swhere + " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls   ");
                    sql.Append("and b.nzp_bank=sb.nzp_bank and sb.nzp_payer = p.nzp_payer ");
#endif
                    sql.Append("group by 1,2,3  ");
                }
                //   MonitorLog.WriteLog("Запрос " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
            }
            if (prm_.add_geu_column == 1) ExecSQL(conn_db, " Create index ix_t_spis_sm on t_spis_sm (payer,geu) ", true);
            else ExecSQL(conn_db, " Create index ix_t_spis_sm on t_spis_sm (payer) ", true);
#if PG
            ExecSQL(conn_db, " analyze t_spis_sm ", true);
#else
            ExecSQL(conn_db, " Update statistics for table t_spis_sm ", true);
#endif

            string cols = "";
            if (prm_.add_geu_column == 1) cols = ",geu ";
            else cols = "";
            //    MonitorLog.WriteLog("Запрос " + "select payer || '' payer,  prefix_ls || '' prefix_ls, dat_ || '' dat_, geu || '' geu,round(count_kvit) || '' count_kvit , g_sum_ls || '' g_sum_ls, sum_ur || '' sum_ur, penya || '' penya, komiss || '' komiss, tot || '' tot " +
            //        " from t_spis_sm order by payer, prefix_ls,dat_ " + cols, MonitorLog.typelog.Error, 20, 201, true);
            if (!ExecRead(conn_db, out reader,
                "select payer || '' payer,  prefix_ls || '' prefix_ls, dat_ || '' dat_, geu || '' geu,count_kvit || '' count_kvit , g_sum_ls || '' g_sum_ls, sum_ur || '' sum_ur, penya || '' penya, komiss || '' komiss, tot || '' tot " +
                " from t_spis_sm order by payer, prefix_ls,dat_ " + cols
                , true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }


            try
            {
                if (reader != null)
                {
                    int i = 0, k = 0;
                    string str_payer = "", str_geu = "", str_prefix_ls = "", str_count_kvit = "",
                        str_g_sum_ls = "", str_sum_ur = "", str_penya = "", str_komiss = "", str_tot = "", str_dat_ = "";
                    try
                    {
                        //заполнение DataTable
                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;


                        //Data_Table.Load(reader, LoadOption.OverwriteChanges);

                        List<Pack_ls> list = new List<Pack_ls>();

                        while (reader.Read())
                        {
                            Pack_ls p = new Pack_ls();
                            str_payer = str_geu = str_prefix_ls = str_count_kvit =
                            str_g_sum_ls = str_sum_ur = str_penya = str_komiss = str_tot = str_dat_ = "";
                            i++;

                            k = 1;
                            str_payer = "list: " + reader["payer"].ToString();
                            p.payer = reader["payer"] != DBNull.Value ? Convert.ToString(reader["payer"]).Trim() : "";
                            k++; str_geu = reader["geu"].ToString();
                            p.geu = reader["geu"] != DBNull.Value ? Convert.ToString(reader["geu"]).Trim() : "";
                            k++; str_prefix_ls = reader["prefix_ls"].ToString();
                            if (str_prefix_ls.Trim() == "") str_prefix_ls = "Р/с не определен";
                            p.pref = reader["prefix_ls"] != DBNull.Value ? Convert.ToString(reader["prefix_ls"]).Trim() : "";
                            if (p.pref.Trim() == "") p.pref = "Р/с не определен";
                            k++; str_count_kvit = reader["count_kvit"].ToString();
                            p.count_kv = reader["count_kvit"] != DBNull.Value ? Convert.ToInt32(Convert.ToString(reader["count_kvit"]).Trim()) : 0;
                            k++; str_g_sum_ls = reader["g_sum_ls"].ToString();
                            p.g_sum_ls = reader["g_sum_ls"] != DBNull.Value ? Convert.ToDecimal(Convert.ToString(reader["g_sum_ls"]).Trim()) : 0;
                            k++; str_sum_ur = reader["sum_ur"].ToString();
                            p.sum_ls = reader["sum_ur"] != DBNull.Value ? Convert.ToDecimal(Convert.ToString(reader["sum_ur"]).Trim()) : 0;
                            k++; str_penya = reader["penya"].ToString();
                            p.sum_peni = reader["penya"] != DBNull.Value ? Convert.ToDecimal(Convert.ToString(reader["penya"]).Trim()) : 0;
                            k++; str_komiss = reader["komiss"].ToString();
                            p.sum_not_distr = reader["komiss"] != DBNull.Value ? Convert.ToDecimal(Convert.ToString(reader["komiss"]).Trim()) : 0;
                            k++; str_tot = reader["tot"].ToString();
                            p.sum_nach = reader["tot"] != DBNull.Value ? Convert.ToDecimal(Convert.ToString(reader["tot"]).Trim()) : 0;
                            k++; str_dat_ = reader["dat_"].ToString();
                            p.dat_vvod = reader["dat_"] != DBNull.Value ? Convert.ToDateTime(Convert.ToString(reader["dat_"]).Trim()).ToShortDateString() : "";
                            k++;
                            list.Add(p);
                        }

                        Data_Table = new DataTable();
                        Data_Table.Columns.Add("payer", typeof(string));
                        Data_Table.Columns.Add("geu", typeof(string));
                        Data_Table.Columns.Add("prefix_ls", typeof(string));
                        Data_Table.Columns.Add("count_kvit", typeof(int));
                        Data_Table.Columns.Add("g_sum_ls", typeof(decimal));
                        Data_Table.Columns.Add("sum_ur", typeof(decimal));
                        Data_Table.Columns.Add("penya", typeof(decimal));
                        Data_Table.Columns.Add("komiss", typeof(decimal));
                        Data_Table.Columns.Add("tot", typeof(decimal));
                        Data_Table.Columns.Add("dat_", typeof(string));


                        foreach (Pack_ls p in list)
                        {
                            str_payer = str_geu = str_prefix_ls = str_count_kvit =
                            str_g_sum_ls = str_sum_ur = str_penya = str_komiss = str_tot = str_dat_ = "";
                            i++;
                            DataRow row = Data_Table.NewRow();
                            k = 1;
                            str_payer = p.payer;
                            row["payer"] = p.payer;
                            k++; str_geu = p.geu;
                            row["geu"] = p.geu;
                            k++; str_prefix_ls = p.pref;
                            row["prefix_ls"] = p.pref;
                            k++; str_count_kvit = p.count_kv.ToString();
                            row["count_kvit"] = p.count_kv;
                            k++; str_g_sum_ls = p.g_sum_ls.ToString();
                            row["g_sum_ls"] = p.g_sum_ls;
                            k++; str_sum_ur = p.sum_ls.ToString();
                            row["sum_ur"] = p.sum_ls;
                            k++; str_penya = p.sum_peni.ToString();
                            row["penya"] = p.sum_peni;
                            k++; str_komiss = p.sum_not_distr.ToString();
                            row["komiss"] = p.sum_not_distr;
                            k++; str_tot = p.sum_nach.ToString();
                            row["tot"] = p.sum_nach;
                            k++; str_dat_ = p.dat_vvod;
                            row["dat_"] = p.dat_vvod;
                            k++;
                            Data_Table.Rows.Add(row);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message + "\n row =" + i + " col = " + k +
                            "\n payer=" + str_payer +
                            "\n geu=" + str_geu +
                            "\n prefix_ls=" + str_prefix_ls +
                            "\n count_kvit=" + str_count_kvit +
                            "\n g_sum_ls=" + str_g_sum_ls +
                            "\n sum_ur=" + str_sum_ur +
                            "\n penya=" + str_penya +
                            "\n komiss=" + str_komiss +
                            "\n tot=" + str_tot +
                            "\n dat_=" + str_dat_, MonitorLog.typelog.Error, true);
                        reader.Close();
                        conn_db.Close();
                        return null;
                    }
                    if (reader != null) reader.Close();
                    return Data_Table;


                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    conn_db.Close(); //закрыть соединение с основной базой     
                    return null;
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

                if (reader != null)
                {
                    reader.Close();
                }

                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
                #endregion


        }

        #region универсальный сервер отчетов

        /// <summary>
        /// функция получения данных справочников для универсальной системы отчетов
        /// </summary>
        /// <param name="idDicts">список идентификаторов справочников</param>
        /// <param name="loadDictsData">признак загрузки данных для справочников</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Dict> GetReportDicts(List<int> idDicts, bool loadDictsData, out Returns ret)
        {
            IDataReader reader = null;
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;

            try
            {
                ret = Utils.InitReturns();
                StringBuilder sql = new StringBuilder();
                //переменные для обращения к web базе из основной БД
                string database = "";
                string server = "";

                if (idDicts.Count > 0)
                {
                    #region Подключение к БД

                    conn_web = GetConnection(Constants.cons_Webdata);
                    ret = OpenDb(conn_web, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("GetReportDicts : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                        conn_web.Close();
                        ret.result = false;
                        return null;
                    }
                    else
                    {
                        database = conn_web.Database;
                        server = DBManager.getServer(conn_web);
                        conn_web.Close();
                    }

                    conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("GetReportDicts : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    #endregion

                    //итоговый справочник
                    List<Dict> dicts = new List<Dict>();

                    #region получение данных о справочниках

#if PG
                    sql.Append("SELECT * FROM " + database + ". report_params_catalog ");
#else
                    sql.Append("SELECT * FROM " + database + "@" + server + ": report_params_catalog ");
#endif
                    sql.Append("WHERE nzp_cat IN (" + String.Join(",", idDicts.Select(x => x.ToString()).ToArray()) + ") AND short_name IS NOT NULL;");

                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки в процедуре GetReportDicts " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            string shortName = "";
                            int id = 0;
                            string name = "";
                            if (reader["short_name"].ToString().Trim() != string.Empty)
                            {
                                shortName = reader["short_name"].ToString().Trim();
                                if (reader["nzp_cat"] != DBNull.Value) id = Convert.ToInt32(reader["nzp_cat"]);
                                if (reader["name"] != DBNull.Value) name = reader["name"].ToString().Trim();
                                dicts.Add(new Dict() { id = id, name = name, shortName = shortName });
                            }
                        }
                    }

                    #endregion

                    if (loadDictsData)
                    {
                        foreach (int idDict in idDicts)
                        {
                            //получение данных для каждого справочника
                            switch (idDict)
                            {
                                #region справочник лет
                                case 1:
                                    {
                                        List<DictionaryItem> dictValues = new List<DictionaryItem>();
                                        dictValues.Add(new DictionaryItem() { key = 2006, value = "2006" });
                                        dictValues.Add(new DictionaryItem() { key = 2007, value = "2007" });
                                        dictValues.Add(new DictionaryItem() { key = 2008, value = "2008" });
                                        dictValues.Add(new DictionaryItem() { key = 2009, value = "2009" });
                                        dictValues.Add(new DictionaryItem() { key = 2010, value = "2010" });
                                        dictValues.Add(new DictionaryItem() { key = 2011, value = "2011" });
                                        dictValues.Add(new DictionaryItem() { key = 2012, value = "2012" });
                                        dictValues.Add(new DictionaryItem() { key = 2013, value = "2013" });
                                        var obj = dicts.FirstOrDefault(x => x.id == idDict);
                                        if (obj != null)
                                        {
                                            obj.items = dictValues;
                                        }
                                        else
                                        {
                                            MonitorLog.WriteLog("Попытка записи данных к справочнику с идентификатором : " + idDict + " в функции GetReportDicts", MonitorLog.typelog.Error, true);
                                        }
                                        break;
                                    }
                                #endregion

                                #region справочник месяцев
                                case 2:
                                    {
                                        List<DictionaryItem> dictValues = new List<DictionaryItem>();
                                        dictValues.Add(new DictionaryItem() { key = 1, value = "январь" });
                                        dictValues.Add(new DictionaryItem() { key = 2, value = "февраль" });
                                        dictValues.Add(new DictionaryItem() { key = 3, value = "март" });
                                        dictValues.Add(new DictionaryItem() { key = 4, value = "апрель" });
                                        dictValues.Add(new DictionaryItem() { key = 5, value = "май" });
                                        dictValues.Add(new DictionaryItem() { key = 6, value = "июнь" });
                                        dictValues.Add(new DictionaryItem() { key = 7, value = "июль" });
                                        dictValues.Add(new DictionaryItem() { key = 8, value = "август" });
                                        dictValues.Add(new DictionaryItem() { key = 9, value = "сентябрь" });
                                        dictValues.Add(new DictionaryItem() { key = 10, value = "октябрь" });
                                        dictValues.Add(new DictionaryItem() { key = 11, value = "ноябрь" });
                                        dictValues.Add(new DictionaryItem() { key = 12, value = "декабрь" });
                                        var obj = dicts.FirstOrDefault(x => x.id == idDict);
                                        if (obj != null)
                                        {
                                            obj.items = dictValues;
                                        }
                                        else
                                        {
                                            MonitorLog.WriteLog("Попытка записи данных к справочнику с идентификатором : " + idDict + " в функции GetReportDicts", MonitorLog.typelog.Error, true);
                                        }
                                        break;
                                    }
                                #endregion

                                default:
                                    {
                                        MonitorLog.WriteLog("Попытка обращения к справочнику с идентификатором : " + idDict + " в функции GetReportDicts", MonitorLog.typelog.Error, true);
                                        break;
                                    }
                            }
                        }
                    }

                    #region заполнение полей справочника
                    foreach (Dict idDict in dicts)
                    {
                        switch (idDict.id)
                        {
                            //год
                            case 1:
                                {
                                    idDict.type = ExtraParamEditor.ComboBox.GetHashCode();
                                    idDict.extraParam.checkBoxes.label = "Год";
                                    break;
                                }
                            //месяц
                            case 2:
                                {
                                    idDict.id = idDict.id;
                                    idDict.type = ExtraParamEditor.ComboBox.GetHashCode();
                                    idDict.extraParam.checkBoxes.label = "Месяц";
                                    break;
                                }
                        }
                    }
                    #endregion

                    return dicts;
                }
                else
                {
                    MonitorLog.WriteLog("Объект отчета не содержит справочники в GetReportDicts", MonitorLog.typelog.Error, true);
                    return null;
                }
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                MonitorLog.WriteLog("GetReportDicts : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (conn_web != null)
                {
                    conn_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        #endregion






        /// <summary>
        /// Выгрузка показаний ПУ
        /// </summary>
        /// <returns></returns>
        /// 

        public Returns GetUploadPU(out Returns ret, SupgFinder finder, string year, string month)
        {
            ret = Utils.InitReturns();
            List<_Point> prefixs = new List<_Point>();
            _Point point = new _Point();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }

            DateTime date = new DateTime(int.Parse(year), int.Parse(month), 1);
            DateTime date_next = date.AddMonths(1);
            string dat = date.ToShortDateString(); //начало месяца
            string dat_s = date_next.ToShortDateString(); //конец месяца 
            //путь, по которому скачивается файл
            string path = "";
            //Имя файла отчета
            string fileNameIn = "";
            ExcelRep excelRepDb = new ExcelRep();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка показаний ПУ за " + month.Trim() + "." + year.Trim() + " по Управляющим организацим " + finder.area.Trim()
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            MyDataReader reader = null;

            try
            {
                conn_web = DBManager.newDbConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret;

                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = DBManager.newDbConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;

                if (finder.pref != "")
                {
                    point.pref = finder.pref;
                    prefixs.Add(point);
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("SELECT pref FROM " + Points.Pref + sDataAliasRest + "kvar where nzp_area=" + finder.nzp_area + " group by nzp_area,pref ");
                    DataTable prefs = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
                    for (int j = 0; j < prefs.Rows.Count; j++)
                    {
                        prefixs.Add(new _Point { pref = prefs.Rows[j]["pref"].ToString().Trim() });
                    }
                }
                string s_year = year.Substring(2);

                #region Получение показаний ПУ

                ExecSQL(conn_db, "drop table  tmp_cnt;", false);
                sql.Remove(0, sql.Length);

                sql.Append(" create temp table ");
                sql.Append(" tmp_cnt( ");
                sql.Append(" nzp integer, ");
                sql.Append(" nzp_counter integer, ");
                sql.Append(" nzp_serv integer, ");
                sql.Append(" pref char(10), ");
                sql.Append(" predpr char(3), ");
                sql.Append(" geu char(2), ");
                sql.Append(" lc char(5), ");
                sql.Append(" llc char(1), ");
                sql.Append(" adr char(60), ");
                sql.Append(" usl char(4), ");
                sql.Append("  rs decimal(10,2), ");
                sql.Append(" dold date, ");
                sql.Append(" zold decimal(12,2), ");
                sql.Append(" message char(80), ");
                sql.Append(" don date, ");
                sql.Append(" nuk char(50), ");
                sql.Append(" ud char(2) ");
                sql.Append(", nzp_supp integer ");
                sql.Append(", num_cnt char(40) ");
                sql.Append(", ns char(2) ) with no log;");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка создания таблицы";
                    ret.result = false;
                    return ret;
                }
                sql.Remove(0, sql.Length);

                foreach (var points in prefixs)
                {

#if PG
                    //пришлось создать функцию, иначе виснет
                    sql.Append(" select " + points.pref + "_data.get_upload_pu('" + dat + "','" + (int)Ls.States.Open + "'," + (int)CounterKinds.Kvar + "," + (int)CounterKinds.Communal + ");");
                    //sql.Append(" insert into tmp_cnt (nzp,nzp_counter, pref, predpr,geu, lc, llc, adr, usl, ns,rs,dold,don,nuk,ud ) ");
                    //sql.Append("  select  cs.nzp,cs.nzp_counter, '" + points.pref + "' as pref ,substring(k.pkod::char from 0 for 3) as predpr,substring(k.pkod::char from 4 for 2) as geu, substring(k.pkod::char from 6 for 5) as lc, ");
                    //sql.Append(" (case when substring(k.pkod::char from 11 for 1)<>'0' then  substring(k.pkod::char from 11 for 1) else null end ) as llc , ");
                    //sql.Append(" max(trim(u.ulica) ||', '|| trim(d.ndom)||' - '|| trim(k.nkvar))   as adr, cs.nzp_serv as usl , max(c.num_cnt) as ns,  max(ty.cnt_stage) as rs , ");
                    //sql.Append(" max(c.dat_uchet) as dold, min(c.dat_uchet) as don,  ");
                    //sql.Append(" max(a.area) as nuk, max(k.uch) as ud   ");
                    //sql.Append(" from " + points.pref + "_data.counters c,  ");
                    //sql.Append(" " + points.pref + "_data.counters_spis cs, ");
                    //sql.Append(" " + points.pref + "_data.kvar k,  ");
                    //sql.Append(" " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica u, ");
                    //sql.Append(" " + Points.Pref + "_data.s_area a," + Points.Pref + "_data.s_geu g, ");
                    //sql.Append(" " + points.pref + "_kernel. s_counttypes ty ");
                    //sql.Append(" where c.nzp_counter=cs.nzp_counter and cs.nzp_type in (" + (int)CounterKinds.Kvar + "," + (int)CounterKinds.Communal + ") and cs.is_actual=1 and c.dat_uchet<='" + dat_s + "' and k.nzp_area=" + finder.nzp_area + " ");
                    //sql.Append(" and k.nzp_kvar=c.nzp_kvar ");
                    //sql.Append(" and k.nzp_dom = d.nzp_dom ");
                    //sql.Append(" and d.nzp_ul  = u.nzp_ul ");
                    //sql.Append(" and cs.dat_close is null ");
                    //sql.Append(" and k.nzp_area = a.nzp_area ");
                    //sql.Append(" and k.nzp_geu  = g.nzp_geu and ty.nzp_cnttype=cs.nzp_cnttype ");
                    //sql.Append(" and (select count(*) from " + points.pref + "_data.prm_3 where nzp = k.nzp_kvar and nzp_prm = 51 and is_actual <> 100 and '" + dat + "' between dat_s and dat_po and val_prm = '" + (int)Ls.States.Open + "') > 0 ");
                    //sql.Append(" group by cs.nzp,cs.nzp_counter,k.pkod,cs.nzp_serv ; ");
#else
                    sql.Append(" insert into tmp_cnt (nzp,nzp_counter,nzp_serv, pref, predpr,geu, lc, llc, adr, usl,rs,dold,don,nuk,ud, num_cnt) ");
                    sql.Append("  select  cs.nzp,cs.nzp_counter,cs.nzp_serv, '" + points.pref + "' as pref ,substr(k.pkod,0,3) as predpr,substr(k.pkod,4,2) as geu, substr(k.pkod,6,5) as lc, ");
                    sql.Append(" (case when substr(k.pkod,11,1)<>0 then  substr(k.pkod,11,1) else '0' end ) as llc , ");
                    sql.Append(" max(trim(u.ulica) ||', '|| trim(d.ndom)||' - '|| trim(k.nkvar))   as adr, cs.nzp_serv as usl ,  max(ty.cnt_stage) as rs , ");
                    sql.Append(" max(c.dat_uchet) as dold, min(c.dat_uchet) as don,  ");
                    sql.Append(" max(a.area) as nuk, '' as ud, cs.num_cnt   ");
                    sql.Append(" from " + points.pref + "_data:counters c,  ");
                    sql.Append(" " + points.pref + "_data:counters_spis cs, ");
                    sql.Append(" " + points.pref + "_data:kvar k,  ");
                    sql.Append(" " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica u, ");
                    sql.Append(" " + Points.Pref + "_data:s_area a," + Points.Pref + "_data:s_geu g, ");
                    sql.Append(" " + points.pref + "_kernel: s_counttypes ty ");
                    sql.Append(" where c.nzp_counter=cs.nzp_counter and cs.nzp_type in (" + (int)CounterKinds.Kvar + "," + (int)CounterKinds.Communal + ") and cs.is_actual=1 and c.dat_uchet<='" + dat_s + "' and k.nzp_area=" + finder.nzp_area + " ");
                    sql.Append(" and k.nzp_kvar=c.nzp_kvar ");
                    sql.Append(" and k.nzp_dom = d.nzp_dom ");
                    sql.Append(" and d.nzp_ul  = u.nzp_ul ");
                    sql.Append(" and cs.dat_close is null ");
                    sql.Append(" and k.nzp_area = a.nzp_area ");
                    sql.Append(" and k.nzp_geu  = g.nzp_geu and ty.nzp_cnttype=cs.nzp_cnttype ");
                    sql.Append(" and (select count(*) from " + points.pref + "_data:prm_3 where nzp = k.nzp_kvar and nzp_prm = 51 and is_actual <> 100 and '" + dat + "' between dat_s and dat_po and val_prm = '" + (int)Ls.States.Open + "') > 0 ");
                    sql.Append(" group by cs.nzp,cs.nzp_counter,cs.num_cnt,k.pkod,cs.nzp_serv ");
#endif

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;

                    //IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
                    //cmd.ExecuteNonQuery();
                    //cmd.Dispose();

                    sql.Remove(0, sql.Length);

                    sql.Append("update tmp_cnt  ");
#if PG
                    sql.Append("set zold = (select max(val_cnt) from " + points.pref + "_data.counters where nzp_counter=tmp_cnt.nzp_counter and dat_uchet=tmp_cnt.dold and is_actual=1 ) ");
#else
                    sql.Append("set zold = (select max(val_cnt) from " + points.pref + "_data:counters where nzp_counter=tmp_cnt.nzp_counter and dat_uchet=tmp_cnt.dold and is_actual=1 ) ");
#endif
                    sql.Append("where  pref='" + points.pref + "';  ");
                    ret = ExecSQL(conn_db, null, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "\nОшибка обновления показаний приборов" + ret.text;
                        return ret;
                    }
                    sql.Remove(0, sql.Length);

                    sql.Append(" update tmp_cnt  ");
                    sql.Append(" set message = (select max(case  ");
                    sql.Append(" when cs.dat_close is not null then 'Счетчик отключен!'   ");
                    sql.Append(" when tmp_cnt.dold>cs.dat_uchet and tmp_cnt.zold<cs.val_cnt then 'Показание меньше предыдущего!' end ) ");
#if PG
                    sql.Append(" from " + points.pref + "_data.counters cs  where  cs.nzp_counter=tmp_cnt.nzp_counter ) ");
#else
                    sql.Append(" from " + points.pref + "_data:counters cs  where  cs.nzp_counter=tmp_cnt.nzp_counter ) ");
#endif
                    sql.Append(" where  pref='" + points.pref + "';  ");
                    ret = ExecSQL(conn_db, null, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "\nОшибка обновления комментариев" + ret.text;
                        return ret;
                    }
                    sql.Remove(0, sql.Length);

                }
                //код услуги  для всех pref  конце где нет неопределенности с кодом

                sql.Append(" update tmp_cnt  ");
#if PG
                sql.Append(" set usl  = (select kod_usl from " + Points.Pref + "_data.services_smr where nzp_serv=tmp_cnt.usl::int and nzp_area=" + finder.nzp_area + ") ");
                sql.Append(" where (select count(*) from " + Points.Pref + "_data.services_smr where nzp_serv=tmp_cnt.usl::int and nzp_area=" + finder.nzp_area + ")=1 ");
#else
                sql.Append(" set usl  = (select kod_usl from " + Points.Pref + "_data:services_smr where nzp_serv=tmp_cnt.usl and nzp_area=" + finder.nzp_area + ") ");
                sql.Append(" where (select count(*) from " + Points.Pref + "_data:services_smr where nzp_serv=tmp_cnt.usl and nzp_area=" + finder.nzp_area + ")=1 ");
#endif
                ret = ExecSQL(conn_db, null, sql.ToString(), true);
                sql.Remove(0, sql.Length);

                if (!ret.result)
                {
                    ret.text = "\nОшибка обновления кодов услуг" + ret.text;
                    return ret;
                }
                foreach (var points in prefixs)
                {
                    sql.Append("update tmp_cnt  ");
#if PG
                    sql.Append("set nzp_supp = (select max(nzp_supp) from " + points.pref + "_data.tarif where nzp_kvar = tmp_cnt.nzp and nzp_serv = tmp_cnt.usl::int and tarif > 0 ");
#else
                    sql.Append("set nzp_supp = (select max(nzp_supp) from " + points.pref + "_data:tarif where nzp_kvar = tmp_cnt.nzp and nzp_serv = tmp_cnt.usl and tarif > 0 ");
#endif
                    sql.Append(" and dat_s < '" + dat_s + "' and dat_po >= '" + dat + "') ");
                    sql.Append("where pref = '" + points.pref + "'");
                    ret = ExecRead(conn_db, null, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        ret.text = "\nОшибка обновления кодов поставщиков: " + ret.text;
                        return ret;
                    }
                    sql.Remove(0, sql.Length);

                    sql.Append(" update tmp_cnt  ");
#if PG
                    sql.Append(" set usl  = coalesce((select max(kod_usl) from " + Points.Pref + "_data.services_smr where nzp_serv = tmp_cnt.usl::int and nzp_area = " + finder.nzp_area + " and nzp_supp = tmp_cnt.nzp_supp), ");
                    sql.Append(" (select distinct kod_usl from " + Points.Pref + "_data.services_smr where nzp_serv = tmp_cnt.usl::int and nzp_area = " + finder.nzp_area + " and (nzp_supp = 0 or nzp_supp is null) ))");
                    sql.Append(" where (select count(*) from " + Points.Pref + "_data.services_smr where nzp_serv=tmp_cnt.usl::int and nzp_area=" + finder.nzp_area + ") > 1 and pref='" + points.pref + "' ");
#else
                    sql.Append(" set usl  = nvl((select max(kod_usl) from " + Points.Pref + "_data:services_smr where nzp_serv = tmp_cnt.usl and nzp_area = " + finder.nzp_area + " and nzp_supp = tmp_cnt.nzp_supp), ");
                    sql.Append(" (select kod_usl from " + Points.Pref + "_data:services_smr where nzp_serv = tmp_cnt.usl and nzp_area = " + finder.nzp_area + " and (nzp_supp = 0 or nzp_supp is null) ))");
                    sql.Append(" where (select count(*) from " + Points.Pref + "_data:services_smr where nzp_serv=tmp_cnt.usl and nzp_area=" + finder.nzp_area + ") > 1 and pref='" + points.pref + "' ");
#endif
                    ret = ExecRead(conn_db, null, out reader, sql.ToString(), true);
                    sql.Remove(0, sql.Length);
                    if (!ret.result)
                    {
                        ret.text = "\nОшибка обновления кодов услуг, при неопределенности" + ret.text;
                        return ret;
                    }
                }

                #region определение номеров счетчиков
                ExecSQL(conn_db, "drop table tmp_cnt_one", false);
                ret = ExecSQL(conn_db, "select nzp, usl from tmp_cnt group by nzp,usl having count(*) = 1 into temp tmp_cnt_one", true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db, "create index ix_tmp_cnt_one on tmp_cnt_one(nzp,usl)", true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db, "update tmp_cnt set ns = 1 where exists (select nzp from tmp_cnt_one a where a.nzp = tmp_cnt.nzp and a.usl = tmp_cnt.usl)", true);
                if (!ret.result) return ret;

                sql = new StringBuilder("select nzp, usl, nzp_counter from tmp_cnt where not exists (select nzp from tmp_cnt_one a where a.nzp = tmp_cnt.nzp and a.usl = tmp_cnt.usl) order by nzp, usl, num_cnt");
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result) return ret;
                try
                {
                    int nzp = -1, nzp_prev = -1;
                    string usl = "-1", usl_prev = "-1";
                    int ns = 0;
                    int nzp_counter = -1;
                    while (reader.Read())
                    {
                        if (reader["nzp"] != DBNull.Value) nzp = Convert.ToInt32(reader["nzp"]);
                        if (reader["nzp_counter"] != DBNull.Value) nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                        if (reader["usl"] != DBNull.Value) usl = Convert.ToString(reader["usl"]);

                        if (nzp != nzp_prev || usl != usl_prev)
                        {
                            nzp_prev = nzp;
                            usl_prev = usl;
                            ns = 1;
                        }
                        else ns++;

                        ret = ExecSQL(conn_db, "update tmp_cnt set ns = " + ns.ToString("00") + " where nzp_counter = " + nzp_counter);
                        if (!ret.result) return ret;
                    }
                }
                finally
                {
                    if (reader != null) reader.Close();
                    ExecSQL(conn_db, "drop table tmp_cnt_one", false);
                }
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append("select count(*) as num from tmp_cnt");
                object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                int num = Convert.ToInt32(obj);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("select predpr, nuk from tmp_cnt order by predpr limit 1; ");
#else
                sql.Append("select first 1 predpr, nuk from tmp_cnt order by predpr; ");
#endif

                ret = ExecRead(conn_db, null, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                fileNameIn = "EmptyFile_" + DateTime.Now.Ticks;
                while (reader.Read())
                {
                    string pre = (reader["predpr"] != DBNull.Value ? ((string)reader["predpr"]).ToString().Trim() : "");
                    string area = (reader["nuk"] != DBNull.Value ? ((string)reader["nuk"]).ToString().Trim() : "");
                    fileNameIn = pre + month.Trim() + s_year + "_" + DateTime.Now.Ticks;
                }


                DBManager.CreateIndexIfNotExists(conn_db, "ix1_tmp_cnt", Points.Pref + "_kernel" + tableDelimiter + "tmp_cnt", "nzp,lc,nzp_counter");
                ExecSQL(conn_db, DBManager.sUpdStat + "tmp_cnt", false);

                sql.Remove(0, sql.Length);
#if PG
                //sql.Append("   SELECT  a.*,(SELECT count(*) FROM tmp_cnt b WHERE a.nzp =b.nzp  AND a.nzp_counter<= b.nzp_counter AND a.nzp_serv=b.nzp_serv) as ns INTO TEMP tmp_cnt2 FROM tmp_cnt a ORDER BY predpr,lc");
                sql.Append("   SELECT * INTO TEMP tmp_cnt2 FROM tmp_cnt ORDER BY predpr, lc");
#else
                //sql.Append("   SELECT  a.*,(SELECT count(*) FROM tmp_cnt b WHERE a.nzp =b.nzp  AND a.nzp_counter<= b.nzp_counter AND a.nzp_serv=b.nzp_serv) as ns FROM tmp_cnt a ORDER BY predpr,lc INTO TEMP tmp_cnt2");
                sql.Append("   SELECT * FROM tmp_cnt ORDER BY predpr, lc INTO TEMP tmp_cnt2");
#endif
                ret = ExecSQL(conn_db, sql.ToString());
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                ExecSQL(conn_db, "drop table tmp_cnt", false);

                //-------------------------Пишем в реестр выгрузок и табличку с выгрузками----------------------
                //----------------Если уже существует реестры с таким же nzp_area, то их делаем архивным is_actual=100


                //пишем в реестр
                sql.Remove(0, sql.Length);
                sql.Append("INSERT into " + Points.Pref + sDataAliasRest + "reestr_upload_pu ");
                sql.Append(" (nzp_area,month,year,is_actual,date_upload ) values ");
                sql.Append(" (" + finder.nzp_area + "," + int.Parse(month) + "," + int.Parse(year) + ",1," + sCurDateTime + ")");
                ret = ExecSQL(conn_db, sql.ToString());
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                int nzp_reestr = GetSerialValue(conn_db);

                //в архив старые реестры
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE " + Points.Pref + sDataAliasRest + "reestr_upload_pu SET (is_actual,date_when) =(100," + sCurDateTime + ") WHERE nzp_area=" + finder.nzp_area + " and nzp_reestr<>" + nzp_reestr);
                ret = ExecSQL(conn_db, sql.ToString());
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                //пишем данные реестра
                sql.Remove(0, sql.Length);
                string fin_bank = Points.Pref + "_fin_" + s_year + tableDelimiter;

                sql.Append("INSERT INTO " + fin_bank + "upload_pu_" + month);
                sql.Append(" (nzp_reestr,nzp_kvar,nzp_serv,nzp_counter,ns,usl,subpkod)  ");
                sql.Append(" SELECT " + nzp_reestr + ", nzp,nzp_serv,nzp_counter,ns,usl, ");
                sql.Append("(trim(" + sNvlWord + "(predpr,0))||trim(" + sNvlWord + "(geu,0))||trim(" + sNvlWord + "(lc,0))||trim(" + sNvlWord + "(llc,0))) as subpkod FROM tmp_cnt2;");
                ret = ExecSQL(conn_db, sql.ToString());
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }


                ExecSQL(conn_db, DBManager.sUpdStat + fin_bank + "upload_pu_" + month, false);

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT * FROM tmp_cnt2 ");
                DataTable DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;

                #endregion

                exDBF eDBF = new exDBF(fileNameIn);
                eDBF.AddColumn("PREDPR", Type.GetType("System.String"), 3, 0);
                eDBF.AddColumn("GEU", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("LC", Type.GetType("System.String"), 5, 0);
                eDBF.AddColumn("LLC", Type.GetType("System.String"), 1, 0);
                eDBF.AddColumn("ADR", Type.GetType("System.String"), 60, 0);
                eDBF.AddColumn("USL", Type.GetType("System.String"), 4, 0);
                eDBF.AddColumn("NS", Type.GetType("System.String"), 2, 0);
                eDBF.AddColumn("RS", Type.GetType("System.Double"), 10, 0);
                eDBF.AddColumn("DOLD", Type.GetType("System.DateTime"), 0, 0);
                eDBF.AddColumn("ZOLD", Type.GetType("System.Double"), 12, 2);
                eDBF.AddColumn("DNEW", Type.GetType("System.DateTime"), 0, 0);
                eDBF.AddColumn("ZNEW", Type.GetType("System.Double"), 12, 2);
                eDBF.AddColumn("MESSAGE", Type.GetType("System.String"), 80, 0);
                eDBF.AddColumn("DON", Type.GetType("System.DateTime"), 3, 0);
                eDBF.AddColumn("NUK", Type.GetType("System.String"), 50, 0);
                eDBF.AddColumn("UD", Type.GetType("System.String"), 2, 0);
                string pathIn = STCLINE.KP50.Global.Constants.ExcelDir;
                if (InputOutput.useFtp) pathIn = InputOutput.GetInputDir();
                eDBF.Save(pathIn, 866);

                string strFilePath = Path.GetFullPath(String.Format("{0}\\{1}.DBF", pathIn, fileNameIn));

                int i = 0;
                for (int m = 0; i < DT.Rows.Count; m++)
                {
                    i++;
                    //string NS = "";
                    string NS = (DT.Rows[m]["ns"] != DBNull.Value ? (Convert.ToDecimal(DT.Rows[m]["ns"])).ToString() : "");
                    //String[] mas = NS_.Split('_');
                    //if (mas.Length > 0)
                    //{
                    //    if (mas[0].Trim().Length > 4)
                    //    {
                    //        NS = mas[0].Trim().Substring(mas[0].Trim().Length - 2, 2);
                    //    }
                    //    else
                    //    {
                    //        NS = mas[0].Trim();
                    //    }
                    //}
                    if (NS.Length > 2)
                    {
                        NS = ""; // если num_cnt кривой, то ничего не записываем
                    }

                    Utils.setCulture();
                    string PREDPR = (DT.Rows[m]["predpr"] != DBNull.Value ? ((string)DT.Rows[m]["predpr"]).ToString().Trim() : "");
                    string GEU = (DT.Rows[m]["geu"] != DBNull.Value ? ((string)DT.Rows[m]["geu"]).ToString().Trim() : "");
                    string LC = (DT.Rows[m]["lc"] != DBNull.Value ? ((string)DT.Rows[m]["lc"]).ToString().Trim() : "0");
                    string LLC = (DT.Rows[m]["llc"] != DBNull.Value ? ((string)DT.Rows[m]["llc"]).ToString().Trim() : "");
                    string ADR = (DT.Rows[m]["adr"] != DBNull.Value ? ((string)DT.Rows[m]["adr"]).ToString().Trim() : "");
                    string USL = (DT.Rows[m]["usl"] != DBNull.Value ? ((string)DT.Rows[m]["usl"]).ToString().Trim() : "");
                    decimal RS = (DT.Rows[m]["rs"] != DBNull.Value ? ((decimal)DT.Rows[m]["rs"]) : 0);
                    DateTime DOLD = (DT.Rows[m]["dold"] != DBNull.Value ? ((DateTime)DT.Rows[m]["dold"]) : new DateTime());

                    decimal ZOLD = (DT.Rows[m]["zold"] != DBNull.Value ? ((decimal)DT.Rows[m]["zold"]) : 0);
                    string MESSAGE = (DT.Rows[m]["message"] != DBNull.Value ? ((string)DT.Rows[m]["message"]).ToString().Trim() : "");

                    DateTime DON = (DT.Rows[m]["don"] != DBNull.Value ? ((DateTime)DT.Rows[m]["don"]) : new DateTime());

                    string NUK = (DT.Rows[m]["nuk"] != DBNull.Value ? ((string)DT.Rows[m]["nuk"]).ToString().Trim() : "");
                    string UD = (DT.Rows[m]["ud"] != DBNull.Value ? ((string)DT.Rows[m]["ud"]).ToString().Trim() : "");

                    DataRow R = eDBF.DataTable.NewRow();
                    R["PREDPR"] = PREDPR;
                    R["GEU"] = GEU;
                    R["LC"] = LC;
                    R["LLC"] = LLC;
                    R["ADR"] = ADR;
                    R["USL"] = USL;
                    R["NS"] = NS;
                    R["RS"] = RS;
                    R["DOLD"] = DOLD.ToShortDateString();
                    R["ZOLD"] = ZOLD;
                    R["MESSAGE"] = MESSAGE;
                    R["DON"] = DON.ToShortDateString();
                    R["NUK"] = NUK;
                    R["UD"] = UD;
                    eDBF.DataTable.Rows.Add(R);
                    if (i % 100 == 0)
                    {
                        excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)i) / num });
                        eDBF.Append(strFilePath);
                        eDBF.DataTable.Rows.Clear();
                    }
                }
                if (eDBF.DataTable.Rows.Count > 0) eDBF.Append(strFilePath);

                sql.Remove(0, sql.Length);
                DT.Clear();

                if (InputOutput.useFtp) InputOutput.SaveOutputFile(strFilePath);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetUploadPU:\n" + ex, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = path + fileNameIn + ".DBF", file_name = fileNameIn + ".DBF" });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();

                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
                if (conn_web != null) conn_web.Close();
            }

            return ret;
        }

        /// <summary>
        /// Выгрузка реестра для загрузки в БС
        /// </summary>
        /// <returns></returns>
        public Returns GetUploadReestr(out Returns ret, Finder finder, List<int> BanksList, string unloadVersionFormat,string statusLS)
        {
            ret = Utils.InitReturns();

            if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.Version4).ToString())
            {
                // Калужская область
                BankUnloadKaluga vers = new BankUnloadKaluga();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.Version3).ToString())
            {
                // Марий Эл
                BankDownloadReestrVersion3 vers = new BankDownloadReestrVersion3();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.VersionBaikalskVstkb).ToString())
            {
                BankDownloadReestrBaikalskVstkb vers = new BankDownloadReestrBaikalskVstkb();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.VersionBaikalskSber).ToString())
            {
                BankDownloadReestrBaikalskSber vers = new BankDownloadReestrBaikalskSber();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.VersionBaikalskSocProtect).ToString())
            {
                BankDownloadReestrBaikalskSocProtect vers = new BankDownloadReestrBaikalskSocProtect(12, Convert.ToInt32(unloadVersionFormat));
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }

            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.Version2_2).ToString())
            {
                // Тула с кодом 86040167
                BankDownloadReestrVersion22 vers = new BankDownloadReestrVersion22();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.IssrpR102).ToString())
            {
                BankDownloadReestrIssrpR102 vers = new BankDownloadReestrIssrpR102();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else if (unloadVersionFormat == ((int)FilesImported.UnloadFormatVersion.IssrpF102).ToString())
            {
                BankDownloadReestrIssrpF102 vers = new BankDownloadReestrIssrpF102();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            else
            {
                // Тула с кодом 86040111
                BankDownloadReestrVersion21 vers = new BankDownloadReestrVersion21();
                ret = vers.GetUploadReestr(finder, BanksList, statusLS);
            }
            
            return ret;
        }
        
        
        //todo pstgree
        //выгрузка начислений
        public Returns GetUploadCharge(out Returns ret, SupgFinder finder, string year, string month)
        {
            ret = Utils.InitReturns();
            //путь, по которому скачивается файл
            string path = "";
            //Имя файла отчета
            string fileNameOut = "";
            string fileNameIn = "";
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }
            ExcelRep excelRepDb = new ExcelRep();
            StringBuilder sql = new StringBuilder();

            fileNameOut = Utility.FileUtility.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "charge" +
                            year.ToString() +
                            month.ToString()) + ".7z";
            fileNameIn = Utility.FileUtility.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "charge" +
                           year.ToString() +
                           month.ToString()) + ".txt";

            FileStream memstr = new FileStream(Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileNameIn), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));


            string version = "*version*|1.0.0|" + DateTime.Now.ToUniversalTime().ToShortDateString() + "|";
            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка начислений за " + month.Trim() + "." + year.Trim()
            });
            if (!ret.result) return ret;
            int nzpExc = ret.tag;

            List<string> prefixs = new List<string>();
            if (finder.pref != "")
            {
                prefixs.Add(finder.pref.Trim());
            }
            else
            {
                foreach (_Point point in Points.PointList)
                    prefixs.Add(point.pref.Trim());
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = DBManager.newDbConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                return ret;
            }

            MyDataReader reader = null;

            try
            {
                string s_year = year.Substring(2);

                #region Получение начислений
                ExecSQL(conn_db, "drop table up_charge_01;", false);

#if PG
                sql.Append(" create temp table up_charge_01( ");
                sql.Append(" nzp_key serial, ");
                sql.Append(" pref char(10), ");
                sql.Append(" pkod decimal(13), ");
                sql.Append(" nzp_kvar integer, ");
                sql.Append(" fio varchar(100), ");
                sql.Append(" adres varchar(100), ");
                sql.Append(" nzp_supp integer, ");
                sql.Append(" nzp_serv integer, ");
                sql.Append(" period char(10), ");
                sql.Append(" sum_tarif decimal(14,2), ");
                sql.Append(" dolg decimal(14,7), ");
                sql.Append(" peni decimal(14,2), ");
                sql.Append(" sum_charge decimal(14,2) ");
                sql.Append(" ) ; ");
#else
                sql.Append(" create temp table up_charge_01( ");
                sql.Append(" nzp_key serial, ");
                sql.Append(" pref char(10), ");
                sql.Append(" pkod decimal(13), ");
                sql.Append(" nzp_kvar integer, ");
                sql.Append(" fio varchar(100), ");
                sql.Append(" adres varchar(100), ");
                sql.Append(" nzp_supp integer, ");
                sql.Append(" nzp_serv integer, ");
                sql.Append(" period char(10), ");
                sql.Append(" sum_tarif decimal(14,2), ");
                sql.Append(" dolg decimal(14,7), ");
                sql.Append(" peni decimal(14,2), ");
                sql.Append(" sum_charge decimal(14,2) ");
                sql.Append(" ) with no log; ");
#endif

                ret = ExecSQL(conn_db, sql.ToString(), true);
                sql.Remove(0, sql.Length);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db, "create index ix_up_charge_01_1 on up_charge_01(nzp_key)", true);
                ret = ExecSQL(conn_db, "create index ix_up_charge_01_2 on up_charge_01(pref, nzp_kvar)", true);
                sql.Remove(0, sql.Length);
                if (!ret.result) return ret;

                foreach (var pref in prefixs)
                {
                    if (!TempTableInWebCashe(conn_db, pref + "_charge_" + year.Substring(2, 2) + ":charge_" + month)) continue;

                    sql.Append(" insert into up_charge_01 (nzp_key, pref, nzp_kvar,nzp_supp,nzp_serv,period,sum_tarif,dolg,peni,sum_charge) ");
                    sql.Append(" select 0, " + Utils.EStrNull(pref) + ", nzp_kvar, nzp_supp, nzp_serv, '" + month + "." + year + "', sum_tarif, sum_insaldo-sum_money, 0, sum_charge");
#if PG
                    sql.Append(" from " + pref + "_charge_" + year.Substring(2, 2) + ".charge_" + month);
#else
                    sql.Append(" from " + pref + "_charge_" + year.Substring(2, 2) + ":charge_" + month);
#endif
                    sql.Append(" where dat_charge is null and nzp_serv > 1");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    sql.Remove(0, sql.Length);
                    if (!ret.result) return ret;

#if PG
                    ExecSQL(conn_db, "analyze up_charge_01", true);
#else
                    ExecSQL(conn_db, "update statistics for table up_charge_01", true);
#endif

                    sql.Append(" update up_charge_01 set (pkod, fio, adres) = ((select pkod, trim(nvl(fio,''))");
#if PG
                    sql.Append(",trim(coalesce(t.town,''))||','||trim(coalesce(r.rajon,''))||','||trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||");
                    sql.Append("',д.'||trim(coalesce(ndom,''))||',корп.'|| trim(coalesce(nkor,''))||',кв.'||trim(coalesce(nkvar,''))||',комн.'||trim(coalesce(nkvar_n,''))");
                    sql.Append(" from " + Points.Pref + "_data.kvar k," + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica u," + Points.Pref + "_data.s_rajon r," + Points.Pref + "_data.s_town t");
#else
                    sql.Append(",trim(nvl(t.town,''))||','||trim(nvl(r.rajon,''))||','||trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||");
                    sql.Append("',д.'||trim(nvl(ndom,''))||',корп.'|| trim(nvl(nkor,''))||',кв.'||trim(nvl(nkvar,''))||',комн.'||trim(nvl(nkvar_n,''))");
                    sql.Append(" from " + Points.Pref + "_data:kvar k," + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica u," + Points.Pref + "_data:s_rajon r," + Points.Pref + "_data:s_town t");
#endif
                    sql.Append(" where nzp_kvar=up_charge_01.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj and r.nzp_town=t.nzp_town))");
                    sql.Append(" where pref = " + Utils.EStrNull(pref));
                    //ExecByStep(conn_db, "up_charge_01", "nzp_key", sql.ToString(), 100000, "", out ret);
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    sql.Remove(0, sql.Length);
                    if (!ret.result) return ret;
                }

                object obj = ExecScalar(conn_db, "select count(*) as num from up_charge_01 where pkod is not null", out ret, true);
                if (!ret.result) return ret;
                int num = Convert.ToInt32(obj);

                ret = ExecRead(conn_db, out reader, "select * from up_charge_01 where pkod is not null order by adres", true);
                if (!ret.result) return ret;

                int i = 0;
                string str;
                while (reader.Read())
                {
                    str =
                        (reader["pkod"] != DBNull.Value ? ((Decimal)reader["pkod"]).ToString("0").Trim() + "|" : "|") +
                        (reader["fio"] != DBNull.Value ? ((string)reader["fio"]) + "|" : "|") +
                        (reader["adres"] != DBNull.Value ? ((string)reader["adres"]) + "|" : "|").Replace(",-", "").Replace(",комн.-", "").Replace(",корп.-", "") +
                        (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                        (reader["nzp_serv"] != DBNull.Value ? ((int)reader["nzp_serv"]).ToString().Trim() + "|" : "|") +
                        (reader["period"] != DBNull.Value ? ((string)reader["period"]).Trim() + "|" : "|") +
                        (reader["sum_tarif"] != DBNull.Value ? ((Decimal)reader["sum_tarif"]) + "|" : "|") +
                        (reader["dolg"] != DBNull.Value ? ((Decimal)reader["dolg"]) + "|" : "|") +
                        (reader["peni"] != DBNull.Value ? ((Decimal)reader["peni"]) + "|" : "|") +
                        (reader["sum_charge"] != DBNull.Value ? ((Decimal)reader["sum_charge"]) + "|" : "|");
                    writer.WriteLine(str);
                    str = null;
                    i++;
                    if (i % 100 == 0) excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)i) / num });
                }
                reader.Close();
                #endregion

                writer.Flush();
                writer.Close();
                memstr.Close();

                SevenZipCompressor file = new SevenZipCompressor();
                file.EncryptHeaders = false;
                file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
                file.DefaultItemName = fileNameIn;
                file.CompressionLevel = SevenZip.CompressionLevel.Normal;

                file.CompressFiles(Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileNameOut), Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileNameIn));
                File.Delete(Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileNameIn));
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetUploadCharge:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();

                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = path + fileNameOut });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();
            }

            return ret;
        }
      
    }




}

