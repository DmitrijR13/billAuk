using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep : ExcelRepClient
    {


        //справка по отключениям подачи коммунальных услуг
        public DataTable GetDT_SpravkaPoOtklKomUsl(Ls finder, int nzp_serv, int month, int year)
        {
            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;
            IDataReader reader2 = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();

            try
            {
                #region Открытие соединения с БД
                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region Получение списка префиксов
                sql.Append(" select unique  pref from  " + con_web.Database + ": t" + finder.nzp_user + "_spls; ");

                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table sprav_otkl_usl; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table sprav_otkl_usl (" +
                             " nzp_kvar          INTEGER, " +
                             " name_supp         CHAR(100), " +
                             " service           CHAR(100), " +
                             " geu               CHAR(100), " +
                             " adr               CHAR(100), " +
                             " countKvar         INTEGER, " +
                             " count_daynedo     DECIMAL(14,2), " +
                             " count_kvarchas    INTEGER DEFAULT 0, " +
                             " col_gil           INTEGER, " +
                             " sum_nedop         DECIMAL(14,2) " +
                             " ) With no log; "
                          );

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                foreach (string pref in prefix)
                {
                    sql.Remove(0, sql.Length);

                    //количество дней в месяце
                    int dayInMonth = DateTime.DaysInMonth(year, month);

                    sql.Append(" insert into sprav_otkl_usl (nzp_kvar, name_supp, service,  geu, adr ,countKvar,  count_daynedo, sum_nedop) " +
                               " select  s.nzp_kvar, sup.name_supp, ser.service , g.geu , s.adr ,count(unique s.nzp_kvar) as countKvar, " +
                               " sum(" + dayInMonth + "-c.c_okaz) as count_daynedo, sum(c.sum_nedop) as sum_nedop  " +

                               " from " + con_web.Database + "@" + DBManager.getServer(con_web) + ":   t" + finder.nzp_user + "_spls s , " +
                               " " + pref + "_charge_" + year.ToString().Substring(year.ToString().Length - 2) + ":  charge_" + String.Format("{0:00}", month) + " c, " +
                               " " + con_db.Database + " :  supplier sup, " +
                               " " + con_db.Database + " :  services ser, " +
                        //"  " + con_db.Database.Replace("kernel", "data") + " :  s_geu g " +
                               "  " + Points.Pref + "_data:  s_geu g " +

                               " where s.nzp_kvar = c.nzp_kvar " +
                               " and sup.nzp_supp = c.nzp_supp " +
                               " and ser.nzp_serv =  c.nzp_serv " +
                               "  and g.nzp_geu = s. nzp_geu " +
                               " and  c.nzp_serv > 1 " +
                               "  and  c.sum_nedop > 0 ");
                    //" group by 1,2,3,4;  ");

                    //добавляем фильтр
                    if (nzp_serv != -1)
                    {
                        sql.Append(" and c.nzp_serv = " + nzp_serv + " ");
                    }

                    // начисление должно быть самое позднее                    
                    sql.Append(
                        " and nvl(c.dat_charge,date('01.01.1901'))=(select max(nvl(c1.dat_charge,date('01.01.1901'))) " +
                        " from " + pref + "_charge_" + year.ToString().Substring(year.ToString().Length - 2) + ":  charge_" + String.Format("{0:00}", month) + " c1 " +
                        " where  c1.nzp_kvar=c.nzp_kvar" +
                        " and c1.nzp_supp = c.nzp_supp  and c1.nzp_serv =  c.nzp_serv)   "
                            );

                    sql.Append(" group by 1,2,3,4,5; ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    //вставка количества жильцов
                    sql.Remove(0, sql.Length);

                    sql.Append(" update  sprav_otkl_usl set col_gil = (   " +
                                " select  max(p.val_prm)  " +

                                " from  " + con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_spls s, " +
                                " " + pref + "_data: prm_1 p " +
                                " where s.nzp_kvar = p.nzp  " +
                                " and p.nzp_prm = 5  " +
                                " and p.is_actual = 1  " +
                                " and p.dat_s <= '" + "01" + String.Format("{0:00}", month) + year + "' " +
                                " and p. dat_po >= '" + "01" + String.Format("{0:00}", month) + year + "'  " +
                                " and sprav_otkl_usl.nzp_kvar = p.nzp " +
                                " ); ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }
                #endregion

                #region Выборка из  sprav_otkl_usl
                sql.Remove(0, sql.Length);

                sql.Append(" select * from  sprav_otkl_usl Order by name_supp; ");

                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                if (reader2 != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader2, LoadOption.OverwriteChanges);
                }


                //удаление nzp_kvar
                dt.Columns.Remove("nzp_kvar");


                return dt;

                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table sprav_otkl_usl; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (reader2 != null)
                {
                    reader2.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

        }

        //справка по отключениям подачи коммунальных услуг
        public DataTable GetDT_SpravkaPoOtklKomUslDom(Ls finder, int nzp_serv, int month, int year)
        {
            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;
            IDataReader reader2 = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();

            try
            {
                #region Открытие соединения с БД
                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region Получение списка префиксов
                sql.Append(" select unique  pref from  " + con_web.Database + "@" + DBManager.getServer(con_web) + ":t" + finder.nzp_user + "_spls; ");

                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table t_sprav_otkl_usl; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_sprav_otkl_usl (" +
                             " nzp_dom           INTEGER, " +
                             " nzp_kvar          INTEGER, " +
                             " nzp_supp          INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " countKvar         INTEGER, " +
                             " count_daynedo     DECIMAL(14,2), " +
                             " count_kvarchas    INTEGER DEFAULT 0, " +
                             " col_gil           INTEGER, " +
                             " sum_nedop         DECIMAL(14,2) " +
                             " ) With no log; "
                          );

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                foreach (string pref in prefix)
                {
                    sql.Remove(0, sql.Length);

                    //количество дней в месяце
                    int dayInMonth = DateTime.DaysInMonth(year, month);

                    sql.Append(" insert into t_sprav_otkl_usl (nzp_dom, nzp_kvar, nzp_supp, nzp_serv, count_daynedo, sum_nedop) " +
                               " select  s.nzp_dom, s.nzp_kvar, nzp_supp, nzp_serv, " +
                               " sum(" + dayInMonth + "-c.c_okaz) as count_daynedo, sum(c.sum_nedop) as sum_nedop  " +
                               " from " + con_web.Database + "@" + DBManager.getServer(con_web) + ":t" + finder.nzp_user + "_spls s , " +
                               " " + pref + "_charge_" + year.ToString().Substring(year.ToString().Length - 2) +
                               ":charge_" + month.ToString("00") + " c " +
                               " where s.nzp_kvar = c.nzp_kvar " +
                               " and  c.nzp_serv > 1  and dat_charge is null " +
                               "  and  c.sum_nedop > 0.001 ");
                    //" group by 1,2,3,4;  ");

                    //добавляем фильтр
                    if (nzp_serv != -1)
                    {
                        sql.Append(" and c.nzp_serv = " + nzp_serv + " ");
                    }

                    sql.Append(" group by 1,2,3,4; ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    ExecSQL(con_db, "create index ix_tmp756 on t_sprav_otkl_usl(nzp_kvar) ", true);
                    ExecSQL(con_db, "update statisticvs for table t_sprav_otkl_usl ", true);
                    //вставка количества жильцов
                    sql.Remove(0, sql.Length);

                    sql.Append(" update  t_sprav_otkl_usl set col_gil = (   " +
                                " select  max(p.val_prm)  " +
                                " from  " + pref + "_data: prm_1 p " +
                                " where t_sprav_otkl_usl.nzp_kvar = p.nzp  " +
                                " and p.nzp_prm = 5  " +
                                " and p.is_actual = 1  " +
                                " and p.dat_s <= '" + "01." + month.ToString("00") + "." + year.ToString() + "' " +
                                " and p. dat_po >= '" + "01." + month.ToString("00") + "." + year.ToString() + "' ) ");

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }
                #endregion

                #region Выборка из  sprav_otkl_usl

                DbTables tables = new DbTables(con_db);

                sql.Remove(0, sql.Length);
                sql.Append(" select geu,ulica, ndom, nkor, idom, service, nvl(name_supp,'Отсутствует в общем справочнике')  as name_supp, ");
                sql.Append(" count(unique nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                sql.Append(" from  t_sprav_otkl_usl a, ");
                sql.Append(tables.dom + " d,  ");
                sql.Append(tables.ulica + " su, outer ");
                sql.Append(tables.supplier + " s, ");
                sql.Append(tables.services + " se, ");
                sql.Append(tables.geu + " sg ");
                sql.Append(" where a.nzp_dom=d.nzp_dom ");
                sql.Append(" and d.nzp_ul=su.nzp_ul ");
                sql.Append(" and a.nzp_serv=se.nzp_serv ");
                sql.Append(" and a.nzp_supp=s.nzp_supp ");
                sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                sql.Append(" group by 1,2,3,4,5,6,7 ");
                sql.Append(" Order by name_supp, service, ulica, idom, ndom, nkor ");

                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                if (reader2 != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader2, LoadOption.OverwriteChanges);
                }


                //удаление nzp_kvar



                return dt;

                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table t_sprav_otkl_usl; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (reader2 != null)
                {
                    reader2.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

        }



        //справка по отключениям подачи коммунальных услуг
        public DataTable GetDT_SpravkaPoOtklKomUslVinovnik(Prm prm, int id_report)
        {
            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;
            IDataReader reader2 = null;
            IDataReader reader3 = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();


            try
            {
                #region Открытие соединения с БД
                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
#if PG
                string tXX_spls = defaultPgSchema + "." + "t" + prm.nzp_user + "_spls";
#else
                string tXX_spls = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + "t" + prm.nzp_user + "_spls";
#endif
                con_web.Close();
                #endregion

                #region Получение списка префиксов

                ExecRead(con_db, out reader, "drop table sel_kvar31", false);

                sql.Remove(0, sql.Length);
                sql.Append(" select * ");
#if PG
                sql.Append("into unlogged sel_kvar31 from  " + tXX_spls);
#else
        sql.Append(" from  " + tXX_spls + " into temp sel_kvar31 with no log");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);

                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                ExecRead(con_db, out reader, "create index ixselkv_013 on sel_kvar31(num_ls) ", true);
#if PG
                ExecRead(con_db, out reader, "analyze sel_kvar31", true);
#else
                ExecRead(con_db, out reader, "update statistics for table sel_kvar31", true);
#endif

                ExecRead(con_db, out reader, "select pref from sel_kvar31 group by 1 ", true);

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table t_sprav_otkl_usl; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged  table t_sprav_otkl_usl (" +
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
                             " nzp_serv_sg       INTEGER, " +
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
                    return null;
                }

                string last_day_m = prm.year_.ToString() + "-" + prm.month_.ToString("00") + "-" + System.DateTime.DaysInMonth(prm.year_, prm.month_) + " 23:59";
                string first_day_m = prm.year_.ToString() + "-" + prm.month_.ToString("00") + "-01 00:00";

                foreach (string pref in prefix)
                {


                    sql.Remove(0, sql.Length);

                    string dat_month = "01." + prm.month_.ToString("00") + "." + prm.year_;

                    //количество дней в месяце
                    int dayInMonth = DateTime.DaysInMonth(prm.year_, prm.month_);
                    string dbcharge = pref + "_charge_" + prm.year_.ToString().Substring(prm.year_.ToString().Length - 2);

                    #region Выборка текущих начислений недопоставки
#if PG
                    sql.Append(
                                     " select  s.nzp_dom, s.nzp_kvar, nzp_supp, nzp_serv, Date('01." + prm.month_.ToString("00") +
                                     "." + prm.year_.ToString() + "') as dat_month,  " +
                                     " date('01." + prm.month_.ToString("00") +
                                     "." + prm.year_.ToString() + "') as dat_month_end,  " +
                                     " sum(c.sum_nedop) as sum_nedop  " +
                                     " into unlogged t_sum_nedo  from sel_kvar31 s , " +
                                     " " + dbcharge + ".charge_" + prm.month_.ToString("00") + " c " +
                                     " where s.nzp_kvar = c.nzp_kvar " +
                                     " and  c.nzp_serv > 1  " +
                                     " and dat_charge is null " +
                                     "  and  c.sum_nedop > 0.001 ");
#else
              sql.Append(
                               " select  s.nzp_dom, s.nzp_kvar, nzp_supp, nzp_serv, Date('01." + prm.month_.ToString("00") +
                               "." + prm.year_.ToString() + "') as dat_month,  " +
                               " date('01." + prm.month_.ToString("00") +
                               "." + prm.year_.ToString() + "') as dat_month_end,  " +
                               " sum(c.sum_nedop) as sum_nedop  " +
                               " from sel_kvar31 s , " +
                               " " + dbcharge + ":charge_" + prm.month_.ToString("00") + " c " +
                               " where s.nzp_kvar = c.nzp_kvar " +
                               " and  c.nzp_serv > 1  " +
                               " and dat_charge is null " +
                               "  and  c.sum_nedop > 0.001 ");
#endif
                    //добавляем фильтр
                    if (prm.nzp_serv > 0)
                    {
                        sql.Append(" and c.nzp_serv = " + prm.nzp_serv + " ");
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
                        return null;
                    }
                    #endregion


                    #region Выборка перерасчетов прошлого периода
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select month_, year_ ");
                    sql.Append(" from " + dbcharge + ".lnk_charge_" + prm.month_.ToString("00") + " b, sel_kvar31 d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" group by 1,2");
#else
           sql.Append(" select month_, year_ ");
                    sql.Append(" from " + dbcharge + ":lnk_charge_" + prm.month_.ToString("00") + " b, sel_kvar31 d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" group by 1,2");
#endif
                    if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        con_db.Close();
                        ret.result = false;
                        return null;
                    }
                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                        sql.Remove(0, sql.Length);
#if PG
                        sql.Append(" insert into t_sum_nedo (nzp_dom,nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" dat_month, dat_month_end, sum_nedop) ");
                        sql.Append(" select d.nzp_dom, d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" sum(sum_nedop-sum_nedop_p)  ");
                        sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql.Append(" b, sel_kvar31 d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "') and abs(sum_nedop-sum_nedop_p)>0.001");
#else
                        sql.Append(" insert into t_sum_nedo (nzp_dom,nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" dat_month, dat_month_end, sum_nedop) ");
                        sql.Append(" select d.nzp_dom, d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" date('01." + Int32.Parse(reader2["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader2["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" sum(sum_nedop-sum_nedop_p)  ");
                        sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql.Append(" b, sel_kvar31 d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "') and abs(sum_nedop-sum_nedop_p)>0.001");
#endif
                        //добавляем фильтр
                        if (prm.nzp_serv > 0)
                        {
                            sql.Append(" and nzp_serv = " + prm.nzp_serv + " ");
                        }
                        sql.Append(" group by 1,2,3, 4");
                        if (!ExecRead(con_db, out reader3, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            con_db.Close();
                            ret.result = false;
                            return null;
                        }
                        reader3.Close();

                    }
                    reader2.Close();


                    #endregion



#if PG
                    ExecSQL(con_db, "update t_sum_nedo set dat_month_end =  date(dat_month_end)  + interval '1 month '- interval '1 day' ", true);
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
                             " MDY(extract (month from (dat_s))::integer, 01 , extract (year from (dat_s))::integer) as dat_month," +
                             " Round((((EXTRACT (Minute FROM (dat_po)) -  extract (minute from (dat_s)) )||'')::int)/1440,2) as day_nedo " +
                             "  into unlogged t_vinovnik  from   " + pref + "_data.nedop_kvar a, sel_kvar31 d " +
                             " where  a.nzp_kvar=d.nzp_kvar " +
                             " and a.month_calc = date('" + dat_month + "') ");
#else
                    sql.Append(
                             " select  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, (case when a.nzp_serv=17 then u.step else  a.nzp_serv end ) as nzp_serv_sg," +
                             " MDY(month(dat_s),01,year(dat_s)) as dat_month," +
                             " Round(((cast( dat_po  - dat_s " +
                             "  as interval minute(6) to minute)||'')+0)/1440,2) as day_nedo " +
                             " from   " + pref + "_data:nedop_kvar a, sel_kvar31 d, "+pref+"_data:upg_s_nedop_type u" +
                             " where  a.nzp_kvar=d.nzp_kvar and a.nzp_kind=u.nzp_nedop_type " +
                             " and a.month_calc = date('" + dat_month + "') and is_actual = 1 ");
#endif
                    if (prm.nzp_key != -1)
                    {
                        sql.Append(" and a.nzp_supp = " + prm.nzp_key.ToString());
                    }
#if PG
                   
#else
                    sql.Append(" into temp t_vinovnik with no log ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_vinovnik : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }


                    #region Добавляем связанную недопоставку
                    ExecSQL(con_db, "drop table t_v", false);

                    string sqls = " create temp table t_v (" +
                                  " nzp_kvar integer, " +
                                  " nzp_serv integer," +
                                  " vinovnik integer," +
                                  " nzp_serv_sg integer," +
                                  " dat_month Date," +
                                  " day_nedo Decimal(14,2))" + DBManager.sUnlogTempTable;
                    ExecSQL(con_db, sqls, true);

                    sqls = " insert into t_v (nzp_kvar, nzp_serv, vinovnik, nzp_serv_sg, dat_month,  day_nedo)" +
                           " select nzp_kvar, 14, vinovnik, 14, dat_month,  day_nedo " +
                           " from t_vinovnik a " +
                           " where a.nzp_serv = 9 " +
                           "  and a.nzp_kvar not in (select nzp_kvar from t_vinovnik b where b.nzp_serv=14) ";
                    ExecSQL(con_db, sqls, true);
                    

                    sqls = " insert into t_vinovnik (nzp_kvar, nzp_serv, vinovnik, nzp_serv_sg, dat_month,  day_nedo)" +
                       " select nzp_kvar, nzp_serv, vinovnik, nzp_serv_sg, dat_month,  day_nedo " +
                       " from t_v ";
                    ExecSQL(con_db, sqls, true);

                    ExecSQL(con_db, "drop table t_v", true);


                    #endregion

                    //Вычисляем общее число дней недопоставки
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(
                              " select  nzp_kvar,nzp_serv, dat_month," +
                              " sum(day_nedo) as count_daynedo " +
                              " into unlogged t_alldaynedo from  t_vinovnik group by 1,2,3  ");
#else
                   sql.Append(
                             " select  nzp_kvar,nzp_serv, dat_month," +
                             " sum(day_nedo) as count_daynedo " +
                             " from  t_vinovnik group by 1,2,3 into temp t_alldaynedo with no log");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);




                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into t_sprav_otkl_usl (nzp_dom, nzp_kvar, nzp_supp, nzp_vinovnik, nzp_serv,  " +
                                         " count_daynedo, sum_nedop, sum_nedop_all, count_day_all, dat_month) " +
                                         " select nzp_dom, a.nzp_kvar, nzp_supp, vinovnik, a.nzp_serv, day_nedo,  " +
                                         " (case when count_daynedo>0 then sum_nedop*day_nedo/count_daynedo else 0 end), sum_nedop, " +
                                         " count_daynedo, a.dat_month  " +
                                         " from t_sum_nedo a, t_vinovnik b, t_alldaynedo d" +
                                         " where a.nzp_kvar=b.nzp_kvar and a.nzp_kvar=d.nzp_kvar and a.nzp_serv=d.nzp_serv " +
                                         " and a.dat_month=d.dat_month and a.dat_month=b.dat_month " +
                                         " and a.nzp_serv=b.nzp_serv ");
#else
          sql.Append(" insert into t_sprav_otkl_usl (nzp_dom, nzp_kvar, nzp_supp, nzp_vinovnik, nzp_serv_sg, nzp_serv,  " +
                               " count_daynedo, sum_nedop, sum_nedop_all, count_day_all, dat_month) " +
                               " select nzp_dom, a.nzp_kvar, nzp_supp, vinovnik, b.nzp_serv_sg, a.nzp_serv, day_nedo,  " +
                               " (case when count_daynedo>0 then sum_nedop*day_nedo/count_daynedo else 0 end), sum_nedop, " +
                               " count_daynedo, a.dat_month  " +
                               " from t_sum_nedo a, t_vinovnik b, t_alldaynedo d" +
                               " where a.nzp_kvar=b.nzp_kvar and a.nzp_kvar=d.nzp_kvar and a.nzp_serv=d.nzp_serv " +
                               " and a.dat_month=d.dat_month and a.dat_month=b.dat_month " +
                               " and a.nzp_serv=b.nzp_serv ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                    DataTable d1 = DBManager.ExecSQLToTable(con_db, "select * from t_sprav_otkl_usl where nzp_serv=14");

                    ExecSQL(con_db, "drop index ix_tmp756 ", false);
                    ExecSQL(con_db, "create index ix_tmp756 on t_sprav_otkl_usl(nzp_kvar) ", true);
#if PG
                    ExecSQL(con_db, "analyze t_sprav_otkl_usl ", true);
#else
                    ExecSQL(con_db, "update statistics for table t_sprav_otkl_usl ", true);
#endif

                   

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select nzp_kvar, nzp_serv, dat_month, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                               " max(count_day_all)-sum(count_daynedo) as day_nedo " +
                               " into unlogged  t_corr from t_sprav_otkl_usl " +
                               "    group by 1,2,3 ");
#else
                    sql.Append(" select nzp_kvar, nzp_serv, dat_month, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                               " max(nzp_serv_sg) as max_serv_sg, max(count_day_all)-sum(count_daynedo) as day_nedo " +
                               " from t_sprav_otkl_usl " +
                               " group by 1,2,3 " +
                               " into temp t_corr with no log    ");
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_corr : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" update t_sprav_otkl_usl set sum_nedop = sum_nedop+ coalesce((select sum(sum_nedop) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month),0), " +
                               " count_daynedo = count_daynedo + coalesce((select sum(day_nedo) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv),0) " +
                               " where sum_nedop>0 " +
                               " and 1=(select 1 from t_corr " +
                               " where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month " +
                               " and t_sprav_otkl_usl.nzp_vinovnik=t_corr.max_vin)  ");
#else
                    sql.Append(" update t_sprav_otkl_usl set sum_nedop = sum_nedop+ nvl((select sum(sum_nedop) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month),0), " +
                               " count_daynedo = count_daynedo + nvl((select sum(day_nedo) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv),0) " +
                               " where sum_nedop>0 " +
                               " and 1=(select 1 from t_corr " +
                               " where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month " +
                               " and t_sprav_otkl_usl.nzp_vinovnik=t_corr.max_vin"+
                               " and t_sprav_otkl_usl.nzp_serv_sg=t_corr.max_serv_sg)  ");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_corr : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    ret = ExecSQL(con_db, "drop table t_corr", true);
                    ret = ExecSQL(con_db, "drop table t_sum_nedo", true);
                    ret = ExecSQL(con_db, "drop table t_vinovnik", true);
                    ret = ExecSQL(con_db, "drop table t_alldaynedo", false);



                    sql.Remove(0, sql.Length);

                    sql.Append(
                        " insert into t_sprav_otkl_usl (nzp_dom, nzp_kvar, nzp_supp, nzp_vinovnik, nzp_serv_sg, nzp_serv,  " +
                        " count_daynedo, sum_nedop, sum_nedop_all, count_day_all, dat_month) " +
                        " select nzp_dom, p.nzp_kvar, nzp_supp, nzp_supp, nzp_serv, nzp_serv, 0,  " +
                        " sum(-sum_rcl), sum(-sum_rcl), " +
                        " 0, today  " +
                        " FROM " + dbcharge + DBManager.tableDelimiter + "perekidka p, sel_kvar31 kv " +
                        " WHERE p.nzp_kvar=kv.nzp_kvar " +
                        " AND type_rcl = 101 " +
                        " AND month_=" + prm.month_ +
                        " AND ABS(sum_rcl)>0.001 " +
                        " GROUP BY 1,2,3,4,5,6,7,10,11 ");

                    ExecSQL(con_db, sql.ToString(), true);
                

                    #region вставка количества жильцов
                    sql.Remove(0, sql.Length);

                    sql.Append(" update  t_sprav_otkl_usl set col_gil = (   " +
                                " select  max(cnt2-val5+val3)  " +
                                " from  " + dbcharge + DBManager.tableDelimiter + "gil_" + prm.month_.ToString("00") + " p " +
                                " where t_sprav_otkl_usl.nzp_kvar = p.nzp_kvar and stek=3  )" +
                                " where nzp_kvar in (select nzp_kvar from " + dbcharge + DBManager.tableDelimiter + "gil_" + prm.month_.ToString("00") + " p " +
                                " where stek=3)");
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                    #endregion

                }
                #endregion

                #region Выборка из  sprav_otkl_usl

                DbTables tables = new DbTables(con_db);

                ExecSQL(con_db, "drop table t9_supp", false);
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select nzp_kvar, nzp_supp  into unlogged t9_supp from t_sprav_otkl_usl where nzp_serv = 9 group by 1,2 " +
                           "  ; ");
#else
                sql.Append(" select nzp_kvar, nzp_supp from t_sprav_otkl_usl where nzp_serv = 9 group by 1,2 " +
                           " into temp t9_supp with no log; ");
#endif
                ExecSQL(con_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_sprav_otkl_usl set nzp_serv = 9, count_daynedo = 0, col_gil = 0  " +
                                    " where 0<(select count(*) from t9_supp where t9_supp.nzp_kvar=t_sprav_otkl_usl.nzp_kvar " +
                                    " and t9_supp.nzp_supp=t_sprav_otkl_usl.nzp_supp) and nzp_serv=14 ");
#else
       sql.Append(" update t_sprav_otkl_usl set nzp_serv = 9, count_daynedo = 0, col_gil = 0  " +
                           " where 0<(select count(*) from t9_supp where t9_supp.nzp_kvar=t_sprav_otkl_usl.nzp_kvar " +
                           " and t9_supp.nzp_supp=t_sprav_otkl_usl.nzp_supp) and nzp_serv=14 ");
#endif
                ExecSQL(con_db, sql.ToString(), true);

                ExecSQL(con_db, "drop table t9_supp", true);

                if (id_report == 1)
                {
#if PG
                    sql.Remove(0, sql.Length);
                    sql.Append(" select geu,ulica, ndom, nkor, idom, service, s1.name_supp as vinovnik, coalesce(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, ");
                    sql.Append(" count(DISTINCT nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                    sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                    sql.Append(" from  ");
                    sql.Append(tables.dom + " d,  ");
                    sql.Append(tables.ulica + " su,  t_sprav_otkl_usl a left outer join ");
                    sql.Append(tables.supplier + " s on    a.nzp_serv=se.nzp_serv,  ");
                    sql.Append(tables.supplier + " s1,  ");
                    sql.Append(tables.services + " se,  ");
                    sql.Append(tables.geu + " sg ");
                    sql.Append(" where a.nzp_dom=d.nzp_dom ");
                    sql.Append(" and d.nzp_ul=su.nzp_ul ");
             
                    sql.Append(" and a.nzp_supp=s.nzp_supp ");
                    sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                    sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                    if (prm.nzp_key != -1)
                        sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                    sql.Append(" group by 1,2,3,4,5,6,7,8 ");
                    sql.Append(" Order by 8,7, service, ulica, idom, ndom, nkor ");
#else
                    sql.Remove(0, sql.Length);
                    sql.Append(" select geu,ulica, ndom, nkor, idom, se.service, s1.name_supp as vinovnik, se1.service as stat_calc, ");
                    sql.Append(" nvl(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, ");
                    sql.Append(" count(unique nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                    sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                    sql.Append(" from  t_sprav_otkl_usl a, ");
                    sql.Append(tables.dom + " d,  ");
                    sql.Append(tables.ulica + " su, outer ");
                    sql.Append(tables.supplier + " s,  ");
                    sql.Append(tables.supplier + " s1,  ");
                    sql.Append(tables.services + " se,  ");
                    sql.Append(tables.services + " se1,  ");
                    sql.Append(tables.geu + " sg ");
                    sql.Append(" where a.nzp_dom=d.nzp_dom ");
                    sql.Append(" and d.nzp_ul=su.nzp_ul ");
                    sql.Append(" and a.nzp_serv=se.nzp_serv ");
                    sql.Append(" and a.nzp_serv_sg=se1.nzp_serv ");
                    sql.Append(" and a.nzp_supp=s.nzp_supp ");
                    sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                    sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                    if (prm.nzp_key != -1)
                        sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                    sql.Append(" group by 1,2,3,4,5,6,7,8,9 ");
                    sql.Append(" Order by 8,7, service, ulica, idom, ndom, nkor ");
#endif
                }
                else
                    if (id_report == 2)
                    {
                        sql.Remove(0, sql.Length);
                        #if PG
                        sql.Append(" select geu, service, coalesce(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, ");
                        sql.Append(" ulica, ndom, nkor, idom, ");
                        sql.Append(" count(DISTINCT nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                        sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                        sql.Append(" from  ");
                        sql.Append(tables.dom + " d,  ");
                        sql.Append(tables.ulica + " su, t_sprav_otkl_usl a left outer join ");
                        sql.Append(tables.supplier + " s on a.nzp_supp=s.nzp_supp, ");
                        sql.Append(tables.supplier + " s1, ");
                        sql.Append(tables.services + " se, ");
                        sql.Append(tables.geu + " sg ");
                        sql.Append(" where a.nzp_dom=d.nzp_dom ");
                        sql.Append(" and d.nzp_ul=su.nzp_ul ");
                        sql.Append(" and a.nzp_serv=se.nzp_serv ");
               
                        sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                        sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                        if (prm.nzp_key != -1)
                            sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                        sql.Append(" group by 1,2,3,4,5,6,7 ");
                        sql.Append(" Order by 3,2, service, 1, ulica, idom, ndom, nkor");
 
#else
                              sql.Append(" select geu, se.service, nvl(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, se1.service as stat_calc, ");
                        sql.Append(" ulica, ndom, nkor, idom, ");
                        sql.Append(" count(unique nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                        sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                        sql.Append(" from  t_sprav_otkl_usl a, ");
                        sql.Append(tables.dom + " d,  ");
                        sql.Append(tables.ulica + " su, outer ");
                        sql.Append(tables.supplier + " s, ");
                        sql.Append(tables.supplier + " s1, ");
                        sql.Append(tables.services + " se, ");
                        sql.Append(tables.services + " se1, ");
                        sql.Append(tables.geu + " sg ");
                        sql.Append(" where a.nzp_dom=d.nzp_dom ");
                        sql.Append(" and d.nzp_ul=su.nzp_ul ");
                        sql.Append(" and a.nzp_serv=se.nzp_serv ");
                        sql.Append(" and a.nzp_serv_sg=se1.nzp_serv ");
                        sql.Append(" and a.nzp_supp=s.nzp_supp ");
                        sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                        sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                        if (prm.nzp_key != -1)
                            sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                        sql.Append(" group by 1,2,3,4,5,6,7,8 ");
                        sql.Append(" Order by 3,2, service, 1, ulica, idom, ndom, nkor");
 
#endif



                    }
                    else
                        if (id_report == 3)
                        {
                            sql.Remove(0, sql.Length);
                            #if PG

                            sql.Append(" select geu, service, s1.name_supp as vinovnik, coalesce(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, ");
                            sql.Append(" count(DISTINCT nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                            sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                            sql.Append(" from  ");
                            sql.Append(tables.dom + " d,  ");
                            sql.Append(tables.ulica + " su,  t_sprav_otkl_usl a left  outer join ");
                            sql.Append(tables.supplier + " s on a.nzp_supp=s.nzp_supp,   ");
                            sql.Append(tables.supplier + " s1, ");
                            sql.Append(tables.services + " se, ");
                            sql.Append(tables.geu + " sg ");
                            sql.Append(" where a.nzp_dom=d.nzp_dom ");
                            sql.Append(" and d.nzp_ul=su.nzp_ul ");
                            sql.Append(" and a.nzp_serv=se.nzp_serv ");
                         
                            sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                            sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                            if (prm.nzp_key != -1)
                                sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                            sql.Append(" group by 1,2,3,4 ");
                            sql.Append(" Order by 4,3, service, 1");
#else
  
  sql.Append(" select geu, se.service, s1.name_supp as vinovnik, nvl(s.name_supp,'Отсутствует в общем справочнике')  as name_supp," +
             "          se1.service as stat_calc,  ");
                            sql.Append(" count(unique nzp_kvar) as count_kvar, sum(count_daynedo) as count_daynedo, ");
                            sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                            sql.Append(" from  t_sprav_otkl_usl a, ");
                            sql.Append(tables.dom + " d,  ");
                            sql.Append(tables.ulica + " su, outer ");
                            sql.Append(tables.supplier + " s, ");
                            sql.Append(tables.supplier + " s1, ");
                            sql.Append(tables.services + " se, ");
                            sql.Append(tables.services + " se1, ");
                            sql.Append(tables.geu + " sg ");
                            sql.Append(" where a.nzp_dom=d.nzp_dom ");
                            sql.Append(" and d.nzp_ul=su.nzp_ul ");
                            sql.Append(" and a.nzp_serv=se.nzp_serv ");
                            sql.Append(" and a.nzp_serv_sg=se1.nzp_serv ");
                            sql.Append(" and a.nzp_supp=s.nzp_supp ");
                            sql.Append(" and a.nzp_vinovnik=s1.nzp_supp ");
                            sql.Append(" and d.nzp_geu=sg.nzp_geu ");
                            if (prm.nzp_key != -1)
                                sql.Append(" and a.nzp_vinovnik = " + prm.nzp_key.ToString(""));
                            sql.Append(" group by 1,2,3,4,5 ");
                            sql.Append(" Order by 4,3, service, 1");
#endif


                        }
                        else
                            if (id_report == 4)
                            {
                                sql.Remove(0, sql.Length);
#if PG
                                sql.Append(" select coalesce(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, service, geu,  ");
                                sql.Append(" adr, sum(1) as countKvar, sum(count_daynedo) as count_daynedo, sum(0) as count_kvarchas,");
                                sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                                sql.Append(" from   ");
                                sql.Append("sel_kvar31 k, t_sprav_otkl_usl a ");
                                sql.Append(" left outer join ");
                                sql.Append(tables.supplier + " s on a.nzp_supp=s.nzp_supp, ");
                                sql.Append(tables.services + " se, ");
                                sql.Append(tables.geu + " sg ");
                                sql.Append(" where a.nzp_kvar=k.nzp_kvar  ");
                                sql.Append(" and a.nzp_serv=se.nzp_serv ");
                       
                                sql.Append(" and k.nzp_geu=sg.nzp_geu ");
                                sql.Append(" group by 1,2,3,4 ");
                                sql.Append(" Order by 1,2,3,4 ");
#else
 sql.Append(" select nvl(s.name_supp,'Отсутствует в общем справочнике')  as name_supp, se.service, geu, se1.service as stat_calc,   ");
                                sql.Append(" adr, sum(1) as countKvar, sum(count_daynedo) as count_daynedo, sum(0) as count_kvarchas,");
                                sql.Append(" sum(col_gil) as count_gil, sum(sum_nedop) as sum_nedop ");
                                sql.Append(" from  t_sprav_otkl_usl a, ");
                                sql.Append("sel_kvar31 k,  ");
                                sql.Append(" outer ");
                                sql.Append(tables.supplier + " s, ");
                                sql.Append(tables.services + " se, ");
                                sql.Append(tables.services + " se1, ");
                                sql.Append(tables.geu + " sg ");
                                sql.Append(" where a.nzp_kvar=k.nzp_kvar  ");
                                sql.Append(" and a.nzp_serv=se.nzp_serv ");
                                sql.Append(" and a.nzp_serv_sg=se1.nzp_serv ");
                                sql.Append(" and a.nzp_supp=s.nzp_supp ");
                                sql.Append(" and k.nzp_geu=sg.nzp_geu ");
                                sql.Append(" group by 1,2,3,4, 5 ");
                                sql.Append(" Order by 1,2,3,4, 5 ");
#endif

                            }



                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                if (reader2 != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader2, LoadOption.OverwriteChanges);
                }


                //удаление nzp_kvar



                return dt;

                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table t_sprav_otkl_usl; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (reader2 != null)
                {
                    reader2.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

        }




        //справка по отключениям подачи коммунальных услуг
        public void GetSumNedopByStatCalc(IDbConnection conn_db, string temp_table, Prm prm)
        {
            Returns ret = Utils.InitReturns();

            


            MyDataReader reader = null;
            
            
            var sql = new StringBuilder();

            var prefix = new List<string>();


            try
            {


                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(conn_db, " Drop table t_sprav_otkl_usl; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_sprav_otkl_usl (" +
                             " nzp_kvar          INTEGER, " +
                             " nzp_supp          INTEGER, " +
                             " nzp_vinovnik      INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " nzp_serv_sg       INTEGER, " +
                             " dat_month         DATE, " +
                             " countKvar         INTEGER, " +
                             " count_daynedo     DECIMAL(14,2), " +
                             " count_day_all     DECIMAL(14,2), " +
                             " count_kvarchas    INTEGER DEFAULT 0, " +
                             " col_gil           INTEGER, " +
                             " sum_nedop         DECIMAL(14,2), " +
                             " sum_nedop_p       DECIMAL(14,2), " +
                             " sum_nedop_all     DECIMAL(14,2) " +
                             " ) With no log; "
                          );


                ret = ExecSQL(conn_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы sprav_otkl_usl : " + ret.text, MonitorLog.typelog.Error, true);
                    return;
                }

                #region Получение списка префиксов

                ExecRead(conn_db, out reader, "select pref from " + temp_table + " group by 1 ", true);

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                reader.Close();
                #endregion

                string last_day_m = prm.year_.ToString() + "-" + prm.month_.ToString("00") + "-" + System.DateTime.DaysInMonth(prm.year_, prm.month_) + " 23:59";
                string first_day_m = prm.year_.ToString() + "-" + prm.month_.ToString("00") + "-01 00:00";

                foreach (string pref in prefix)
                {


                    sql.Remove(0, sql.Length);

                    string dat_month = "01." + prm.month_.ToString("00") + "." + prm.year_;

                    //количество дней в месяце
                    int dayInMonth = DateTime.DaysInMonth(prm.year_, prm.month_);
                    string dbcharge = pref + "_charge_" + prm.year_.ToString().Substring(prm.year_.ToString().Length - 2);

                    #region Выборка текущих начислений недопоставки
                    sql.Append(
                                     " select   s.nzp_kvar, nzp_supp, nzp_serv, Date('01." + prm.month_.ToString("00") +
                                     "." + prm.year_.ToString() + "') as dat_month,  " +
                                     " date('01." + prm.month_.ToString("00") +
                                     "." + prm.year_.ToString() + "') as dat_month_end,  " +
                                     " sum(c.sum_nedop) as sum_nedop, sum(cast (0 as Decimal(14,2))) as sum_nedop_p " +
                                     " from " + temp_table + " s , " +
                                     " " + dbcharge + ":charge_" + prm.month_.ToString("00") + " c " +
                                     " where s.nzp_kvar = c.nzp_kvar " +
                                     " and  c.nzp_serv = 17  " +
                                     " and dat_charge is null " +
                                     "  and  c.sum_nedop > 0.001 "+
                                     " group by 1,2,3,4 into temp t_sum_nedo with no log ");

                    ret = ExecSQL(conn_db, sql.ToString(), true);
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
                    sql.Append(" from " + dbcharge + ":lnk_charge_" + prm.month_.ToString("00") + " b, "+temp_table+" d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" group by 1,2");
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        return ;
                    }
                    while (reader.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader["year_"].ToString()) - 2000).ToString("00");

                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_sum_nedo (nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" dat_month, dat_month_end, sum_nedop, sum_nedop_p) ");
                        sql.Append(" select d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql.Append(" date('01." + Int32.Parse(reader["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" date('01." + Int32.Parse(reader["month_"].ToString()).ToString("00") + "." +
                                    Int32.Parse(reader["year_"].ToString()).ToString() + "'), ");
                        sql.Append(" sum(sum_nedop-sum_nedop_p),sum(sum_nedop-sum_nedop_p)  ");
                        sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader["month_"].ToString()).ToString("00"));
                        sql.Append(" b, " + temp_table + " d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "') and abs(sum_nedop-sum_nedop_p)>0.001");
                        //добавляем фильтр
                        sql.Append(" group by 1,2,3");
                        ExecSQL(conn_db, sql.ToString(), true);

                    }
                    reader.Close();


                    #endregion



                    ExecSQL(conn_db, "update t_sum_nedo set dat_month_end =  date(dat_month_end) + 1 units month - 1 units day ", true);
                    ExecSQL(conn_db, "create index ix_tmp753 on t_sum_nedo(nzp_kvar, nzp_serv) ", true);
                    ExecSQL(conn_db, "update statistics for table t_sum_nedo ", true);

                    sql.Remove(0, sql.Length);
                    sql.Append(
                             " select  a.nzp_kvar, a.nzp_serv, a.nzp_supp as vinovnik, (case when a.nzp_serv=17 then u.step else  a.nzp_serv end ) as nzp_serv_sg," +
                             " MDY(month(dat_s),01,year(dat_s)) as dat_month," +
                             " Round(((cast( dat_po  - dat_s " +
                             "  as interval minute(6) to minute)||'')+0)/1440,2) as day_nedo " +
                             " from   " + pref + "_data:nedop_kvar a, "+temp_table+" d, " + pref + "_data:upg_s_nedop_type u" +
                             " where  a.nzp_kvar=d.nzp_kvar and a.nzp_kind=u.nzp_nedop_type " +
                             " and a.month_calc = date('" + dat_month + "') and is_actual = 1 ");
                
                    sql.Append(" into temp t_vinovnik with no log ");

                    ret = ExecSQL(conn_db, sql.ToString(), true);
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
                              " from  t_vinovnik group by 1,2,3 into temp t_alldaynedo with no log");

                    ret = ExecSQL(conn_db, sql.ToString(), true);




                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_sprav_otkl_usl ( nzp_kvar, nzp_supp, nzp_vinovnik, nzp_serv_sg, nzp_serv,  " +
                                         " count_daynedo, sum_nedop, sum_nedop_p,sum_nedop_all, count_day_all, dat_month) " +
                                         " select a.nzp_kvar, nzp_supp, vinovnik, b.nzp_serv_sg, a.nzp_serv, day_nedo,  " +
                                         " (case when count_daynedo>0 then sum_nedop*day_nedo/count_daynedo else 0 end), "+
                                         " sum_nedop_p, " +
                                         " sum_nedop, " +
                                         " count_daynedo, a.dat_month  " +
                                         " from t_sum_nedo a, t_vinovnik b, t_alldaynedo d" +
                                         " where a.nzp_kvar=b.nzp_kvar and a.nzp_kvar=d.nzp_kvar and a.nzp_serv=d.nzp_serv " +
                                         " and a.dat_month=d.dat_month and a.dat_month=b.dat_month " +
                                         " and a.nzp_serv=b.nzp_serv ");

                    ExecSQL(conn_db, sql.ToString(), true);
                    

                    ExecSQL(conn_db, "drop index ix_tmp756 ", false);
                    ExecSQL(conn_db, "create index ix_tmp756 on t_sprav_otkl_usl(nzp_kvar) ", true);
                    ExecSQL(conn_db, "update statistics for table t_sprav_otkl_usl ", true);

                    //вставка количества жильцов
                    sql.Remove(0, sql.Length);


                    sql.Remove(0, sql.Length);
                    sql.Append(" select nzp_kvar, nzp_serv, dat_month, max(sum_nedop_all)-sum(sum_nedop) as sum_nedop, max(nzp_vinovnik) as max_vin, " +
                               " max(nzp_serv_sg) as max_serv_sg, max(count_day_all)-sum(count_daynedo) as day_nedo " +
                               " from t_sprav_otkl_usl " +
                               " group by 1,2,3 " +
                               " into temp t_corr with no log    ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы t_corr : " + ret.text, MonitorLog.typelog.Error, true);
                        return;
                    }
                    sql.Remove(0, sql.Length);
                    sql.Append(" update t_sprav_otkl_usl set sum_nedop = sum_nedop+ nvl((select sum(sum_nedop) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month),0), " +
                               " count_daynedo = count_daynedo + nvl((select sum(day_nedo) " +
                               " from t_corr where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv),0) " +
                               " where sum_nedop>0 " +
                               " and 1=(select 1 from t_corr " +
                               " where t_sprav_otkl_usl.nzp_kvar=t_corr.nzp_kvar " +
                               " and t_sprav_otkl_usl.nzp_serv=t_corr.nzp_serv and t_sprav_otkl_usl.dat_month=t_corr.dat_month " +
                               " and t_sprav_otkl_usl.nzp_vinovnik=t_corr.max_vin" +
                               " and t_sprav_otkl_usl.nzp_serv_sg=t_corr.max_serv_sg)  ");
                    ExecSQL(conn_db, sql.ToString(), true);
                    //проверка на успех вставки

                    ExecSQL(conn_db, "drop table t_corr", true);
                    ExecSQL(conn_db, "drop table t_sum_nedo", true);
                    ExecSQL(conn_db, "drop table t_vinovnik", true);
                    ExecSQL(conn_db, "drop table t_alldaynedo", false);

                }
                #endregion



            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
            }

        }


        //Справка по поставщикам коммунальных услуг ф.3
        public DataTable GetSpravSuppSamara(Prm prm)
        {
            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;
            IDataReader reader2 = null;
            IDataReader reader3 = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();


            try
            {
                #region Открытие соединения с БД
                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
#if PG
                string tXX_spls = defaultPgSchema + "." + "t" + prm.nzp_user + "_spls";
#else
                string tXX_spls = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + "t" + prm.nzp_user + "_spls";
#endif
                con_web.Close();
                #endregion

                #region Получение списка префиксов

                ExecRead(con_db, out reader, "drop table sel_kvar31", false);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select * ");
                sql.Append(" into unlogged  sel_kvar31  from  " + tXX_spls );
#else
                sql.Append(" select * ");
                sql.Append(" from  " + tXX_spls + " into temp sel_kvar31 with no log");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);

                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                ExecRead(con_db, out reader, "create index ixselkv_013 on sel_kvar31(num_ls) ", true);
#if PG
                ExecRead(con_db, out reader, "analyze sel_kvar31", true);
#else
              ExecRead(con_db, out reader, "update statistics for table sel_kvar31", true);
#endif

                ExecRead(con_db, out reader, "select pref from sel_kvar31 group by 1 ", true);

                while (reader.Read())
                {
                    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                }
                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table t_svod; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table t_svod (" +
                             " nzp_supp          INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " pl_izol     NUMERIC(14,2), " +
                             " pl_komm     NUMERIC(14,2), " +
                             " count_ls    NUMERIC(14,2), " +
                             " count_gil   NUMERIC(14,2), " +
                             " nzp_frm     INTEGER DEFAULT 0, " +
                             " nzp_measure INTEGER DEFAULT 0, " +
                             " c_calc      NUMERIC(14,4), " +
                             " rsum_tarif  NUMERIC(14,2), " +
                             " sum_nedop   NUMERIC(14,2), " +
                             " reval_k     NUMERIC(14,2), " +
                             " reval_d     NUMERIC(14,2), " +
                             " sum_ito     NUMERIC(14,2), " +
                             " sum_charge  NUMERIC(14,2), " +
                             " sum_money   NUMERIC(14,2) ) ; "
                          );
#else
                sql.Append(" create temp table t_svod (" +
                             " nzp_supp          INTEGER, " +
                             " nzp_serv          INTEGER, " +
                             " pl_izol     DECIMAL(14,2), " +
                             " pl_komm     DECIMAL(14,2), " +
                             " count_ls    DECIMAL(14,2), " +
                             " count_gil   DECIMAL(14,2), " +
                             " nzp_frm     INTEGER DEFAULT 0, " +
                             " nzp_measure INTEGER DEFAULT 0, " +
                             " c_calc      DECIMAL(14,4), " +
                             " rsum_tarif  DECIMAL(14,2), " +
                             " sum_nedop   DECIMAL(14,2), " +
                             " reval_k     DECIMAL(14,2), " +
                             " reval_d     DECIMAL(14,2), " +
                             " sum_ito     DECIMAL(14,2), " +
                             " sum_charge  DECIMAL(14,2), " +
                             " sum_money   DECIMAL(14,2) ) With no log; "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);


                ret = ExecSQL(con_db, " Drop table t_perekidka; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table t_perekidka (" +
                                    " nzp_supp          INTEGER, " +
                                    " sum_perekidka   NUMERIC(14,2) )  ; "
                                 );
#else
         sql.Append(" create temp table t_perekidka (" +
                             " nzp_supp          INTEGER, " +
                             " sum_perekidka   DECIMAL(14,2) ) With no log; "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);


                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы t_svod : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }


                foreach (string pref in prefix)
                {
                    string sAliasm = pref + "_charge_" + (prm.year_ - 2000).ToString("00");
                    string dat = "01." + prm.month_ + "." + prm.year_;


                    //Выбираем площадь для расчетов
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select a.nzp_kvar, nzp_serv, max(round(gil,0)) as count_gil, ");
                    sql.Append(" max(squ) as pl_kvar, 0 as izol ");
                    sql.Append("  into unlogged t13  from " + sAliasm + ".calc_gku_");
                    sql.Append(prm.month_.ToString("00") + " a, sel_kvar31  k ");
                    sql.Append(" where k.nzp_kvar=a.nzp_kvar and a.tarif >0 group by 1,2 ");
            
#else
                    sql.Append(" select a.nzp_kvar, nzp_serv, max(round(gil,0)) as count_gil, ");
                    sql.Append(" max(squ) as pl_kvar, 0 as izol ");
                    sql.Append(" from " + sAliasm + ":calc_gku_");
                    sql.Append(prm.month_.ToString("00") + " a, sel_kvar31  k ");
                    sql.Append(" where k.nzp_kvar=a.nzp_kvar and a.tarif >0 group by 1,2 ");
                    sql.Append(" into temp t13 with no log");
#endif
                    ExecRead(con_db, out reader2, sql.ToString(), true);

                    //Проставляем коммунальные квартиры
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" update t13 set izol =   ");
                    sql.Append(" (select max(2) ");
                    sql.Append(" from " + pref + "_data.prm_1 p ");
                    sql.Append(" where nzp_prm=3 and p.nzp=t13.nzp_kvar ");
                    sql.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "' and val_prm='2')");
#else
                    sql.Append(" update t13 set izol =   ");
                    sql.Append(" (select max(2) ");
                    sql.Append(" from " + pref + "_data:prm_1 p ");
                    sql.Append(" where nzp_prm=3 and p.nzp=t13.nzp_kvar ");
                    sql.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "' and val_prm='2')");
#endif

                    ExecRead(con_db, out reader, sql.ToString(), true);


                    ExecRead(con_db, out reader, "create index ixskv31_01 on t13(nzp_kvar, nzp_serv)", true);
#if PG
                    ExecRead(con_db, out reader, "analyze t13", true);
#else
         ExecRead(con_db, out reader, "update statistics for table t13", true);
#endif

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm, ");
                    sql.Append(" rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money, c_calc)  ");
                    sql.Append(" select a.nzp_supp, a.nzp_serv, a.nzp_frm,");
                    sql.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql.Append(" sum(sum_nedop) as sum_nedop,");
                    sql.Append(" sum(case when real_charge<0 then real_charge else 0 end),");
                    sql.Append(" sum(case when real_charge>0 then real_charge else 0 end),");
                    sql.Append(" sum(sum_charge),");
                    sql.Append(" sum(sum_money),");
                    sql.Append(" max(c_calc) as c_calc");
                    sql.Append(" from " + sAliasm + ".charge_" + prm.month_.ToString("00") + " a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and dat_charge is null ");
                    sql.Append(" and a.nzp_serv>1 and rsum_tarif+abs(sum_charge)+abs(reval)+abs(real_charge)>0.001");
                    sql.Append(" group by 1,2,3");
#else
                    sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm, ");
                    sql.Append(" rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money, c_calc)  ");
                    sql.Append(" select a.nzp_supp, a.nzp_serv, a.nzp_frm,");
                    sql.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql.Append(" sum(sum_nedop) as sum_nedop,");
                    sql.Append(" sum(case when real_charge<0 then real_charge else 0 end),");
                    sql.Append(" sum(case when real_charge>0 then real_charge else 0 end),");
                    sql.Append(" sum(sum_charge),");
                    sql.Append(" sum(sum_money),");
                    sql.Append(" max(c_calc) as c_calc");
                    sql.Append(" from " + sAliasm + ":charge_" + prm.month_.ToString("00") + " a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and dat_charge is null ");
                    sql.Append(" and a.nzp_serv>1 and rsum_tarif+abs(sum_charge)+abs(reval)+abs(real_charge)>0.001");
                    sql.Append(" group by 1,2,3");
#endif
                    ExecRead(con_db, out reader2, sql.ToString(), true);


                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm, pl_izol, pl_komm, count_gil, count_ls) ");
                    sql.Append(" select a.nzp_supp, a.nzp_serv, a.nzp_frm,");
                    sql.Append(" max(case when tarif >0 and coalesce(izol,0) = 0 then pl_kvar else 0 end) as pl_izol, ");
                    sql.Append(" max(case when tarif >0 and coalesce(izol,0) = 2 then pl_kvar else 0 end) as pl_komm, ");
                    sql.Append(" max(case when tarif >0 then count_gil else 0 end) as count_gil, ");
                    sql.Append(" count(distinct num_ls) as count_gil ");
                    sql.Append(" from " + sAliasm + ".charge_" + prm.month_.ToString("00") + " a, t13 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and dat_charge is null ");
                    sql.Append(" and rsum_tarif+abs(sum_charge)+abs(reval)+abs(real_charge)>0.001");
                    sql.Append(" and a.nzp_serv=b.nzp_serv and a.nzp_serv>1 ");
                    sql.Append(" group by 1,2,3");
#else
                    sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm, pl_izol, pl_komm, count_gil, count_ls) ");
                    sql.Append(" select a.nzp_supp, a.nzp_serv, a.nzp_frm,");
                    sql.Append(" max(case when tarif >0 and nvl(izol,0) = 0 then pl_kvar else 0 end) as pl_izol, ");
                    sql.Append(" max(case when tarif >0 and nvl(izol,0) = 2 then pl_kvar else 0 end) as pl_komm, ");
                    sql.Append(" max(case when tarif >0 then count_gil else 0 end) as count_gil, ");
                    sql.Append(" count(unique num_ls) as count_gil ");
                    sql.Append(" from " + sAliasm + ":charge_" + prm.month_.ToString("00") + " a, t13 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and dat_charge is null ");
                    sql.Append(" and rsum_tarif+abs(sum_charge)+abs(reval)+abs(real_charge)>0.001");
                    sql.Append(" and a.nzp_serv=b.nzp_serv and a.nzp_serv>1 ");
                    sql.Append(" group by 1,2,3");
#endif
                    ExecRead(con_db, out reader2, sql.ToString(), true);


                    #region Выборка перерасчетов прошлого периода
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql.Append(" into unlogged  t_nedop  from " + pref + "_data.nedop_kvar a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "' ");
                    sql.Append(" group by 1,2,3  ");
#else
                    sql.Append(" select b.nzp_geu,a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "' ");
                    sql.Append(" group by 1,2 into temp t_nedop with no log");
#endif
                    if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        con_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select month_, year_ ");
                    sql.Append(" from " + sAliasm + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql.Append(" group by 1,2");
#else
                 sql.Append(" select month_, year_ ");
                    sql.Append(" from " + sAliasm + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql.Append(" and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql.Append(" group by 1,2");
#endif
                    if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        con_db.Close();
                        ret.result = false;
                        return null;
                    }
                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                        sql.Remove(0, sql.Length);
#if PG
                        sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm,");
                        sql.Append(" sum_nedop, reval_k, reval_d ) ");
                        sql.Append(" select nzp_supp, nzp_serv, nzp_frm,");
                        sql.Append(" sum(sum_nedop-sum_nedop_p), ");
                        sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql.Append(" b, t_nedop d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "') ");
                        sql.Append(" group by 1,2,3");
#else
                        sql.Append(" insert into t_svod(nzp_supp, nzp_serv, nzp_frm,");
                        sql.Append(" sum_nedop, reval_k, reval_d ) ");
                        sql.Append(" select nzp_supp, nzp_serv, nzp_frm,");
                        sql.Append(" sum(sum_nedop-sum_nedop_p), ");
                        sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql.Append(" b, t_nedop d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "') ");
                        sql.Append(" group by 1,2,3");
#endif
                        if (!ExecRead(con_db, out reader3, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            con_db.Close();
                            ret.result = false;
                            return null;
                        }
                        reader3.Close();

                    }
                    reader2.Close();

                    ExecRead(con_db, out reader3, "drop table t_nedop", true);
                    #endregion

                    //Перекидки ОДН по статье содержание жилья
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into t_perekidka (nzp_supp, sum_perekidka) ");
                    sql.Append("select nzp_supp,-sum(sum_rcl) as reval_k ");
                    sql.Append(" from " + sAliasm + ".perekidka a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and type_rcl = 63 and month_=" + prm.month_.ToString("00"));
                    sql.Append(" and abs(sum_rcl)>0.001");
                    sql.Append(" group by 1 ");
#else
                 sql.Append("insert into t_perekidka (nzp_supp, sum_perekidka) ");
                    sql.Append("select nzp_supp,-sum(sum_rcl) as reval_k ");
                    sql.Append(" from " + sAliasm + ":perekidka a, sel_kvar31 b ");
                    sql.Append(" where a.nzp_kvar=b.nzp_kvar and type_rcl = 63 and month_=" + prm.month_.ToString("00"));
                    sql.Append(" and abs(sum_rcl)>0.001");
                    sql.Append(" group by 1 ");
#endif
                    if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        con_db.Close();
                        ret.result = false;
                        return null;
                    }



                    ExecRead(con_db, out reader2, "drop table t13", true);
                }




                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_svod set nzp_measure = (select max(nzp_measure)");
                sql.Append(" from " + Points.Pref + "_kernel.formuls a where a.nzp_frm=t_svod.nzp_frm)");
#else
               sql.Append(" update t_svod set nzp_measure = (select max(nzp_measure)");
                sql.Append(" from " + Points.Pref + "_kernel:formuls a where a.nzp_frm=t_svod.nzp_frm)");
#endif
                ExecRead(con_db, out reader2, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_svod set nzp_measure = (select max(nzp_measure)");
                sql.Append(" from " + Points.Pref + "_kernel.services a where a.nzp_serv=t_svod.nzp_serv)");
#else
        sql.Append(" update t_svod set nzp_measure = (select max(nzp_measure)");
                sql.Append(" from " + Points.Pref + "_kernel:services a where a.nzp_serv=t_svod.nzp_serv)");
#endif
                ExecRead(con_db, out reader2, sql.ToString(), true);

                //Вычитаем ОДН из содержания жилья
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_svod set rsum_tarif = rsum_tarif - coalesce((select sum(sum_perekidka)");
                sql.Append(" from t_perekidka a where a.nzp_supp=t_svod.nzp_supp),0) ");
                sql.Append(" where nzp_serv=17 and nzp_supp=(select max(nzp_supp) ");
                sql.Append(" from t_svod where nzp_serv=17 and rsum_tarif>0.001)");
#else
                sql.Append(" update t_svod set rsum_tarif = rsum_tarif - nvl((select sum(sum_perekidka)");
                sql.Append(" from t_perekidka a where a.nzp_supp=t_svod.nzp_supp),0) ");
                sql.Append(" where nzp_serv=17 and nzp_supp=(select max(nzp_supp) ");
                sql.Append(" from t_svod where nzp_serv=17 and rsum_tarif>0.001)");
#endif
                ExecRead(con_db, out reader2, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_svod set reval_k = reval_k + coalesce((select sum(sum_perekidka)");
                sql.Append(" from t_perekidka a where a.nzp_supp=t_svod.nzp_supp),0) ");
                sql.Append(" where nzp_serv=17 and nzp_supp=(select max(nzp_supp) ");
                sql.Append(" from t_svod where nzp_serv=17 and rsum_tarif>0.001)");
#else
                sql.Append(" update t_svod set reval_k = reval_k + nvl((select sum(sum_perekidka)");
                sql.Append(" from t_perekidka a where a.nzp_supp=t_svod.nzp_supp),0) ");
                sql.Append(" where nzp_serv=17 and nzp_supp=(select max(nzp_supp) ");
                sql.Append(" from t_svod where nzp_serv=17 and rsum_tarif>0.001)");
#endif
                ExecRead(con_db, out reader2, sql.ToString(), true);




                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select 1 as ord, name_supp, service, measure, s.nzp_serv, ");
                sql.Append(" sum(pl_izol) as pl_izol, ");
                sql.Append(" sum(pl_komm) as pl_komm, ");
                sql.Append(" sum(count_gil) as count_gil, ");
                sql.Append(" sum(count_ls) as count_ls, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(c_calc,0) end) as c_calc,");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(rsum_tarif,0) end) as rsum_tarif, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_nedop,0) end ) as sum_nedop, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_charge,0) end) as sum_charge, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_money,0) end) as sum_money, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else -1*coalesce(reval_k,0) end) as reval_k, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(reval_d,0) end) as reval_d, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(c_calc,0) else 0 end) as c_calc_odn,");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(rsum_tarif,0) else 0 end) as rsum_tarif_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_nedop,0) else 0 end) as sum_nedop_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_charge,0) else 0 end) as sum_charge_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then -1*coalesce(reval_k,0) else 0 end) as reval_k_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(reval_d,0) else 0 end) as reval_d_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_money,0) else 0 end) as sum_money_odn ");
                sql.Append(" from t_svod a, " + Points.Pref + "_kernel");
                sql.Append( DBManager.getServer(con_db) + ".s_measure g,   " + Points.Pref + "_kernel");
                sql.Append(  DBManager.getServer(con_db) + ".services s," + Points.Pref + "_kernel");
                sql.Append(  DBManager.getServer(con_db) + ".supplier su");
                sql.Append(" where a.nzp_measure=g.nzp_measure and a.nzp_supp not in (1,1000)");
                sql.Append(" and (case when a.nzp_serv = 515 then 25 else a.nzp_serv end) =s.nzp_serv  and a.nzp_supp=su.nzp_supp ");
                sql.Append(" group by 1,2,3,4,5 ");
                sql.Append(" union all ");
                sql.Append(" select 2 as ord, 'Всего' as name_supp, service, measure, s.nzp_serv, ");
                sql.Append(" sum(pl_izol) as pl_izol, ");
                sql.Append(" sum(pl_komm) as pl_komm, ");
                sql.Append(" sum(count_gil) as count_gil, ");
                sql.Append(" sum(count_ls) as count_ls, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(c_calc,0) end) as c_calc,");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(rsum_tarif,0) end) as rsum_tarif, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_nedop,0) end ) as sum_nedop, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_charge,0) end) as sum_charge, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(sum_money,0) end) as sum_money, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else -1*coalesce(reval_k,0) end) as reval_k, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then 0 else coalesce(reval_d,0) end) as reval_d, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(c_calc,0) else 0 end) as c_calc_odn,");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(rsum_tarif,0) else 0 end) as rsum_tarif_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_nedop,0) else 0 end) as sum_nedop_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_charge,0) else 0 end) as sum_charge_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then -1*coalesce(reval_k,0) else 0 end) as reval_k_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(reval_d,0) else 0 end) as reval_d_odn, ");
                sql.Append(" sum(case when coalesce(a.nzp_serv,0) = 515 then coalesce(sum_money,0) else 0 end) as sum_money_odn ");
                sql.Append(" from t_svod a, " + Points.Pref + "_kernel");
                sql.Append(  DBManager.getServer(con_db) + ".s_measure g,   " + Points.Pref + "_kernel");
                sql.Append(  DBManager.getServer(con_db) + ".services s," + Points.Pref + "_kernel");
                sql.Append(  DBManager.getServer(con_db) + ".supplier su");
                sql.Append(" where a.nzp_measure=g.nzp_measure and a.nzp_supp not in (1,1000) ");
                sql.Append(" and (case when a.nzp_serv = 515 then 25 else a.nzp_serv end) =s.nzp_serv  and a.nzp_supp=su.nzp_supp ");
                sql.Append(" group by 1,2,3,4,5 order by 1,2,3,4,5 ");
