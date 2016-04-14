using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading;
using FastReport;
using System.IO;
using System.Data.OleDb;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Security.AccessControl;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter : DbCounterKernel
    //----------------------------------------------------------------------
    {
        public Returns GetRashodIPU(Counter finder, out string rashod_k_opl)
        {
            Returns ret = Utils.InitReturns();
            rashod_k_opl = "";

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
                      
            if (finder.nzp_user <= 0) new Returns(false, "Пользователь не определен", -1);
            if (finder.nzp_serv <= 0) new Returns(false, "Услуга не определена", -1);
            if (finder.nzp_kvar <= 0) new Returns(false, "Лицевой счет не определен", -1);
            if (finder.pref == "") new Returns(false, "Префикс не определен", -1);
            if (!(finder.year_ > 0 && finder.month_ > 0)) new Returns(false, "Расчетный месяц не определен", -1);
              
            rashod_k_opl = GetRashodKOplate(conn_db, finder);
            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// Получить показания ПУ введенных пользователем
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<CounterValLight> GetCountersUserVals(CounterValLight finder, out Returns ret)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetCountersUserVals");
            List<CounterValLight> retList = new List<CounterValLight>();

            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (!ret.result) throw new Exception(ret.text);

                if (!ret.result) throw new Exception("Ошибка при открытии соединения с БД " + ret.text);
                #endregion

                #region Получение бд
                var tempObj = ExecScalar(con_db, " SELECT dbname from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_baselist where idtype = 8", out ret, true);

                if (!ret.result) throw new Exception("Ошибка получение webfon bd:" + ret.text);
                
                string webFonDbName = tempObj.ToString().Trim();
                #endregion

                sql.Append(" select co.service, co.num_cnt, co.nzp_cnttype, co.prev_dat, co.prev_val, co.cur_dat, co.cur_val, co.cnt_stage, co. formula, k.pref ");
                sql.Append(" from " + webFonDbName + DBManager.tableDelimiter + "counters_ord co, ");
                sql.Append("  " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k ");
                sql.Append(" where co.num_ls = k.num_ls ");
                //sql.Append(" and dat_month = " + Utils.EStrNull(Convert.ToDateTime(Points.CalcMonth.name).ToShortDateString()));
                sql.Append(" and dat_month = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_uchet).ToShortDateString()) );
                sql.Append(" and k.nzp_kvar = " + finder.nzp_kvar + " ");
                sql.Append(" and co.prev_val is not null and co.cur_val is not null ");

                if (!ExecRead(con_db, out reader, sql.ToString(), false).result)
                {
                    MonitorLog.WriteLog("Ошибка получения показаний пользовательских ПУ", MonitorLog.typelog.Error, true);
                    throw new Exception(ret.text);
                }

                while (reader.Read())
                {
                    CounterValLight cv = new CounterValLight();

                    cv.service = reader["service"] != DBNull.Value ? Convert.ToString(reader["service"]) : "";
                    cv.num_cnt = reader["num_cnt"] != DBNull.Value ? Convert.ToString(reader["num_cnt"]) : "";
                    cv.nzp_cnttype = reader["nzp_cnttype"] != DBNull.Value ? Convert.ToInt32(reader["nzp_cnttype"]) : 0;
                    cv.dat_uchet_pred = reader["prev_dat"] != DBNull.Value ? Convert.ToDateTime(reader["prev_dat"]).ToShortDateString() : "-";
                    cv.val_cnt_pred = reader["prev_val"] != DBNull.Value ? Decimal.Round(Convert.ToDecimal(reader["prev_val"]), 4) : -1;
                    cv.dat_uchet = reader["cur_dat"] != DBNull.Value ? Convert.ToDateTime(reader["cur_dat"]).ToShortDateString() : "-";
                    cv.val_cnt = reader["cur_val"] != DBNull.Value ? Decimal.Round(Convert.ToDecimal(reader["cur_val"]), 4) : -1;
                    cv.cnt_stage = reader["cnt_stage"] != DBNull.Value ? Convert.ToInt32(reader["cnt_stage"]) : 0;
                    string localDb = reader["pref"] != DBNull.Value ? Convert.ToString(reader["pref"]).Trim() : "";

                    #region Определение типа прибора учета
                    object tempCnt = null;
                    if (localDb != "")
                    {
                        tempCnt = ExecScalar(con_db, " select name_type from " +
                            localDb + "_kernel" + DBManager.tableDelimiter + "s_counttypes where nzp_cnttype=" + cv.nzp_cnttype + " ", out ret, true);

                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка получение типа прибора учета:" + ret.text, MonitorLog.typelog.Error, true);
                            throw new Exception(ret.text);
                        }
                    }
                    cv.name_type = tempCnt != null ? tempCnt.ToString().Trim() : "-";
                    #endregion

                    string formulaStr = reader["formula"] != DBNull.Value ? Convert.ToString(reader["formula"]) : "";

                    #region Расчет формулы
                    if (cv.val_cnt_pred != -1 && cv.val_cnt != -1)
                    {
                        decimal rashod = cv.val_cnt - cv.val_cnt_pred;
                        if (rashod < 0)
                        {
                            int formula = 0;
                            try
                            {
                                formula = Convert.ToInt32(formulaStr);
                            }
                            catch (Exception)
                            {
                                formula = 1;
                            }

                            rashod = Convert.ToDecimal(Math.Pow(10, cv.cnt_stage)) * Convert.ToDecimal(formula) - cv.val_cnt_pred + cv.val_cnt;
                        }
                        cv.rashod = Decimal.Round(rashod, 4).ToString();
                    }
                    else
                    {
                        cv.rashod = "-";
                    }


                    #endregion

                    //заполнение списка
                    retList.Add(cv);
                }
                if (Constants.Trace) Utility.ClassLog.WriteLog("Финиш функции GetCountersUserVals");
                return retList;

            }
            catch (Exception ex)
            {                
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetCountersUserVals : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

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

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        

        /// <summary>
        /// Определить максимально допустимый расход (Самара)
        /// </summary>
        private Returns DefineMaxRashod(IDbConnection conn_db, CounterValLight finder, List<CounterReading> Spis)
        {
            for (int i = 0; i < Spis.Count; i++)
            {
                if (Spis[i].pref.Trim() == "") Spis[i].pref = finder.pref.Trim();
            }

            List<string> pref_list = Spis.Select(x => x.pref).Distinct().ToList<string>();

            Returns ret = new Returns(true, "", 0);
            IDataReader reader;

            double val_prm = 0;
            Int32 nzp_serv = 0;

            DateTime dd;

            try
            {
                dd = Convert.ToDateTime(finder.dat_uchet);
            }
            catch
            {
                return new Returns(false, "Ошибка конвертации даты учета при определении величины максимальных расходов", -1);
            }

            for (int i = 0; i < pref_list.Count; i++)
            {
                string sql = " select val_prm, 9 as nzp_serv from " + pref_list[i].Trim() + "_data" + DBManager.tableDelimiter + "prm_10 p " +
                    " where p.nzp_prm = 2082 " +
                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po and is_actual <> 100 " +
                    " union all " +
                    " select val_prm, 6 as nzp_serv from " + pref_list[i].Trim() + "_data" + DBManager.tableDelimiter + "prm_10 p " +
                    " where p.nzp_prm = 2083 " +
                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po and is_actual <> 100 " +
                    " union all " +
                    " select val_prm, 25 as nzp_serv from " + pref_list[i].Trim() + "_data" + DBManager.tableDelimiter + "prm_10 p " +
                    " where p.nzp_prm = 2081 " +
                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po and is_actual <> 100 " +
                    " union all " +
                    " select val_prm, 10 as nzp_serv from " + pref_list[i].Trim() + "_data" + DBManager.tableDelimiter + "prm_10 p " +
                    " where p.nzp_prm = 2084 " +
                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po and is_actual <> 100 ";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                    try
                    { val_prm = Convert.ToDouble(reader["val_prm"]); }
                    catch
                    { val_prm = 0; }

                    if (val_prm <= 0) continue;

                    for (int j = 0; j < Spis.Count; j++)
                    {
                        if (Spis[j].pref == pref_list[i] && Spis[j].nzp_serv == nzp_serv)
                        {
                            Spis[j].max_rashod = val_prm;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Получить список приборов учета
        /// </summary>
        public Returns FindPuList(CounterValLight finder)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции FindPuList");
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            Returns ret = new Returns();

            try
            {
                #region проверка значений
                //-----------------------------------------------------------------------
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");
                if (finder.dat_uchet.Trim().Length <= 0) throw new Exception("Не определена дата учета");
                if (finder.nzp_type != (int)CounterKinds.Kvar && finder.nzp_type != (int)CounterKinds.Dom) throw new Exception("Групповой ввод показаний не предусмотрен для указанного типа");
                //-----------------------------------------------------------------------
                #endregion

                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);
                if (finder.pref == null) finder.pref = "";

                #region создать кэш-таблицу
                //-----------------------------------------------------------------------           
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) throw new Exception(ret.text);
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

                string tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_cv";
                if (finder.nzp_type == (int)CounterKinds.Dom) tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

                bool web_table_created = false;
                if (TableInWebCashe(conn_web, tXX_cv))
                {
                    ret = ExecSQL(conn_web, "drop table " + tXX_cv, false);

                    if (ret.result)
                    {
                        ret = CreateTableWebPuValList(conn_web, tXX_cv, true);
                        if (!ret.result) throw new Exception(ret.text);
                        web_table_created = true;
                    }
                    else
                    {
                        // почистить таблицу
                        ExecSQL(conn_web, " delete from " + tXX_cv, true);
                    }
                }
                else
                {
                    ret = CreateTableWebPuValList(conn_web, tXX_cv, true);
                    if (!ret.result) throw new Exception(ret.text);
                    web_table_created = true;
                }
                //-----------------------------------------------------------------------
                #endregion

                // подключение к базе
                conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

#if PG
                // полный путь к таблице в кэше
                string tXX_cv_full = "public." + tXX_cv;
#else
                // полный путь к таблице в кэше
                string tXX_cv_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cv;
#endif

                string sql = "";

                #region сформировать условие
                //----------------------------------------------------------------------------            
                // месяц текущих показаний
                string _where = " and " + DBManager.sNvlWord + "(cs.dat_close, " + DBManager.MDY(1, 1, 3000) + ") >= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString());
                string _where_kvar = "";

                // услуга
                if (finder.nzp_serv > 0) _where += " and cs.nzp_serv = " + finder.nzp_serv.ToString();
                // улица
                if (finder.nzp_ul > 0) _where_kvar += " and d.nzp_ul = " + finder.nzp_ul.ToString();
                // дом
                if (finder.nzp_dom > 0) _where_kvar += " and k.nzp_dom = " + finder.nzp_dom.ToString();
                // квартира
                if (finder.nzp_kvar > 0) _where_kvar += " and k.nzp_kvar = " + finder.nzp_kvar;
                // территория
                if (finder.nzp_area > 0) _where_kvar += " and k.nzp_area = " + finder.nzp_area;
                // роли
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql)
                            switch (role.kod)
                            {
                                case Constants.role_sql_serv:
                                    _where += " and cs.nzp_serv in (" + role.val + ")";
                                    break;
                                case Constants.role_sql_area:
                                    _where_kvar += " and k.nzp_area in (" + role.val + ")";
                                    break;
                                case Constants.role_sql_geu:
                                    _where_kvar += " and k.nzp_geu in (" + role.val + ")";
                                    break;
                            }
                }
                // только открытые приборы учета
                _where += " and cs.dat_close is null";
                // существующие ПУ
                _where += " and cs.is_actual <> 100 ";
                // тип ПУ
                _where += " and cs.nzp_type = " + finder.nzp_type;

                _where += _where_kvar;
                //----------------------------------------------------------------------------
                #endregion

                Finder userFinder = new Finder();
                //DbWorkUser dbWorkUser = new DbWorkUser();

                #region получить список префиксов
                //-------------------------------------------------------------------
                IDataReader reader;
                List<string> prefList = new List<string>();

                if (finder.pref == "")
                {
                    sql = "Select distinct k.pref " +
                    "From " + Points.Pref + "_data" + tableDelimiter + "kvar k, " +
                            Points.Pref + "_data" + tableDelimiter + "dom d " +
                    " where k.nzp_dom = d.nzp_dom " + _where_kvar + "order by 1";

                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    while (reader.Read())
                    {
                        if (reader["pref"] != DBNull.Value) prefList.Add(Convert.ToString(reader["pref"]).Trim());
                    }
                    reader.Close();
                }
                else prefList.Add(finder.pref);
                //-------------------------------------------------------------------
                #endregion

                for (int i = 0; i < prefList.Count; i++)
                {
                    string cur_pref = prefList[i];

                    #region получить код пользователя
                    //----------------------------------------------------------------------------
                    /*userFinder.pref = cur_pref;
                    userFinder.nzp_user = finder.nzp_user;
                    userFinder.nzp_user = dbWorkUser.GetLocalUser(conn_db, userFinder, out ret);*/

                    userFinder.nzp_user = finder.nzp_user;
                    //----------------------------------------------------------------------------
                    #endregion

                    #region сформировать sql для записи в кэш-таблицу
                    //----------------------------------------------------------------------------
                    if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        #region квартирные ПУ
                        //----------------------------------------------------------------------------
                        sql = " Insert into " + tXX_cv_full + " (" +
                            "   pref, nzp_counter, " +
                            "   nzp_dom, nzp_serv, " +
                            "   ulica, ndom, nkor, idom,  " +
                            "   nkvar, ikvar, num_ls, smrlitera, " +
                            "   fio, service," +
                            "   mmnog, cnt_stage," +
                            "   comment, num_cnt, name_type, " +
                            "   dat_prov, dat_provnext) " +
                            " Select distinct " + Utils.EStrNull(cur_pref) + ", cs.nzp_counter, " +
                            "   d.nzp_dom, cs.nzp_serv, " +
                            "   ul.ulica, d.ndom, d.nkor, d.idom,  " +
                            "   trim(" + DBManager.sNvlWord + "(k.nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')), k.ikvar, " +
                            // номер лицевого счета
                            " k.num_ls as num_ls, " +
                            // литера для Самары
                            (Points.IsSmr ? "case when substr(pkod" + sConvToVarChar + ", 11, 1) = '0' then '' else substr(pkod" + sConvToVarChar + ", 11, 1) end" : "''") + " as litera, " +
                            "   k.fio, cc.name, " +
                            "   t.mmnog, t.cnt_stage, " +
                            "   cs.comment, cs.num_cnt, t.name_type, " +
                            "   cs.dat_prov, cs.dat_provnext " +
                            " From " + cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "s_ulica ul, " +
                                cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cc, " +
                                cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                            " Where cs.nzp = k.nzp_kvar " +
                                " and k.nzp_dom = d.nzp_dom " +
                                " and d.nzp_ul = ul.nzp_ul " +
                                " and cs.nzp_cnttype = t.nzp_cnttype " +
                                " and cs.nzp_serv = cc.nzp_serv " + _where;

                        if (finder.nzp_kvar <= 0)
                        { 
                            // только открытые лицевые счета
                            sql += " and exists (select 1 from " + cur_pref + "_data" + DBManager.tableDelimiter + "prm_3 p3 " +
                                   " where p3.nzp = k.nzp_kvar and p3.nzp_prm = 51 and p3.val_prm = '1' and p3.is_actual <> 100 " +
                                   " and " + Utils.EStrNull(curMonth.ToShortDateString()) +
                                   " between p3.dat_s and p3.dat_po )";
                        }
                        //----------------------------------------------------------------------------
                        #endregion
                    }
                    else
                    {
                        #region домовые ПУ
                        //----------------------------------------------------------------------------
                        sql = " Insert into " + tXX_cv_full + " (" +
                            "   pref, nzp_counter, " +
                            "   nzp_dom, nzp_serv, " +
                            "   ulica, ndom, nkor, idom,  " +
                            "   service," +
                            "   mmnog, cnt_stage," +
                            "   num_cnt, name_type) " +
                            " Select distinct '" + cur_pref + "', cs.nzp_counter, " +
                            "   d.nzp_dom, cs.nzp_serv, " +
                            "   ul.ulica, d.ndom, d.nkor, d.idom, " +
                            "   cc.name, " +
                            "   t.mmnog, t.cnt_stage, " +
                            "   cs.num_cnt, t.name_type " +
                            " From " + cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "s_ulica ul, " +
                                cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cc, " +
                                cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                            " Where cs.nzp = d.nzp_dom " +
                                " and k.nzp_dom = d.nzp_dom " +
                                " and d.nzp_ul = ul.nzp_ul " +
                                " and cs.nzp_cnttype = t.nzp_cnttype " +
                                " and cs.nzp_serv = cc.nzp_serv " + _where;
                        //----------------------------------------------------------------------------
                        #endregion
                    }
                    //----------------------------------------------------------------------------
                    #endregion

                    if (Constants.Trace) Utility.ClassLog.WriteLog("FindPuList: Запись в кэш-таблицу: старт");

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Constants.Trace) Utility.ClassLog.WriteLog("FindPuList: Запись в кэш-таблицу: стоп");
                }

                //создать индексы на tXX_cv
                //создать индексы на tXX_cv
                if (web_table_created)
                {
                    ret = CreateTableWebPuValList(conn_web, tXX_cv, false);
                    if (!ret.result) throw new Exception(ret.text);
                }
                else
                {
                    // обновить статистику
                    ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + tXX_cv);
                    if (!ret.result) throw new Exception(ret.text);
                }

                conn_db.Close(); //закрыть соединение с основной базой
                conn_web.Close();

                if (Constants.Trace) Utility.ClassLog.WriteLog("Финиш функции FindLastCntVal");
                return ret;
            }
            catch (Exception ex)
            {
                if (conn_db != null) conn_db.Close();
                if (conn_web != null) conn_db.Close();
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Создать кэш-таблицу для показаний приборов учета
        /// </summary>
        private Returns CreateTableWebPuValList(IDbConnection conn_web, string tXX_cv, bool onCreate)
        {
            Returns ret = new Returns();

            try
            {
                if (onCreate)
                {
                    if (TableInWebCashe(conn_web, tXX_cv)) ExecSQL(conn_web, " Drop table " + tXX_cv, false);

                    //создать таблицу webdata:tXX_cv
#if PG
                    ExecSQL(conn_web, "set search_path to 'public'", false);
                    string serial = "serial";
#else
                    string serial = "serial(1)";
#endif
                    ret = ExecSQL(conn_web, " Create table " + tXX_cv + "(" +
                        " nzp_serial     " + serial + ", " +
                        " pref             char(20), " +
                        " nzp_cv           integer," +
                        " nzp_counter      integer," +
                        " nzp_dom          integer," +
                        " nzp_serv         integer," +
                        " ulica            char(40)," +
                        " ndom             char(15)," +
                        " idom             integer," +
                        " nkor             char(15)," +
                        " nkvar            char(20)," +
                        " ikvar            integer," +
                        " num_ls           integer," +
                        " smrlitera        char(1)," +
                        " fio              char(50)," +
                        " service          char(100)," +
                        " val_cnt          float," +
                        " dat_uchet        date," +
                        " val_cnt_pred     float," +
                        " dat_uchet_pred   date," +
                        " ngp_cnt        " + DBManager.sDecimalType + "(14,7)," +
                        " ngp_lift       " + DBManager.sDecimalType + "(14,7), " +
                        " sred_rashod    float, " +
                        " mmnog          " + DBManager.sDecimalType + "(14,7)," +
                        " cnt_stage      integer," +
                        " comment        char(60)," +
                        " num_cnt        char(40)," +
                        " name_type      char(40)," +
                        " user_          char(100)," +
                        " dat_when       date," +
                        " dat_prov       date, " +
                        " dat_provnext   date, " +
                        " blocked        integer default 0, " +
                        " show           integer default 0," +
                        " prepared       integer default 0)");

                    if (!ret.result) throw new Exception(ret.text);
                }
                else
                {
                    ret = ExecSQL(conn_web, "Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_serial)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, "Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_counter)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, "Create index ix_3" + tXX_cv + " on " + tXX_cv + " (pref)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, "Create index ix_4" + tXX_cv + " on " + tXX_cv + " (show)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, "Create index ix_5" + tXX_cv + " on " + tXX_cv + " (prepared)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, "Create index ix_6" + tXX_cv + " on " + tXX_cv + " (ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, name_type, num_cnt, nzp_counter)");
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + tXX_cv);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Получить список показаний ПУ из кэша
        /// </summary>
        public List<CounterReading> GetPuListVals(CounterValLight finder, out Returns ret)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetLastCntVal");
            IDataReader reader = null;
            IDbConnection conn_web = null;
            IDbConnection conn_db = null;

            string sql = "";
            object count;
            int total = 0;
            string skip = "";
            string first = "";
            string order = " ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, name_type, num_cnt, nzp_counter ";

            try
            {
                ret = new Returns();

                #region проверка параметров

                //------------------------------------------------------------------
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");
                if (finder.nzp_type != (int) CounterKinds.Kvar && finder.nzp_type != (int) CounterKinds.Dom)
                    throw new Exception("Групповой ввод показаний не предусмотрен для указанного типа");
                //------------------------------------------------------------------

                #endregion

                #region подключение к web, проверка, что данные были выбраны

                //------------------------------------------------------------------
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return null;
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

                string tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_cv";
                if (finder.nzp_type == (int) CounterKinds.Dom)
                    tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

                if (!TableInWebCashe(conn_web, tXX_cv))
                {
                    conn_web.Close();
                    ret = new Returns(false, "Данные не были выбраны", -22);
                    return null;
                }
                //------------------------------------------------------------------

                #endregion

#if PG
                string tXX_cv_full = "public." + tXX_cv;
                if (finder.skip > 0) skip = " offset " + finder.skip;
                if (finder.rows > 0) first = " limit " + finder.rows;
                string interval = "now() -  INTERVAL '" + Constants.users_min + " minutes'";
#else
                string tXX_cv_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cv;
                if (finder.skip > 0) skip = " skip " + finder.skip;
                if (finder.rows > 0) first = " first " + finder.rows;
                string interval = "current year to second - " + Constants.users_min + " units minute";
#endif

                #region подключиться к базе

                //--------------------------------------------------------------------            
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                //--------------------------------------------------------------------            

                #endregion

                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);

                if (Constants.Trace)
                    Utility.ClassLog.WriteLog(
                        "GETLASCNTVAL: ************************* НАЧАЛО *****************************");

                #region Шаг 1 подсчитать общее количество записей

                //------------------------------------------------------------------
                count = ExecScalar(conn_web, " Select count(*) From " + tXX_cv + " Where 1=1 ", out ret, true);
                if (!ret.result) throw new Exception(ret.text);
                total = Convert.ToInt32(count);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 1 (подсчитать общее количество записей): " + sql);
                //-------------------------------------------------

                #endregion

                #region Шаг 2 снять пометку о том, что записи просматриваются

                //------------------------------------------------------------------
                sql = " Update " + tXX_cv + " Set show = 0 Where show = 1 ";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog(
                        "GETLASCNTVAL: Шаг 2 (снять пометку о том, что записи просматриваются): " + sql);
                //------------------------------------------------------------------

                #endregion

                #region Шаг 3 в кэше сохранить во временную таблицу коды просматриваемых приборов учета

                //------------------------------------------------------------------
                // удалить временную таблицу, ошибки не обрабатывать
                ExecSQL(conn_web, " Drop table tmp_show_cv_web ", false);
#if PG
                sql = " Select " + order + ", nzp_serial into temp tmp_show_cv_web From " + tXX_cv + " Order by " +
                      order + first + skip;
#else
                sql = " Select " + skip + first + order + ", nzp_serial From " + tXX_cv + " Order by " + order + " into temp tmp_show_cv_web with no log ";
#endif
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (Constants.Trace)
                    Utility.ClassLog.WriteLog(
                        "GETLASCNTVAL: Шаг 3 (в кэше сохранить во временную таблицу коды просматриваемых приборов учета): " +
                        sql);
                //------------------------------------------------------------------

                #endregion

                #region Шаг 4 обновить в кэше информацию о том, какие приборы учета просматриваются

                //------------------------------------------------------------------
                sql = " Update " + tXX_cv + " Set show = 1 Where nzp_serial in (Select nzp_serial From tmp_show_cv_web)";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog(
                        "GETLASCNTVAL: Шаг 4 (сохранить в кэше информацию о том, какие приборы учета просматриваются): " +
                        sql);
                //------------------------------------------------------------------

                #endregion

                #region Шаг 5 получить префиксы просматриваемых ПУ, для которых не подготовлены показания

                //-------------------------------------------------------------------------- 
                sql = " Select distinct t.pref From " + tXX_cv + " t Where t.show = 1 and t.prepared = 0 order by 1";
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog(
                        "GETLASCNTVAL: Шаг 5 (получить префиксы просматриваемых ПУ, для которых неподготовлены показания): " +
                        sql);
                //-------------------------------------------------------------------------- 

                #endregion

                #region получить показания

                //-------------------------------------------------------------------------- 
                string pref = "";

                string counter_val_table = "counters";
                string nzp_cr = "nzp_cr";

                if (finder.nzp_type == (int) CounterKinds.Dom)
                {
                    counter_val_table = "counters_dom";
                    nzp_cr = "nzp_crd";
                }

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();

                    #region определить даты

                    sql = "Update " + tXX_cv_full + " set " +
                          // дата текущего показания - максимальная дата учета в текущем месяце
                          " dat_uchet = (select max(v.dat_uchet) from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v " +
                          "   where v.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                          "       and v.is_actual <> 100 " +
                          "       and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                          "       and v.dat_uchet between " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) +
                          " and " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) + "), " +
                          // дата предыдущего показания - максимальная дата учета из прошлого
                          " dat_uchet_pred = (select max(v.dat_uchet) from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v " +
                          "   where v.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                          "       and v.is_actual <> 100 " +
                          "       and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                          "       and v.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) + ") " +
                          " where show = 1 and prepared = 0 " +
                          "   and pref = " + Utils.EStrNull(pref);
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 6 (определить даты): " + sql);

                    #endregion

                    #region определить показания

                    sql = "Update " + tXX_cv_full + " set " +
                          // текущее показание
                          " val_cnt = (select max(v.val_cnt) from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v " +
                          "   where v.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                          "       and v.is_actual <> 100 " +
                          "       and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                          "       and v.dat_uchet = " + tXX_cv_full + ".dat_uchet), " +
                          // предыдущее показание
                          " val_cnt_pred = (select max(v.val_cnt) from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v " +
                          "   where v.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                          "       and v.is_actual <> 100 " +
                          "       and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                          "       and v.dat_uchet = " + tXX_cv_full + ".dat_uchet_pred) " +
                          " where show = 1 and prepared = 0 " +
                          "   and pref = " + Utils.EStrNull(pref);
                    ;
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Constants.Trace)
                        Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 7 (определить показания): " + sql);

                    #endregion

                    #region определить ключи текущих показаний

                    sql = "Update " + tXX_cv_full + " set " +
                          "   nzp_cv = (select max(v." + nzp_cr + ") from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v " +
                          "   where v.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                          "       and v.is_actual <> 100 " +
                          "       and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                          "       and v.dat_uchet = " + tXX_cv_full + ".dat_uchet " +
                          "       and v.val_cnt = " + tXX_cv_full + ".val_cnt) " +
                          " where show = 1 and prepared = 0 and pref = " + Utils.EStrNull(pref);

                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Constants.Trace)
                        Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 8 (определить ключи показаний): " + sql);

                    DataTable table = ClassDBUtils.OpenSQL("select * from " + tXX_cv_full, conn_db).GetData();

                    #endregion

                    #region получить пользователя, дату изменений, расход на нежилые помещения, расход на электороснабжение лифтов ОДПУ

                    sql = "Update " + tXX_cv_full + " set " +
                          // данные подготовлены
                          "   prepared = 1, " +
                          // пользователь
                          "   user_ = (select u.comment from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v, " + Points.Pref + "_data" + DBManager.tableDelimiter + "users u " +
                          "   where v.nzp_user = u.nzp_user and v." + nzp_cr + " = nzp_cv), " +
                          // дата изменений
                          "   dat_when = (select v.dat_when from " + pref + "_data" + DBManager.tableDelimiter +
                          counter_val_table + " v where v." + nzp_cr + " = nzp_cv) ";

                    // расход на нежилые помещения
                    if ((finder.nzp_type == (int) CounterKinds.Dom) ||
                        (finder.nzp_type == (int) CounterKinds.Kvar && Points.IsIpuHasNgpCnt))
                        sql += ", ngp_cnt = (select v.ngp_cnt from " + pref + "_data" + DBManager.tableDelimiter +
                               counter_val_table + " v where v." + nzp_cr + " = nzp_cv) ";

                    // расход на электороснабжение лифтов ОДПУ, средний расход
                    if (finder.nzp_type == (int) CounterKinds.Dom)
                    {
                        // расход на электороснабжение лифтов ОДПУ
                        sql += ", ngp_lift = (select v.ngp_lift from " + pref + "_data" + DBManager.tableDelimiter +
                               counter_val_table + " v where v." + nzp_cr + " = nzp_cv), " +
                               // средний расход
                               " sred_rashod = (Select cast(p17.val_prm as float) " +
                               " From " + pref + "_data" + DBManager.tableDelimiter + "prm_17 p17 " +
                               " Where p17.nzp = " + tXX_cv_full + ".nzp_counter " +
                               "   and p17.nzp_prm = 979 " +
                               "   and p17.is_actual = 1 " +
                               "   and p17.dat_s >= " + Utils.EStrNull(curMonth.ToShortDateString()) +
                               "   and p17.dat_po <= " +
                               Utils.EStrNull(curMonth.AddMonths(1).AddDays(-1).ToShortDateString()) + ")";
                    }

                    sql += " where show = 1 and prepared = 0 and pref = " + Utils.EStrNull(pref);
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 9 (пользователь): " + sql);

                    #endregion
                }

                reader.Close();

                #region обновить информацию о блокировке приборов учета

                //------------------------------------------------------------------
                Finder userFinder = new Finder();
                //DbWorkUser dbWorkUser = new DbWorkUser();

                #region Шаг 10 получить префиксы просматриваемых ПУ

                //--------------------------------------------------------------------------
                sql = "Select distinct pref From " + tXX_cv + " Where show = 1 Order by 1";
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 10 (получить префиксы просматриваемых ПУ): " + sql);
                //--------------------------------------------------------------------------

                #endregion

                while (reader.Read())
                {
                    userFinder.nzp_user = finder.nzp_user;

                    // получить префикс банка данных и код локального пользователя из этого банка
                    if (reader["pref"] != DBNull.Value) userFinder.pref = Convert.ToString(reader["pref"]).Trim();
                    /*userFinder.nzp_user = finder.nzp_user;
                  userFinder.nzp_user = dbWorkUser.GetLocalUser(conn_db, userFinder, out ret);*/

                    #region Шаг 11 снять блокировку с приборов учета, заблокированных текущим пользователем в текущем банке данных

                    //------------------------------------------------------------------
                    sql = " Update " + userFinder.pref + "_data" + DBManager.tableDelimiter +
                          "counters_spis Set dat_block = null, user_block = null " +
                          " Where " + DBManager.sNvlWord + "(user_block, 0) = " + userFinder.nzp_user;
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Constants.Trace)
                        Utility.ClassLog.WriteLog(
                            "GETLASCNTVAL: Шаг 11 (снять блокировку с приборов учета, заблокированных текущим пользователем в текущем банке данных): " +
                            sql);
                    //------------------------------------------------------------------

                    #endregion

                    // если данные берутся на изменение
                    if (finder.prm == Constants.act_mode_edit.ToString())
                    {
                        #region Шаг 12 обновить в кэше информацию о блокировке ПУ

                        //--------------------------------------------------------------------------
                        sql = " Update " + tXX_cv_full + " Set " +
                              " blocked = (Select count(*) " +
                              "   From " + userFinder.pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                              "   where cs.nzp_counter = " + tXX_cv_full + ".nzp_counter " +
                              "       and cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
                              "       and cs.dat_block is not null and (" + interval + ") < cs.dat_block) " +
                              " Where show = 1 and pref = " + Utils.EStrNull(userFinder.pref);
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) throw new Exception(ret.text);
                        if (Constants.Trace)
                            Utility.ClassLog.WriteLog(
                                "GETLASCNTVAL: Шаг 12 (обновить в кэше информацию о блокировке ПУ): " + sql);
                        //--------------------------------------------------------------------------

                        #endregion

                        #region Шаг 14 заблокировать ПУ, просматриваемые пользователем

                        //--------------------------------------------------------------------------
                        string counter_spis = userFinder.pref + "_data" + DBManager.tableDelimiter + "counters_spis ";

                        sql = " Update " + counter_spis +
                              " Set dat_block = " + DBManager.sCurDateTime + ", user_block = " + userFinder.nzp_user +
                              " Where (Select count(*) From " + tXX_cv_full + " t where t.nzp_counter = " + counter_spis +
                              ".nzp_counter and t.blocked = 0 and t.show = 1 and t.pref = " +
                              Utils.EStrNull(userFinder.pref) + ") > 0 ";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) throw new Exception(ret.text);
                        if (Constants.Trace)
                            Utility.ClassLog.WriteLog(
                                "GETLASCNTVAL: Шаг 14 (заблокировать ПУ, просматриваемые пользователем): " + sql);
                        //--------------------------------------------------------------------------

                        #endregion
                    }
                }
                //------------------------------------------------------------------

                #endregion

                List<CounterReading> Spis = new List<CounterReading>();
