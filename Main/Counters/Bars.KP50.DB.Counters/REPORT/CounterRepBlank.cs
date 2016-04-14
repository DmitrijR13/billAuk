using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FastReport;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter
    //----------------------------------------------------------------------
    {
        public Returns PrintCounterValBlank(CounterVal finder)
        {
            IDbConnection conn_db = null;
            string sql = "";

            try
            {
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                conn_db = GetConnection(conn_kernel);
                
                Returns ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception("Не удалось установить подключение");
                
                #region проверка значений
                //-----------------------------------------------------------------------
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");

                if (finder.month_ <= 0) throw new Exception("Не определен месяц");

                if (finder.year_ <= 0) throw new Exception("Не определен год");

                // проверка даты учета
                if (finder.dat_uchet.Trim().Length <= 0) throw new Exception("Не определена дата учета");
                //-----------------------------------------------------------------------
                #endregion

                // дата учета показаний приходит с формы
                // расчетные месяцы банков не трогать
                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);
                
                #region сформировать условие
                //----------------------------------------------------------------------------            
                // месяц текущих показаний
                string _where = " and " + DBManager.sNvlWord + "(cs.dat_close, " + DBManager.MDY(1,1,3000) + ") >= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString());

                // услуга
                if (finder.nzp_serv > 0) _where += " and cs.nzp_serv = " + finder.nzp_serv.ToString();
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
                                case Constants.role_sql_serv:
                                    _where += " and cs.nzp_serv in (" + role.val + ")";
                                    break;
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
                // открытые ПУ
                _where += " and cs.is_actual <> 100 ";
                // тип ПУ
                _where += " and cs.nzp_type = " + finder.nzp_type;
                //----------------------------------------------------------------------------
                #endregion

                string temp_table = "tmp_pu_list";
                string temp_main = "tmp_pu_main_list";

                #region создать таблицу под список ПУ
                //---------------------------------------------------------------------------
                ExecSQL(conn_db, "drop table " + temp_main, false);

                sql = " create temp table " + temp_main +
                    " (" +
                    " ulica          char(40)," +
                    " nzp_dom        integer, " +
                    " ndom           char(15)," +
                    " idom           integer," +
                    " nkor           char(15)," +
                    " nkvar          char(20)," +
                    " ikvar          integer," +
                    " num_ls         integer," +
                    " nzp_counter    integer, " +
                    " service        char(100)," +
                    " num_cnt        char(20)," +
                    " name_type      char(40)," +
                    " val_cnt_pred   float," +
                    " dat_uchet_pred date " +
                    ")" + DBManager.sUnlogTempTable;

                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result) throw new Exception(ret.text);
                //---------------------------------------------------------------------------
                #endregion

                for (int i = 0; i < Points.PointList.Count; i++)
                {
                    string cur_pref = Points.PointList[i].pref;

                    #region создать временную таблицу
                    //---------------------------------------------------------------------------
                    ExecSQL(conn_db, "drop table " + temp_table, false);
                    sql = " create temp table " + temp_table +
                        " (" +
                        " ulica         char(40)," +
                        " nzp_dom        integer, " +
                        " ndom           char(15)," +
                        " idom           integer," +
                        " nkor           char(15)," +
                        " nkvar          char(20)," +
                        " ikvar          integer," +
                        " num_ls         integer," +
                        " nzp_counter    integer, " +
                        " service        char(100)," +
                        " num_cnt        char(20)," +
                        " name_type      char(40)," +
                        " val_cnt_pred   float," +
                        " dat_uchet_pred date " +
                        ")" + DBManager.sUnlogTempTable;

                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    #region получить счетчики
                    //---------------------------------------------------------------------------
                    string where_ist = "";

                    if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        sql = "Insert into " + temp_table + " (ulica, nzp_dom, ndom, idom, nkor, nkvar, ikvar, num_ls, nzp_counter, service, num_cnt, name_type) " +
                            " Select distinct ul.ulica, d.nzp_dom, d.ndom, d.idom, d.nkor, " +
                            "   trim(" + DBManager.sNvlWord + "(k.nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')), k.ikvar, " +
                            (Points.IsSmr ? "substr(pkod, 6, 5) as num_ls " : "k.num_ls") + ", cs.nzp_counter, cc.name, cs.num_cnt, t.name_type " +
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
                                " and cs.nzp_serv = cc.nzp_serv " +
                                " and (select count(*) from " + cur_pref + "_data" + DBManager.tableDelimiter + "prm_3 p3 where p3.nzp_prm = 51 " +
                                " and p3.val_prm = '1' and p3.dat_s <= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
                                " and p3.dat_po >= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) + " and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " + _where;
                        where_ist = ""; // " and v.ist = " + (int)CounterVal.Ist.Operator;    
                    }
                    else
                    {
                        sql = "Insert into " + temp_table + " (ulica, nzp_dom, ndom, idom, nkor, nzp_counter, service, num_cnt, name_type) " +
                            " Select distinct ul.ulica, d.nzp_dom, d.ndom, d.idom, d.nkor, cs.nzp_counter, cc.name, cs.num_cnt, t.name_type " +
                            " From " + cur_pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "s_ulica ul, " +
                                        cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counts cc, " +
                                        cur_pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                                        cur_pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                            " Where cs.nzp = d.nzp_dom  " +
                                " and k.nzp_dom = d.nzp_dom " +
                                " and d.nzp_ul = ul.nzp_ul " +
                                " and cs.nzp_cnttype = t.nzp_cnttype " +
                                " and cs.nzp_serv = cc.nzp_serv " + _where;
                    }

                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = "create index ix_" + temp_table + "_1 on " + temp_table + " (nzp_counter)";
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = DBManager.sUpdStat + " " + temp_table;
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    #region получить значения
                    //---------------------------------------------------------------------------
                    // определить даты
                    string counter_val_table = "counters";
                    if (finder.nzp_type != (int)CounterKinds.Kvar) counter_val_table = "counters_dom";

                    sql = " update " + temp_table + " set dat_uchet_pred = (select max(v.dat_uchet) " +
                        " from " + cur_pref + "_data" + DBManager.tableDelimiter + counter_val_table + " v " +
                        " where v.nzp_counter = " + temp_table + ".nzp_counter " +
                            " and v.is_actual <> 100 " +
                            " and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + where_ist +
                            " and v.dat_uchet <= " + DBManager.MDY(curMonth.Month, curMonth.Day, curMonth.Year) + ")";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #region создать индекс для даты учета
                    //---------------------------------------------------------------------------
                    sql = "create index ix_" + temp_table + "_2 on " + temp_table + " (dat_uchet_pred)";
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    sql = DBManager.sUpdStat + " " + temp_table;
                    ret = ExecSQL(conn_db, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    // определить значения на определенные даты
                    sql = " update " + temp_table + " set val_cnt_pred = (select max(v.val_cnt) " +
                        " from " + cur_pref + "_data" + DBManager.tableDelimiter + counter_val_table + " v " +
                        " where v.nzp_counter = " + temp_table + ".nzp_counter " +
                            " and v.is_actual <> 100 " +
                            " and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + where_ist +
                            " and v.dat_uchet = " + temp_table + ".dat_uchet_pred)";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    #region сохранить данные в основную таблицу
                    //---------------------------------------------------------------------------
                    sql = "Insert into " + temp_main + " (ulica, nzp_dom, ndom, idom, nkor, nkvar, ikvar, num_ls, service, num_cnt, name_type, dat_uchet_pred, val_cnt_pred) " +
                        " Select t.ulica, t.nzp_dom, t.ndom, t.idom, t.nkor, t.nkvar, t.ikvar, t.num_ls, t.service, t.num_cnt, t.name_type, t.dat_uchet_pred, t.val_cnt_pred " +
                        " From " + temp_table + " t ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion
                }

                sql = " select * from " + temp_main + " order by ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, num_cnt ";
                DataTable blank = ClassDBUtils.OpenSQL(sql, "Q_master", conn_db).GetData();

                DataSet ds_rep = new DataSet();
                ds_rep.Tables.Add(blank);

                FastReport.Report rep = new FastReport.Report();
                rep.Load(System.IO.Directory.GetCurrentDirectory() + @"\Template\blank_counters.frx");

                rep.RegisterData(ds_rep);
                rep.SetParameterValue("nzp_type", finder.nzp_type.ToString());

                //параметры
                string fileName = "";
                string filePath = "";

                rep.Prepare();
                try
                {
                    var dir = "";
                    if (InputOutput.useFtp) dir = InputOutput.GetOutputDir();
                    else dir = STCLINE.KP50.Global.Constants.ExcelDir;

                    fileName = (finder.nzp_user * DateTime.Now.Second) + "_" + DateTime.Now.Ticks + "_blankCounters.fpx";
                    filePath = dir + fileName;
                    rep.SavePrepared(filePath);

                    if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));

                    return new Returns(true, fileName);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка формирования отчета \"БЛАНК РЕГИСТРАЦИИ ПОКАЗАНИЙ\" " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return new Returns(false, "Ошибка формирования отчета \"БЛАНК РЕГИСТРАЦИИ ПОКАЗАНИЙ\" ");
                }
            }
            catch (Exception ex)
            {
                conn_db.Close();
                return new Returns(false, ex.Message);
            }

        }

        /// <summary>Формирует "Бланк для регистрации показаний ПУ"</summary>
        /// <param name="finder">Входные параметры с web</param>
        /// <returns>Результат выполнение функции</returns>
        public Returns PrintCounterValBlankLight(CounterValLight finder)
        {
            IDbConnection connDB = null;

            try
            {
                string connKernel = Points.GetConnByPref(Points.Pref);
                connDB = GetConnection(connKernel);

                Returns ret = OpenDb(connDB, true);
                if (!ret.result) throw new Exception("Не удалось установить подключение");

                #region проверка значений
                //-----------------------------------------------------------------------
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");

                if (finder.month_ <= 0) throw new Exception("Не определен месяц");

                if (finder.year_ <= 0) throw new Exception("Не определен год");

                // проверка даты учета
                if (finder.dat_uchet.Trim().Length <= 0) throw new Exception("Не определена дата учета");
                //-----------------------------------------------------------------------
                #endregion

                // дата учета показаний приходит с формы
                // расчетные месяцы банков не трогать
                DateTime prevDate = Convert.ToDateTime(finder.dat_uchet),
                            curDate = prevDate.AddMonths(1);

                #region сформировать условие
                //----------------------------------------------------------------------------            
                // месяц текущих показаний
                string where = " and " + DBManager.sNvlWord + "(cs.dat_close, " + DBManager.MDY(1, 1, 3000) + ") >= " + Utils.EStrNull(curDate.ToShortDateString());

                // услуга
                if (finder.nzp_serv > 0) where += " and cs.nzp_serv = " + finder.nzp_serv;
                // улица
                if (finder.nzp_ul > 0) where += " and d.nzp_ul = " + finder.nzp_ul;
                // дом
                if (finder.nzp_dom > 0) where += " and k.nzp_dom = " + finder.nzp_dom;
                // квартира
                if (finder.nzp_kvar > 0) where += " and k.nzp_kvar = " + finder.nzp_kvar;
                // территория
                if (finder.nzp_area > 0) where += " and k.nzp_area = " + finder.nzp_area;
                // роли
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql)
                            switch (role.kod)
                            {
                                case Constants.role_sql_serv:
                                    where += " and cs.nzp_serv in (" + role.val + ")";
                                    break;
                                case Constants.role_sql_area:
                                    where += " and k.nzp_area in (" + role.val + ")";
                                    break;
                                case Constants.role_sql_geu:
                                    where += " and k.nzp_geu in (" + role.val + ")";
                                    break;
                            }
                }
                // только открытые приборы учета
                where += " and cs.dat_close is null";
                // открытые ПУ
                where += " and cs.is_actual <> 100 ";
                // тип ПУ
                where += " and cs.nzp_type = " + finder.nzp_type;
                //----------------------------------------------------------------------------
                #endregion

                const string tempTable = "tmp_pu_list";
                const string tempMain = "tmp_pu_main_list";

                #region создать таблицу под список ПУ
                //---------------------------------------------------------------------------
                ExecSQL(connDB, "drop table " + tempMain, false);

                string sql = " CREATE TEMP TABLE " + tempMain + " (" +
                            " ulica          char(40)," +
                            " nzp_dom        integer, " +
                            " ndom           char(15)," +
                            " idom           integer," +
                            " nkor           char(15)," +
                            " nkvar          char(20)," +
                            " ikvar          integer," +
                            " num_ls         integer," +
                            " nzp_counter    integer, " +
                            " service        char(100)," +
                            " num_cnt        char(20)," +
                            " name_type      char(40)," +
                            " rashod         float, " +
                            " val_cnt_cur    float," +
                            " val_cnt_pred   float," +
                            " dat_uchet_pred date " +
                            ")" + DBManager.sUnlogTempTable;

                ret = ExecSQL(connDB, sql, false);
                if (!ret.result) throw new Exception(ret.text);
                //---------------------------------------------------------------------------
                #endregion

                foreach (_Point point in Points.PointList)
                {
                    string lPrefData = point.pref + DBManager.sDataAliasRest, 
                            lPrefKernel = point.pref + DBManager.sKernelAliasRest,
                             gPrefData = Points.Pref + DBManager.sDataAliasRest;

                    #region создать временную таблицу
                    //---------------------------------------------------------------------------
                    ExecSQL(connDB, "DROP TABLE " + tempTable, false);
                    sql = " CREATE TEMP TABLE " + tempTable + " ( " +
                                " ulica         char(40)," +
                                " nzp_dom        integer, " +
                                " ndom           char(15)," +
                                " idom           integer," +
                                " nkor           char(15)," +
                                " nkvar          char(20)," +
                                " ikvar          integer," +
                                " num_ls         integer," +
                                " nzp_counter    integer, " +
                                " service        char(100)," +
                                " num_cnt        char(20)," +
                                " name_type      char(40)," +
                                " val_cnt_cur    float," +
                                " dat_uchet_cur  date, " +
                                " val_cnt_pred   float," +
                                " dat_uchet_pred date " +
                                ")" + DBManager.sUnlogTempTable;

                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    #region получить счетчики
                    //---------------------------------------------------------------------------
                    string whereIst = "";

                    if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        sql = "Insert into " + tempTable + " (ulica, nzp_dom, ndom, idom, nkor, nkvar, ikvar, num_ls, nzp_counter, service, num_cnt, name_type) " +
                            " Select distinct ul.ulica, d.nzp_dom, d.ndom, d.idom, d.nkor, " +
                            "   trim(" + DBManager.sNvlWord + "(k.nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')), k.ikvar, " +
                            " k.num_ls, cs.nzp_counter, cc.name, cs.num_cnt, t.name_type " +
                            " From " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k, " +
                                        point.pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                                        point.pref + "_data" + DBManager.tableDelimiter + "s_ulica ul, " +
                                        point.pref + "_kernel" + DBManager.tableDelimiter + "s_counts cc, " +
                                        point.pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes t, " +
                                        point.pref + "_data" + DBManager.tableDelimiter + "counters_spis cs " +
                            " Where cs.nzp = k.nzp_kvar " +
                                " and k.nzp_dom = d.nzp_dom " +
                                " and d.nzp_ul = ul.nzp_ul " +
                                " and cs.nzp_cnttype = t.nzp_cnttype " +
                                " and cs.nzp_serv = cc.nzp_serv " +
                                " and k.is_open = '1'"
                              + @where;
                        whereIst = "";
                    }
                    else
                    {
                        sql = "Insert into " + tempTable + " (ulica, nzp_dom, ndom, idom, nkor, nzp_counter, service, num_cnt, name_type) " +
                            " Select distinct ul.ulica, d.nzp_dom, d.ndom, d.idom, d.nkor, cs.nzp_counter, cc.name, cs.num_cnt, t.name_type " +
                              " From " + lPrefData + "kvar k, " +
                              lPrefData + "dom d, " +
                              lPrefData + "s_ulica ul, " +
                              lPrefKernel + "s_counts cc, " +
                              lPrefKernel + "s_counttypes t, " +
                              lPrefData + "counters_spis cs " +
                            " Where cs.nzp = d.nzp_dom  " +
                                " and k.nzp_dom = d.nzp_dom " +
                                " and d.nzp_ul = ul.nzp_ul " +
                                " and cs.nzp_cnttype = t.nzp_cnttype " +
                              " and cs.nzp_serv = cc.nzp_serv " + @where;
                    }

                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = "create index ix_" + tempTable + "_1 on " + tempTable + " (nzp_counter)";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = DBManager.sUpdStat + " " + tempTable;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    #region получить значения
                    //---------------------------------------------------------------------------
                    // определить даты
                    string counterValTable = "counters";
                    if (finder.nzp_type != (int)CounterKinds.Kvar) counterValTable = "counters_dom";

                    #region предыдущие показания
                     
                    sql = " update " + tempTable + " set dat_uchet_pred = " +
                          " (select max(v.dat_uchet) " +
                           " from " + lPrefData + counterValTable + " v " +
                           " where v.nzp_counter = " + tempTable + ".nzp_counter " +
                            " and v.is_actual <> 100 " +
                             " and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + whereIst +
                            " and v.dat_uchet <= " + DBManager.MDY(prevDate.Month, prevDate.Day, prevDate.Year) + ")";

                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #region создать индекс для даты учета
                    //---------------------------------------------------------------------------
                    sql = "create index ix_" + tempTable + "_2 on " + tempTable + " (dat_uchet_pred)";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    sql = DBManager.sUpdStat + " " + tempTable;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    // определить значения на определенные даты
                    sql = " update " + tempTable + " set val_cnt_pred = " +
                          " (select max(v.val_cnt) " +
                          " from " + lPrefData + counterValTable + " v " +
                          " where v.nzp_counter = " + tempTable + ".nzp_counter " +
                            " and v.is_actual <> 100 " +
                            " and " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + whereIst +
                            " and v.dat_uchet = " + tempTable + ".dat_uchet_pred)";

                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion

                    #region текущие показания

                    sql = " UPDATE " + tempTable + " set dat_uchet_cur = " +
                          " (SELECT MAX(v.dat_uchet) " +
                          " FROM " + lPrefData + counterValTable + " v " +
                          " WHERE v.nzp_counter = " + tempTable + ".nzp_counter " +
                             " AND v.is_actual <> 100 " +
                            " AND " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + whereIst +
                             " AND v.dat_uchet <= " + DBManager.MDY(curDate.Month, curDate.Day, curDate.Year) +
                             " AND v.dat_uchet > " + DBManager.MDY(prevDate.Month, prevDate.Day, prevDate.Year) + ") ";

                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #region создать индекс для даты учета
                    //---------------------------------------------------------------------------
                    sql = "create index ix_" + tempTable + "_3 on " + tempTable + " (dat_uchet_cur)";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    sql = DBManager.sUpdStat + " " + tempTable;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion

                    // определить значения на определенные даты
                    sql = " UPDATE " + tempTable + " SET val_cnt_cur = " +
                          " (SELECT MAX(v.val_cnt) " +
                           " FROM " + lPrefData + counterValTable + " v " +
                           " WHERE v.nzp_counter = " + tempTable + ".nzp_counter " +
                             " AND v.is_actual <> 100 " +
                             " AND " + DBManager.sNvlWord + "(v.val_cnt, -1) > -1 " + whereIst +
                             " AND v.dat_uchet = " + tempTable + ".dat_uchet_cur)";

                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #endregion
                    //---------------------------------------------------------------------------
                    #endregion

                    #region сохранить данные в основную таблицу
                    //---------------------------------------------------------------------------
                    sql = "Insert into " + tempMain + " (ulica, nzp_dom, ndom, idom, nkor, nkvar, ikvar, num_ls, service, num_cnt, " +
                                        " name_type, dat_uchet_pred, val_cnt_pred, val_cnt_cur, rashod) " +
                        " Select t.ulica, t.nzp_dom, t.ndom, t.idom, t.nkor, t.nkvar, t.ikvar, t.num_ls, " +
                                    " t.service, t.num_cnt, t.name_type, t.dat_uchet_pred, t.val_cnt_pred, " +
                                    " t.val_cnt_cur, ABS(t.val_cnt_cur - t.val_cnt_pred) AS rashod " +
                          " From " + tempTable + " t ";
                    ret = ExecSQL(connDB, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                    //---------------------------------------------------------------------------
                    #endregion
                }

                sql = " select * from " + tempMain + " order by ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, num_cnt ";
                DataTable blank = ClassDBUtils.OpenSQL(sql, "Q_master", connDB).GetData();

                var dsRep = new DataSet();
                dsRep.Tables.Add(blank);

                var rep = new Report();
                rep.Load(Directory.GetCurrentDirectory() + @"\Template\blank_counters.frx");

                rep.RegisterData(dsRep);
                rep.SetParameterValue("nzp_type", finder.nzp_type.ToString(CultureInfo.InvariantCulture));

                rep.Prepare();
                try
                {
                    string dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : Constants.ExcelDir;

                    string fileName = (finder.nzp_user * DateTime.Now.Second) + "_" + DateTime.Now.Ticks + "_blankCounters.fpx";
                    string filePath = dir + fileName;
                    rep.SavePrepared(filePath);

                    if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));

                    return new Returns(true, fileName);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка формирования отчета \"БЛАНК РЕГИСТРАЦИИ ПОКАЗАНИЙ\" " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return new Returns(false, "Ошибка формирования отчета \"БЛАНК РЕГИСТРАЦИИ ПОКАЗАНИЙ\" ");
                }
            }
            catch (Exception ex)
            {
                if (connDB != null) connDB.Close();
                return new Returns(false, ex.Message);
            }
        }

    }
}