#else
         sql.Append(" select 1 as ord, name_supp, service, measure, s.nzp_serv, ");
                sql.Append(" sum(pl_izol) as pl_izol, ");
                sql.Append(" sum(pl_komm) as pl_komm, ");
                sql.Append(" sum(count_gil) as count_gil, ");
                sql.Append(" sum(count_ls) as count_ls, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(c_calc,0) end) as c_calc,");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(rsum_tarif,0) end) as rsum_tarif, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_nedop,0) end ) as sum_nedop, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_charge,0) end) as sum_charge, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_money,0) end) as sum_money, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else -1*nvl(reval_k,0) end) as reval_k, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(reval_d,0) end) as reval_d, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(c_calc,0) else 0 end) as c_calc_odn,");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(rsum_tarif,0) else 0 end) as rsum_tarif_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_nedop,0) else 0 end) as sum_nedop_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_charge,0) else 0 end) as sum_charge_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then -1*nvl(reval_k,0) else 0 end) as reval_k_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(reval_d,0) else 0 end) as reval_d_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_money,0) else 0 end) as sum_money_odn ");
                sql.Append(" from t_svod a, " + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":s_measure g,   " + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":services s," + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":supplier su");
                sql.Append(" where a.nzp_measure=g.nzp_measure and a.nzp_supp not in (1,1000)");
                sql.Append(" and (case when a.nzp_serv = 515 then 25 else a.nzp_serv end) =s.nzp_serv  and a.nzp_supp=su.nzp_supp ");
                sql.Append(" group by 1,2,3,4,5 ");
                sql.Append(" union all ");
                sql.Append(" select 2 as ord, 'Всего' as name_supp, service, measure, s.nzp_serv, ");
                sql.Append(" sum(pl_izol) as pl_izol, ");
                sql.Append(" sum(pl_komm) as pl_komm, ");
                sql.Append(" sum(count_gil) as count_gil, ");
                sql.Append(" sum(count_ls) as count_ls, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(c_calc,0) end) as c_calc,");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(rsum_tarif,0) end) as rsum_tarif, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_nedop,0) end ) as sum_nedop, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_charge,0) end) as sum_charge, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(sum_money,0) end) as sum_money, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else -1*nvl(reval_k,0) end) as reval_k, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then 0 else nvl(reval_d,0) end) as reval_d, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(c_calc,0) else 0 end) as c_calc_odn,");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(rsum_tarif,0) else 0 end) as rsum_tarif_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_nedop,0) else 0 end) as sum_nedop_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_charge,0) else 0 end) as sum_charge_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then -1*nvl(reval_k,0) else 0 end) as reval_k_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(reval_d,0) else 0 end) as reval_d_odn, ");
                sql.Append(" sum(case when nvl(a.nzp_serv,0) = 515 then nvl(sum_money,0) else 0 end) as sum_money_odn ");
                sql.Append(" from t_svod a, " + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":s_measure g,   " + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":services s," + Points.Pref + "_kernel");
                sql.Append("@" + DBManager.getServer(con_db) + ":supplier su");
                sql.Append(" where a.nzp_measure=g.nzp_measure and a.nzp_supp not in (1,1000) ");
                sql.Append(" and (case when a.nzp_serv = 515 then 25 else a.nzp_serv end) =s.nzp_serv  and a.nzp_supp=su.nzp_supp ");
                sql.Append(" group by 1,2,3,4,5 order by 1,2,3,4,5 ");
#endif

                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    ret.result = false;
                    return null;
                }


                if (reader2 != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader2, LoadOption.OverwriteChanges);
                }


                //удаление nzp_kvar



                return dt;

                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetSpravSuppSamara : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table t_svod; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (reader2 != null)
                {
                    reader2.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

        }
    }
}