#if PG
                sql = " Select * From " + tXX_cv + " Where show = 1 Order by " + order;
#else
                sql = " Select * From " + tXX_cv + " Where show = 1 Order by " + order;
#endif
                int blocked;

                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 15 старт (загрузка данных): " + sql);

                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    CounterReading zap = new CounterReading();
                    zap.num = (i + finder.skip).ToString();
                    zap.nzp_type = finder.nzp_type;

                    if (reader["nzp_cv"] != DBNull.Value) zap.nzp_cv = Convert.ToInt32(reader["nzp_cv"]);

                    if (finder.nzp_type == (int) CounterKinds.Kvar)
                    {
                        if (reader["nkvar"] != DBNull.Value) zap.nkvar = ((string) reader["nkvar"]).Trim();
                        if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
                        if (reader["smrlitera"] != DBNull.Value)
                            zap.pkod = zap.num_ls + " " + Convert.ToString(reader["smrlitera"]).Trim();

                        if (reader["fio"] != DBNull.Value) zap.fio = ((string) reader["fio"]).Trim();
                        if (reader["comment"] != DBNull.Value) zap.comment = ((string) reader["comment"]).Trim();
                        if (reader["dat_prov"] != DBNull.Value)
                            zap.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
                        if (reader["dat_provnext"] != DBNull.Value)
                            zap.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();
                    }
                    else if (finder.nzp_type == (int) CounterKinds.Dom)
                    {
                        if (reader["ngp_lift"] != DBNull.Value) zap.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);
                        if (reader["sred_rashod"] != DBNull.Value)
                            zap.sred_rashod = Convert.ToString(reader["sred_rashod"]).Trim();
                    }

                    // расход на нежилые помещения
                    if ((finder.nzp_type == (int) CounterKinds.Dom) ||
                        (finder.nzp_type == (int) CounterKinds.Kvar && Points.IsIpuHasNgpCnt))
                    {
                        if (reader["ngp_cnt"] != DBNull.Value) zap.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
                    }

                    if (reader["pref"] != DBNull.Value) zap.pref = (string) reader["pref"];
                    if (reader["nzp_counter"] != DBNull.Value) zap.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                    if (reader["ulica"] != DBNull.Value) zap.ulica = ((string) reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) zap.ndom = ((string) reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) zap.nkor = ((string) reader["nkor"]).Trim();
                    if (reader["service"] != DBNull.Value) zap.service = ((string) reader["service"]).Trim();

                    if (reader["val_cnt"] != DBNull.Value)
                    {
                        zap.val_cnt_s = Convert.ToString(reader["val_cnt"]).Trim();
                        zap.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                    }
                    else zap.val_cnt_s = "";

                    if (reader["dat_uchet"] != DBNull.Value)
                        zap.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    else zap.dat_uchet = curMonth.AddMonths(1).ToShortDateString();

                    if (reader["val_cnt_pred"] != DBNull.Value)
                    {
                        zap.val_cnt_pred_s = Convert.ToString(reader["val_cnt_pred"]);
                        zap.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
                    }
                    else zap.val_cnt_pred_s = "";

                    if (reader["dat_uchet_pred"] != DBNull.Value)
                        zap.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred"]).ToShortDateString();
                    if (reader["num_cnt"] != DBNull.Value) zap.num_cnt = ((string) reader["num_cnt"]).Trim();
                    if (reader["name_type"] != DBNull.Value) zap.name_type = ((string) reader["name_type"]).Trim();
                    if (reader["mmnog"] != DBNull.Value) zap.mmnog = Convert.ToDecimal(reader["mmnog"]);
                    if (reader["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);

                    zap.smmnog = zap.cnt_stage.ToString() + " / " + zap.mmnog.ToString();
                    zap.block = "";

                    if (finder.prm == Constants.act_mode_edit.ToString())
                    {
                        blocked = 0;
                        if (reader["blocked"] != DBNull.Value) blocked = Convert.ToInt32(reader["blocked"]);
                        if (blocked > 0) zap.block = "Прибор учета заблокирован";
                    }

                    if (zap.val_cnt_s.Length > 0)
                    {
                        zap.rashod_d = zap.calculatedRashod;
                        zap.rashod = zap.rashod_d.ToString();
                    }

                    if (reader["user_"] != DBNull.Value) zap.dat_when = ((string) reader["user_"]).Trim();
                    if (reader["dat_when"] != DBNull.Value)
                        zap.dat_when += " (" + Convert.ToDateTime(reader["dat_when"]).ToShortDateString() + ")";

                    Spis.Add(zap);

                    if (i >= finder.rows && finder.rows > 0) break;
                }

                reader.Close();

                #region определить что нужно показывать колонку расход на электроснабжение лифтов

                //------------------------------------------------------------------
                if (Spis.Count > 0 && finder.nzp_type == (int) CounterKinds.Dom)
                {
                    Spis[0].show_ngp_lift = ShowNgpLift(tXX_cv, conn_web, out ret);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //------------------------------------------------------------------

                #endregion

                if (finder.nzp_type == (int) CounterKinds.Kvar)
                {
                    ret = DefineMaxRashod(conn_db, finder, Spis);
                    if (!ret.result) throw new Exception(ret.text);
                }

             
                ret.tag = total;
                return Spis;
                //-------------------------------------------------------------------------- 

                #endregion
            }
            catch (Exception ex)
            {
                if (conn_web != null) conn_web.Close();
                if (conn_db != null) conn_db.Close();
                if (reader != null) reader.Close();
                ret = new Returns(false, ex.Message);
                return null;
            }
            finally
            {
                if (conn_web != null) conn_web.Close();
                if (conn_db != null) conn_db.Close();
            }
        }

        /// <summary>
        /// Определить нужно ли показывать колонку расход на электроснабжение лифтов
        /// </summary>
        private bool ShowNgpLift(string tXX_cv, IDbConnection conn_web, out Returns ret)
        {
            string sql = "Select " + DBManager.sNvlWord + "(count(*), 0) From " + tXX_cv + " Where nzp_serv in (208, 242, 210, 25)";
            object count = ExecScalar(conn_web, sql, out ret, true);
            if (ret.result) return Convert.ToInt32(count) > 0;
            else return false;
        }

        /// <summary>
        /// Копировать текущие показания
        /// </summary>
        private void CopyCounterVal(CounterValLight _new, CounterValLight _old)
        {
            _new.nzp_key = _old.nzp_key;
            _new.dat_uchet = _old.dat_uchet;
            _new.nzp_user = _old.nzp_user;
            _new.webLogin = _old.webLogin;
            _new.webUname = _old.webUname;
            _new.ist = _old.ist;

            _new.year_ = _old.year_;
            _new.month_ = _old.month_;
            _new.pref = _old.pref;
            _new.cnt_stage = _old.cnt_stage;
            _new.val_cnt_s = _old.val_cnt_s;
            _new.nzp_cv = _old.nzp_cv;
            _new.nzp_counter = _old.nzp_counter;

            _new.nzp_type = _old.nzp_type;
            _new.isSaveRashodInCurPeriod = _old.isSaveRashodInCurPeriod;
            _new.ngp_cnt = _old.ngp_cnt;
            _new.ngp_lift = _old.ngp_lift;
            _new.sred_rashod = _old.sred_rashod;
            
            if (_new.sred_rashod == null) _new.sred_rashod = "";
        }

        /// <summary>
        /// Сохранить показания ПУ c формы counterreadings
        /// </summary>
        public Returns SaveCounterListVals(List<CounterValLight> newVals)
        {
            Returns ret = Utils.InitReturns();

            #region проверка параметров
            //------------------------------------------------------------
            if (newVals[0].nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            //------------------------------------------------------------
            #endregion

            #region установка подключений
            //------------------------------------------------------------
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            //------------------------------------------------------------
            #endregion

            #region копирование списка
            //------------------------------------------------------------
            List<CounterValLight> tmpCounterVal = new List<CounterValLight>();
            for (int i = 0; i < newVals.Count; i++)
            {
                CounterValLight tmp_cv = new CounterValLight();
                CopyCounterVal(tmp_cv, newVals[i]);
                tmpCounterVal.Add(tmp_cv);
            }
            //------------------------------------------------------------
            #endregion

            #region сохранение показаний
            //------------------------------------------------------------
            List<CounterValLight> prefCounterVal = new List<CounterValLight>();
            CounterValLight cur_cv = new CounterValLight();

            DbParameters db = new DbParameters();
            Param prm = new Param();
            DateTime monthBegin = new DateTime(newVals[0].year_, newVals[0].month_, 1);
            DateTime monthEnd = monthBegin.AddMonths(1).AddDays(-1);

            while (true)
            {
                if (newVals.Count <= 0) break;

                // формирование списка
                //----------------------------------------------------------------
                cur_cv.pref = newVals[0].pref;
                prefCounterVal.Clear();

                int i = 0;
                while (true)
                {
                    if (newVals[i].pref == cur_cv.pref)
                    {
                        // если сохраняется только средний расход, то пропустить это показание
                        if (!(newVals[i].nzp_key <= 0 && newVals[i].val_cnt_s == "" && newVals[i].sred_rashod != ""))
                        {
                            CounterValLight tmp_cv = new CounterValLight();
                            CopyCounterVal(tmp_cv, newVals[i]);
                            prefCounterVal.Add(tmp_cv);
                        }
                        newVals.RemoveAt(i);
                    }
                    else i++;

                    if (!(i < newVals.Count)) break;
                }
                //----------------------------------------------------------------
                if (prefCounterVal.Count > 0)
                {
                    ret = SaveCountersValsLight(prefCounterVal);
                    if (!ret.result) break;
                }
            }

            if (!ret.result) return ret;
            //------------------------------------------------------------
            #endregion

            #region сохранить средний расход для общедомовых приборов учета
            //------------------------------------------------------------
            for (int j = 0; j < tmpCounterVal.Count; j++)
            {
                if (tmpCounterVal[j].nzp_type == (int)CounterKinds.Dom)
                {
                    prm.dat_s = monthBegin.ToShortDateString();
                    prm.dat_po = monthEnd.ToShortDateString();
                    prm.nzp_user = tmpCounterVal[j].nzp_user;
                    prm.webLogin = tmpCounterVal[j].webLogin;
                    prm.webUname = tmpCounterVal[j].webUname;
                    prm.pref = tmpCounterVal[j].pref;
                    prm.nzp = tmpCounterVal[j].nzp_counter;
                    prm.nzp_prm = 979;
                    prm.val_prm = tmpCounterVal[j].sred_rashod.ToString();
                    prm.prm_num = 17;

                    if (tmpCounterVal[j].sred_rashod.Length > 0)
                    {
                        ret = db.SavePrm(conn_db, null, prm);
                        if (!ret.result) break;
                    }
                    else
                    {
                        prm.prms = Constants.act_del_val.ToString();
                        ret = db.SavePrm(conn_db, null, prm);
                        if (!ret.result) break;
                    }
                }
            }

            if (!ret.result) return ret;
            //------------------------------------------------------------
            #endregion

            return ret;
        }

        /// <summary>
        /// Получить расход для ОДПУ
        /// </summary>
        public List<CounterReading> GetOdpuRashod(CounterValLight finder, out Returns ret)
        {
            IDbConnection conn_web = null;
            IDbConnection conn_db = null;
            List<string> prefList = null;
            List<_Service> serviceList = null;
            List<CounterReading> valList = null;
            IDataReader reader = null;

            ret = new Returns();

            try
            {
                #region проверка значений
                //------------------------------------------------------------------
                ret = Utils.InitReturns();
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");
                if (finder.month_ <= 0) throw new Exception("Не определен месяц");
                if (finder.year_ <= 0) throw new Exception("Не определен год");
                if (finder.dat_uchet.Trim().Length <= 0) throw new Exception("Не определена дата учета");
                //------------------------------------------------------------------
                #endregion

                #region подключение к web + проверка, что данные были выбраны
                //------------------------------------------------------------------
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return null;

                string tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

                if (!TableInWebCashe(conn_web, tXX_cv))
                {
                    conn_web.Close();
                    ret = new Returns(false, "Данные не были выбраны", -22);
                    return null;
                }
                //------------------------------------------------------------------
                #endregion

                #region подключиться к банку
                //--------------------------------------------------------------------            
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);

                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
                //--------------------------------------------------------------------            
                #endregion

                string sql = "";

                _Service service;
                serviceList = new List<_Service>();

                string pref = "";
                prefList = new List<string>();

                valList = new List<CounterReading>();
                CounterReading val;

                #region получить из кэша отсортированный список услуг
                //---------------------------------------------------------------------------------------------------

                sql = "Select distinct nzp_serv, service From " + tXX_cv + " Order by 2";
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    service = new _Service();
                    if (reader["nzp_serv"] != DBNull.Value) service.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) service.service = Convert.ToString(reader["service"]).Trim();
                    serviceList.Add(service);
                }
                reader.Close();
                //---------------------------------------------------------------------------------------------------
                #endregion

                #region получить из кэша список префиксов
                //---------------------------------------------------------------------------------------------------
                sql = "Select distinct pref From " + tXX_cv + " Order by 1";

                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
                    prefList.Add(pref);
                }
                reader.Close();
                //---------------------------------------------------------------------------------------------------
                #endregion

                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);

                #region сформировать условие
                //---------------------------------------------------------------------------- 
                string _where = " and " + DBManager.sNvlWord + "(cs.dat_close, " + DBManager.MDY(1, 1, 3000) + ") >= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString());

                // улица
                if (finder.nzp_ul > 0) _where += " and d.nzp_ul = " + finder.nzp_ul.ToString();
                // дом
                if (finder.nzp_dom > 0) _where += " and k.nzp_dom = " + finder.nzp_dom.ToString();
                // квартира
                if (finder.nzp_kvar > 0) _where += " and k.nzp_kvar = " + finder.nzp_kvar;
                // территория
                if (finder.nzp_area > 0) _where += " and k.nzp_area = " + finder.nzp_area;
                // роли
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql)
                            switch (role.kod)
                            {
                                case Constants.role_sql_area:
                                    _where += " and k.nzp_area in (" + role.val + ")";
                                    break;
                                case Constants.role_sql_geu:
                                    _where += " and k.nzp_geu in (" + role.val + ")";
                                    break;
                            }
                }
                // только открытые приборы учета
                _where += " and cs.dat_close is null";
                // существующие ПУ
                _where += " and cs.is_actual <> 100 ";
                //----------------------------------------------------------------------------
                #endregion

                #region создать временную таблицу
                //---------------------------------------------------------------------------------------------------
                // удалить временную таблицу, ошибки не обрабатывать
                ExecSQL(conn_db, " Drop table tmp_rashod_dom ", false);

                sql = "Create temp table tmp_rashod_dom" +
                    "( val_cnt        float, " +
                    "  ngp_cnt        " + DBManager.sDecimalType + "(14,7), " +
                    "  ngp_lift       " + DBManager.sDecimalType + "(14,7), " +
                    "  val_cnt_pred   float, " +
                    "  sred_rashod    float, " +
                    "  cnt_stage      integer, " +
                    "  mmnog          " + DBManager.sDecimalType + "(14,7), " +
                    "  dat_uchet      date, " +
                    "  dat_uchet_pred date) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                //---------------------------------------------------------------------------------------------------
                #endregion

                decimal sred_rashod;

                foreach (_Service serv in serviceList)
                {
                    sql = "delete From tmp_rashod_dom";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    sred_rashod = 0;

                    foreach (string cur_pref in prefList)
                    {
                        #region создать таблицу
                        //--------------------------------------------------------------------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table tmp_insert_cv ", false);

                        sql = "Create temp table tmp_insert_cv " +
                            "( nzp_counter    integer, " +
                            "  val_cnt        float, " +
                            "  val_cnt_pred   float, " +
                            "  ngp_cnt        " + DBManager.sDecimalType + "(14,7), " +
                            "  ngp_lift       " + DBManager.sDecimalType + "(14,7), " +
                            "  sred_rashod    float, " +
                            "  cnt_stage      integer, " +
                            "  mmnog          " + DBManager.sDecimalType + "(14,7), " +
                            "  dat_uchet      date, " +
                            "  dat_uchet_pred date) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //--------------------------------------------------------------------------------------------------------------------------
                        #endregion

                        #region получить текущие значения
                        //--------------------------------------------------------------------------------------------------------------------------
                        sql = " Insert into tmp_insert_cv (nzp_counter, ngp_cnt, ngp_lift, cnt_stage, mmnog, val_cnt, dat_uchet) " +
                            " Select cs.nzp_counter, v.ngp_cnt, v.ngp_lift, t.cnt_stage, t.mmnog, " +
                            "   v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                            " From " + cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " +
                                cur_pref + "_data" + DBManager.tableDelimiter + "counters_dom v " +
                            " Where cs.nzp_cnttype = t.nzp_cnttype " +
                                " and d.nzp_dom = cs.nzp " +
                                " and d.nzp_dom = k.nzp_dom " +
                                " and cs.nzp_counter = v.nzp_counter " +
                                " and cs.nzp_type = 1 " +
                                " and cs.nzp_serv = " + serv.nzp_serv +
                                " and v.dat_uchet >= " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) + " and v.dat_uchet <= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
                                " and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " +
                                " and v.is_actual <> 100 " +
                                _where +
                                " group by 1,2,3,4,5,6 ";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) throw new Exception(ret.text);
                        //--------------------------------------------------------------------------------------------------------------------------
                        #endregion

                        #region определить дату предыдущего показания
                        //--------------------------------------------------------------------------------------------------------------------------
                        sql = " Update tmp_insert_cv set " +
                            " dat_uchet_pred = (select max(a.dat_uchet) from " + cur_pref + "_data" + DBManager.tableDelimiter + " counters_dom a " +
                            " where a.nzp_counter = tmp_insert_cv.nzp_counter " +
                            "   and a.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) +
                            "   and a.is_actual <> 100 " +
                            "   and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1) ";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) throw new Exception(ret.text);
                        //--------------------------------------------------------------------------------------------------------------------------
                        #endregion

                        #region получить предыдущее показание
                        //--------------------------------------------------------------------------------------------------------------------------
                        sql = " Update tmp_insert_cv set " +
                            " val_cnt_pred = (select max(a.val_cnt) from " + cur_pref + "_data" + DBManager.tableDelimiter + " counters_dom a " +
                            " where a.nzp_counter = tmp_insert_cv.nzp_counter " +
                            "   and a.dat_uchet = tmp_insert_cv.dat_uchet_pred " +
                            "   and a.is_actual <> 100 " +
                            "   and " + DBManager.sNvlWord + "(a.val_cnt, -1) > -1) ";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) throw new Exception(ret.text);
                        //--------------------------------------------------------------------------------------------------------------------------
                        #endregion

                        #region вставить данные из одной временной таблицы в другую временную таблицу
                        //---------------------------------------------------------------------------------------------------
                        sql = "Insert into tmp_rashod_dom (val_cnt, ngp_cnt, ngp_lift, val_cnt_pred, cnt_stage, mmnog, dat_uchet, dat_uchet_pred) " +
                            " Select val_cnt, ngp_cnt, ngp_lift, val_cnt_pred, cnt_stage, mmnog, dat_uchet, dat_uchet_pred From tmp_insert_cv";

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------
                        #endregion

                        #region получить средний расход
                        //---------------------------------------------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table tmp_sred_rashod ", false);

                        sql = " create temp table tmp_sred_rashod (nzp_counter integer, sred_rashod float) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        
                        sql = " insert into tmp_sred_rashod (nzp_counter, sred_rashod) " + 
                            " Select distinct cs.nzp_counter, CAST(p17.val_prm as float) " +
                            "  From " + cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "prm_17 p17 " +
                            " Where d.nzp_dom = cs.nzp " +
                                " and d.nzp_dom = k.nzp_dom " +
                                " and p17.nzp = cs.nzp_counter " +
                                " and cs.nzp_type = 1 " +
                                " and cs.nzp_serv = " + serv.nzp_serv +
                                " and p17.nzp_prm = 979 " +
                                " and p17.dat_s >= " + Utils.EStrNull(curMonth.ToShortDateString()) +
                                " and p17.dat_po <= " + Utils.EStrNull(curMonth.AddMonths(1).AddDays(-1).ToShortDateString()) +
                                " and p17.is_actual = 1 " + _where;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);

                        sql = " Select sum(sred_rashod) From tmp_sred_rashod ";
                        object sums = ExecScalar(conn_db, sql, out ret, true);
                        if (!ret.result) throw new Exception(ret.text);

                        try
                        {
                            if (sums != DBNull.Value) sred_rashod += Convert.ToDecimal(sums);
                            else sred_rashod += 0;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        //---------------------------------------------------------------------------------------------------
                        #endregion
                    }

                    #region подсчитать итоги
                    //---------------------------------------------------------------------------------------------------
                    sql = " select sum(case when cv.val_cnt >= cv.val_cnt_pred then (cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift " +
                           "     else (pow(10, cv.cnt_stage) + cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift end) as rashod, " +
                           "    sum(cv.ngp_cnt) as ngp_cnt, sum(cv.ngp_lift) as ngp_lift " +
                           " from tmp_rashod_dom cv";

                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (reader.Read())
                    {
                        val = new CounterReading();
                        val.service = serv.service;

                        if (reader["rashod"] != DBNull.Value) val.rashod = Convert.ToString(reader["rashod"]);
                        else val.rashod = "0";

                        if (reader["ngp_cnt"] != DBNull.Value) val.ngp_cnt_s = Convert.ToString(reader["ngp_cnt"]).Trim();
                        else val.ngp_cnt_s = "0";

                        if (reader["ngp_lift"] != DBNull.Value) val.ngp_lift_s = Convert.ToString(reader["ngp_lift"]).Trim();
                        else val.ngp_lift_s = "0";

                        // средний расход
                        val.sred_rashod = sred_rashod.ToString();

                        val.num = "итого";
                        val.nzp_type = (int)CounterKinds.Dom;

                        valList.Add(val);
                    }
                    //---------------------------------------------------------------------------------------------------
                    #endregion
                }

                #region определить что нужно показывать колонку расход на электроснабжение лифтов
                //------------------------------------------------------------------
                if (valList.Count > 0 && finder.nzp_type == (int)CounterKinds.Dom)
                {
                    valList[0].ndom = "ИТОГО";
                    if (!ret.result) throw new Exception(ret.text);
                }
                //------------------------------------------------------------------
                #endregion

                conn_db.Close();
                conn_web.Close();
                reader.Close();
                prefList.Clear();
                serviceList.Clear();

                return valList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                if (conn_db != null) conn_db.Close();
                if (conn_web != null) conn_web.Close();
                prefList.Clear();
                serviceList.Clear();
                return null;
            }
        }
        
        /// <summary>
        /// Поиск ЛС для быстрого ввода показаний ПУ
        /// </summary>
        public Returns LoadForFastPu(Ls finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
            if ((finder.num_ls_s.Trim().Length <= 0) && (finder.pkod.Trim().Length <= 0)) return new Returns(false, "Не указан номер ЛС", -1);

            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            //IDataReader reader = null;

            string sql = "";
            string tXX = "t" + Convert.ToString(finder.nzp_user) + "_fstpu";

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                #region создать кэш-таблицу
                //-----------------------------------------------------------------------           
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) throw new Exception(ret.text);

                ret = CreateTableWebFastPuLsList(conn_web, tXX, true);
                if (!ret.result) throw new Exception(ret.text);
                //--------------------------------------------------------------------------------------------------
                #endregion

                #region сформировать условие поиска лицевых счетов
                //--------------------------------------------------------------------------------------------------
                string like = "";
                if (finder.num_ls_s.Trim().Length > 0)
                {
                    like = //(Points.IsSmr ? " and  substr(cast(kv.pkod as varchar(13)), 6, 5) like '" + finder.num_ls_s + "%'" :
                                           " and  cast(kv.num_ls as varchar(10)) like '" + finder.num_ls_s + "%' ";
                }

                if (finder.pkod.Trim().Length > 0) like += " and cast(kv.pkod as varchar(20)) like '" + finder.pkod + "%' ";
                if (finder.remark == "quickaddpackls")
                {
                    if (finder.num_ls_s.Trim().Length > 0)
                    {
                        like = " AND kv.num_ls = " + finder.num_ls_s;
                    }
                    if (finder.pkod.Trim().Length > 0) like += " and kv.pkod =  '" + finder.pkod + "' ";
                }

                //--------------------------------------------------------------------------------------------------
                #endregion

             //   #region получить список префиксов
                //--------------------------------------------------------------------------------------------------                
               /* sql = " select distinct pref from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar kv where 1=1 " + like + " order by 1";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                List<string> prefList = new List<string>();
                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) prefList.Add(Convert.ToString(reader["pref"]).Trim());
                }
                reader.Close();
                //--------------------------------------------------------------------------------------------------
                #endregion
                */
#if PG
                string tXX_full = "public." + tXX;
#else
                string tXX_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX;
#endif

                foreach (_Point p in Points.PointList)
                {
                    sql = " insert into " + tXX_full + " (num_ls, nzp_kvar, fio, pref, pu, adr) " +
                        " SELECT DISTINCT " + "kv.num_ls" + ", " +
                        "   kv.nzp_kvar, kv.fio, " + Utils.EStrNull(p.pref) + ", " +
                        // признак наличия ПУ
                        "   (CASE WHEN (select count(*) from " + p.pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                        "               where cs.nzp = kv.nzp_kvar " + 
                        "                   and cs.nzp_type = 3 " + 
                        "                   and cs.is_actual <> 100 " + 
                        "                   and cs.dat_close is null) > 0 THEN 'Да' ELSE 'Нет' END) AS pu, " +
                        // адрес
                        "   TRIM(" + DBManager.sNvlWord + "(u.ulica, '')) || ' / ' || TRIM(" + DBManager.sNvlWord + "(r.rajon, '')) || " +
                        "   ' дом ' || TRIM(" + DBManager.sNvlWord + "(ndom, ''))  || ' корп. ' || TRIM(" + DBManager.sNvlWord + "(nkor, '')) || " +
                        "   ' кв. ' || TRIM(" + DBManager.sNvlWord + "(nkvar, '')) || ' ком. '  || TRIM(" + DBManager.sNvlWord + "(nkvar_n, '')) AS adr " +
                        " FROM " +
                            p.pref + "_data" + DBManager.tableDelimiter + "kvar kv, " +
                            p.pref + "_data" + DBManager.tableDelimiter + "dom d,	" +
                            p.pref + "_data" + DBManager.tableDelimiter + "s_ulica u " +
                        "   left outer join " + p.pref + "_data" + DBManager.tableDelimiter + "s_rajon r on r.nzp_raj = u.nzp_raj " +
                        " WHERE d.nzp_ul = u.nzp_ul " +
                        "   AND kv.nzp_dom = d.nzp_dom " + like;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Функция LoadForFastPu " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                if (conn_db != null) conn_db.Close();
                if (conn_web != null) conn_web.Close();
                return new Returns(false, ex.Message, -1);
            }
        }

        /// <summary>
        /// Создать кэш-таблицу под список ЛС для быстрого ввода показаний ПУ 
        /// </summary>
        private Returns CreateTableWebFastPuLsList(IDbConnection conn_web, string tXX, bool onCreate) //
        {
            Returns ret = new Returns();


#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
       

            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX)) ExecSQL(conn_web, " Drop table " + tXX, false);

                //создать таблицу webdata:tXX_cv
                ret = ExecSQL(conn_web,
                    " Create table " + tXX + "(" +
                    " adr      char(80), " +
                    " fio      char(50), " +
                    " num_ls   integer, " +
                    " pu       char(3), " +
                    " pref     char(10), " +
                    " nzp_kvar integer)", true);
                if (!ret.result) return ret;
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX + " on " + tXX + " (nzp_kvar) ", true);
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Получить список ЛС для быстрого ввода показаний ПУ
        /// </summary>
        public List<Ls> GetForFastPu(Ls finder, out Returns ret)
        {
            IDataReader reader = null;
            IDbConnection conn_web = null;
            ret = new Returns();
                
            string sql = "";
            object count;
            int total = 0;
            string skip = "";
            string first = "";
            string order = "";
            string tXX = "t" + Convert.ToString(finder.nzp_user) + "_fstpu";

            List<Ls> spis = new List<Ls>();

            try
            {
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");
                
                #region подключение к web + проверка, что данные были выбраны
                //------------------------------------------------------------------
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return null;

                if (!TableInWebCashe(conn_web, tXX))
                {
                    conn_web.Close();
                    ret = new Returns(false, "Данные не были выбраны", -22);
                    return null;
                }
                //------------------------------------------------------------------
                #endregion
#if PG
                if (finder.skip > 0) skip = " offset " + finder.skip;
                if (finder.rows > 0) first = " limit " + finder.rows;
#else
                if (finder.skip > 0) skip = " skip " + finder.skip;
                if (finder.rows > 0) first = " first " + finder.rows;
#endif
                #region подсчитать общее количество записей
                //------------------------------------------------------------------
                count = ExecScalar(conn_web, " Select count(*) From " + tXX, out ret, true);
                if (!ret.result) throw new Exception(ret.text);
                total = Convert.ToInt32(count);
                //-------------------------------------------------
                #endregion

                order = "num_ls";
                if (finder.sortby == 601) order = "adr";
#if PG
                sql = " Select * From " + tXX + " Order by " + order + first + skip;
#else
                sql = " Select " + skip + first + " * From " + tXX + " Order by " + order;
#endif
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Ls ls = new Ls();
                    ls.adr = Convert.ToString(reader["adr"]).Trim();
                    ls.fio = Convert.ToString(reader["fio"]).Trim();
                    ls.num_ls = Convert.ToInt32(reader["num_ls"]);
                    ls.has_pu = Convert.ToString(reader["pu"]).Trim();
                    ls.pref = Convert.ToString(reader["pref"]).Trim();
                    ls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    spis.Add(ls);

                    if (i >= finder.rows) break;
                }

                ret.tag = total;
                return spis;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetForFastPu : " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                if (reader != null) reader.Close();

                conn_web.Close();
                ret = new Returns(false, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Получить показания ПУ для одного ЛС
        /// </summary>
        public List<CounterReading> GetLsPuData(Ls finder, out Returns ret)
        {
            ret = new Returns();
            List<CounterReading> CountersList = new List<CounterReading>();

            try
            {
                if (finder.nzp_kvar <= 0) throw new Exception("Не указан ЛС");

                DbCounter db = new DbCounter();

                // получить расчетный месяц банка
                CalcMonthParams cmp = new CalcMonthParams();
                cmp.pref = finder.pref;
                RecordMonth rm = Points.GetCalcMonth(cmp);

                DateTime dat_uchet = new DateTime(rm.year_, rm.month_, 1);

                CounterValLight newFinder = new CounterValLight();
                newFinder.nzp_user = finder.nzp_user;
                newFinder.dat_uchet = dat_uchet.ToShortDateString();
                newFinder.nzp_type = (int)CounterKinds.Kvar;
                newFinder.nzp_kvar = finder.nzp_kvar;
                newFinder.pref = finder.pref;
                newFinder.prm = Constants.act_mode_edit.ToString();

                // найти приборы учета
                ret = db.FindPuList(newFinder);
                if (!ret.result) throw new Exception(ret.text);

                newFinder.rows = 0;
                newFinder.skip = 0;

                // получить показания ПУ
                CountersList = db.GetPuListVals(newFinder, out ret);

                return CountersList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
        }
    
    }
}
