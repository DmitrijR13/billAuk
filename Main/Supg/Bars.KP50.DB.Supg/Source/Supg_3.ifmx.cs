using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// процедура возвращает результат поиска заявок СУПГ
        /// </summary>
        /// <returns></returns>
        public DataTable OrderList(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable Res_Table = new DataTable();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            string tXX_supg = "";

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

                tXX_supg = "temp" + nzp_user.ToString() + "_supg";

                //удаляем если есть такая таблица в базе
                if (TableInWebCashe(con_web, tXX_supg))
                {
                    ExecSQL(con_web, " Drop table " + tXX_supg, false);
                }

                try
                {
#if PG
                    ret = ExecSQL(con_web,
                                                 " Create table public." + tXX_supg +
                                                 " ( pref      char(30), " +
                                                 "   nzp_zvk   integer not null , " +
                                                 "   date      char(50), " +
                                                 "   adr       char(160), " +
                                                 "   comment   TEXT " +
                                                 " ) ", true);
#else
                    ret = ExecSQL(con_web,
                                                 " Create table webdb." + tXX_supg +
                                                 " ( pref      char(30), " +
                                                 "   nzp_zvk   integer not null , " +
                                                 "   date      char(50), " +
                                                 "   adr       char(160), " +
                                                 "   comment   TEXT " +
                                                 " ) ", true);
#endif
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при создании таблицы temp" + nzp_user.ToString() + "_supg: " + ex.Message, MonitorLog.typelog.Error, true);
                }

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    con_web.Close();
                    return null;
                }
#if PG
                string sql = " Insert into " + tXX_supg + " (pref, nzp_zvk, date, adr, comment) " +
                             " Select pref, nzp_zvk, zvk_date, adr, comment from " + "t" + nzp_user + "_supg";
#else
                string sql = " Insert into " + tXX_supg + " (pref, nzp_zvk, date, adr, comment) " +
                             " Select pref, nzp_zvk, zvk_date, adr, comment from " + "t" + nzp_user + "_supg";
#endif
                ret = ExecSQL(con_web, sql, true);

                if (!ret.result)
                {
                    con_web.Close();
                    return null;
                }
#if PG
                sql = "Select pref, nzp_zvk, date, adr, comment From public." + tXX_supg;
#else
                sql = "Select pref, nzp_zvk, date, adr, comment From " + con_web.Database + ":" + tXX_supg;
#endif
                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки OrderList" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                #region Запись выгрузки

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                try
                {
                    if (reader != null)
                    {
                        Res_Table.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в OrderList" + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }

                #endregion

                #region Добавление двух дополнительных колонок

                DataColumn res_point = Res_Table.Columns.Add("res_point", typeof(String));
                DataColumn actions = Res_Table.Columns.Add("actions", typeof(String));

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                for (int i = 0; i < Res_Table.Rows.Count; i++)
                {
                    ArrayList acts = new ArrayList();
                    string result = "";
                    string res_name = "";
                    string fact_date = "";
                    string result_comment = "";
                    string acts_str = "";
                    string cur_pref = Res_Table.Rows[i][0].ToString().Trim();
                    string cur_nzp_zvk = Res_Table.Rows[i][1].ToString().Trim();
#if PG
                    sql = " Select a.res_name, b.fact_date, b.result_comment From " + Points.Pref + "_supg. s_result a, " + Points.Pref + "_supg. zvk b " +
                          " Where b.nzp_zvk = " + cur_nzp_zvk + " and b.nzp_res = a.nzp_res";
#else
                    sql = " Select a.res_name, b.fact_date, b.result_comment From " + Points.Pref + "_supg: s_result a, " + Points.Pref + "_supg: zvk b " +
                          " Where b.nzp_zvk = " + cur_nzp_zvk + " and b.nzp_res = a.nzp_res";
#endif
                    if (!ExecRead(con_db, out reader, sql, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки OrderList" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["res_name"] != DBNull.Value) res_name = Convert.ToString(reader["res_name"]).Trim();
                            if (reader["fact_date"] != DBNull.Value)
                                fact_date = Convert.ToString(reader["fact_date"]).Substring(0, Convert.ToString(reader["fact_date"]).Length - 7).Trim();
                            if (reader["result_comment"] != DBNull.Value) result_comment = Convert.ToString(reader["result_comment"]).Trim();
                            result = res_name + " " + fact_date + "\r\n" + result_comment;
                        }
                    }
                    Res_Table.Rows[i][5] = result;



#if PG
                    sql = " Select 'Переадресована '||trim(s.slug_name)||' '||date(r._date)::varchar as str " +
                          " From " + Points.Pref + "_supg.readdress r, " + Points.Pref + "_supg.s_slug s " +
                          " Where r.nzp_zvk= " + cur_nzp_zvk + " and r.nzp_slug=s.nzp_slug order by r._date ";
#else
                    sql = " Select 'Переадресована '||trim(s.slug_name)||' '||date(r._date) as str " +
                          " From " + Points.Pref + "_supg:readdress r, " + Points.Pref + "_supg:s_slug s " +
                          " Where r.nzp_zvk= " + cur_nzp_zvk + " and r.nzp_slug=s.nzp_slug order by r._date ";
#endif
                    if (!ExecRead(con_db, out reader, sql, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки OrderList" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["str"] != DBNull.Value) acts.Add((acts.Count + 1).ToString() + ")" + Convert.ToString(reader["str"]).Trim());
                        }
                    }
#if PG
                    sql = "select 'Наряд-заказ '||z.nzp_zk||' от '||date(z.order_date)||' '||trim(s.name_supp)||'; '|| " +
                          "trim(r.res_name)|| " +
                          "(case when z.fact_date is null then '' else ' '||date(z.fact_date) end)||';' " +
                          "||(case when z.replno is null then '' else ' Не подтверждено жильцом; Выдан повторный '||z.replno end) as str2 " +
                          "from " + Points.Pref + "_supg.zakaz z, " + Points.Pref + "_supg.s_result r, " + Points.Pref + "_kernel.supplier s " +
                          "where z.nzp_zvk=" + cur_nzp_zvk +
                          "and z.nzp_res=r.nzp_res " +
                          "and z.nzp_supp=s.nzp_supp " +
                          "order by z.norm, z.nzp_zk";
#else
                    sql = "select 'Наряд-заказ '||z.nzp_zk||' от '||date(z.order_date)||' '||trim(s.name_supp)||'; '|| " +
                          "trim(r.res_name)|| " +
                          "(case when z.fact_date is null then '' else ' '||date(z.fact_date) end)||';' " +
                          "||(case when z.replno is null then '' else ' Не подтверждено жильцом; Выдан повторный '||z.replno end) as str2 " +
                          "from " + Points.Pref + "_supg:zakaz z, " + Points.Pref + "_supg:s_result r, " + Points.Pref + "_kernel:supplier s " +
                          "where z.nzp_zvk=" + cur_nzp_zvk +
                          "and z.nzp_res=r.nzp_res " +
                          "and z.nzp_supp=s.nzp_supp " +
                          "order by z.norm, z.nzp_zk";
#endif
                    if (!ExecRead(con_db, out reader, sql, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки OrderList" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["str2"] != DBNull.Value) acts.Add((acts.Count + 1).ToString() + ")" + Convert.ToString(reader["str2"]).Trim());
                        }
                    }
                    acts_str = "";
                    for (int k = 0; k < acts.Count; k++)
                    {
                        acts_str += acts[k].ToString();

                        if (k != acts.Count)
                        {
                            acts_str += "\r\n";
                        }
                    }
                    Res_Table.Rows[i][6] = acts_str;
                }

                #endregion

                return Res_Table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetOrderList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                //удаляем временную таблицу
                ret = ExecSQL(con_web, " Drop table " + tXX_supg + "; ", true);

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

                #endregion
            }
        }


        public DataTable GetReportInfo(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable res_table = new DataTable();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            string sql = "";

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

                string table_name = "t" + Convert.ToString(nzp_user) + "_spfinder";
#if PG
                sql = "Select name, value From public." + table_name + " Where nzp_page = " + Constants.page_spis_order;
#else
                sql = "Select name, value From " + con_web.Database + ":" + table_name + " Where nzp_page = " + Constants.page_spis_order;
#endif
                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetReportInfo" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                try
                {
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetReportInfo" + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                return res_table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetReportInfo : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }
        }


        /// <summary>
        /// Изменение marks для выбранных списков нарядов заказов
        /// </summary>
        /// <param name="list0">список не выбранных лс</param>
        /// <param name="list1">список выбранных лс</param>
        /// <param name="finder">nzp_user необходим</param>
        /// <returns></returns>
        public Returns ChangeMarksSpisSupg(SupgFinder finder, List<SupgFinder> list0, List<SupgFinder> list1)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            string true_ = "";
            for (int i = 0; i < list1.Count; i++)
            {
                if (i == 0) true_ += list1[i].nzp_zk;
                else true_ += "," + list1[i].nzp_zk;
            }
            if (true_ != "") true_ = "(" + true_ + ")";

            string false_ = "";
            for (int i = 0; i < list0.Count; i++)
            {
                if (i == 0) false_ += list0[i].nzp_zk;
                else false_ += "," + list0[i].nzp_zk;
            }
            if (false_ != "") false_ = "(" + false_ + ")";

            string tXX_sp = "";
            string keyfield = "";
            if (finder.dopFind.Count > 0)
            {
                if (finder.dopFind[0] == Constants.page_incoming_job_orders.ToString() || finder.dopFind[0] == Constants.page_supg_kvar_job_order.ToString())
                {
                    tXX_sp = "t" + Convert.ToString(finder.nzp_user) + "_zakaz";
                    keyfield = "nzp_zk";
                }
            }
            if (tXX_sp != "" && keyfield != "")
            {
                //выбрать общее кол-во
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret;

                if (!TableInWebCashe(conn_web, tXX_sp))
                {
                    conn_web.Close();
                    ret.tag = -1;
                    ret.result = false;
                    ret.text = "Данные не были выбраны";
                    return ret;
                }

                string sql = "";
                if (true_ != "")
                {
#if PG
                    sql = "update " + tXX_sp + " set mark = 1 where " + keyfield + " in " + true_;
#else
                    sql = "update " + tXX_sp + " set mark = 1 where " + keyfield + " in " + true_;
#endif
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                if (false_ != "")
                {
#if PG
                    sql = "update " + tXX_sp + " set mark = 0 where " + keyfield + " in " + false_;
#else
                    sql = "update " + tXX_sp + " set mark = 0 where " + keyfield + " in " + false_;
#endif
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                conn_web.Close();
            }
            return ret;
        }

        /// <summary>
        /// отчет для списка поступивших нарядов-заказов
        /// </summary>
        /// <param name="list0">список не выбранных лс</param>
        /// <param name="list1">список выбранных лс</param>
        /// <param name="finder">nzp_user необходим</param>
        /// <returns></returns>
        public DataTable GetIncomingJobOrders(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable Tube_Table = new DataTable();
            DataTable Res_Table = new DataTable();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;

            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

#if PG
                string sql = " Select pref, nzp_zk, order_date, adr, comment, nzp_supp " +
                                            " From public." + "t" + nzp_user + "_zakaz where mark = 1 Order by nzp_supp";
#else
                string sql = " Select pref, nzp_zk, order_date, adr, comment, nzp_supp " +
                                            " From " + con_web.Database + ":" + "t" + nzp_user + "_zakaz where mark = 1 Order by nzp_supp";
#endif
                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetIncomingJobOrders" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                #region Запись выгрузки

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                try
                {
                    if (reader != null)
                    {
                        Tube_Table.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetIncomingJobOrders" + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }

                #endregion

                #region Добавление и заполнение колонок результирующей таблицы

                DataColumn num_date = Res_Table.Columns.Add("num_date", typeof(String));
                DataColumn adr_phone_fio = Res_Table.Columns.Add("adr_phone_fio", typeof(String));
                DataColumn comment = Res_Table.Columns.Add("comment", typeof(String));

                DataColumn ned_names = Res_Table.Columns.Add("act_ned_name", typeof(String));
                DataColumn nedop_act_s = Res_Table.Columns.Add("nedop_s", typeof(String));
                DataColumn result = Res_Table.Columns.Add("result", typeof(String));
                DataColumn supp = Res_Table.Columns.Add("supp", typeof(String));


                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                for (int i = 0; i < Tube_Table.Rows.Count; i++)
                {

                    DataRow row1 = Res_Table.NewRow();
                    DataRow row2 = Res_Table.NewRow();

                    string phone = "";
                    string demand_name = "";

                    string ned_name = "";
                    string act_ned_name = "";

                    string nedop_s = "";
                    string act_s = "";
                    string res = "";
                    string supp_name = "";

                    string cur_pref = Tube_Table.Rows[i][0].ToString().Trim();//текущий префикс
                    string cur_nzp_zk = Tube_Table.Rows[i][1].ToString().Trim();//текущий номер наряда-заказа

#if PG
                    sql = "  select " +
                                             "  z.phone, " +
                                             "  z.demand_name, " +
                                             "  n1.name as ned_name, n2.name as act_ned_name, " +
                                             "  (case when n1.name is null then null else zk.nedop_s end) nedop_s, (case when n2.name is null then null else zk.act_s end) act_s, " +
                                             "  (case when zk.nzp_res =5 then null else (case when zk.fact_date is null then null else trim(r.res_name)||' '||zk.fact_date end) end) as result " +
                                             "  From " +
                                             Points.Pref + "_supg. zvk z,  " +
                                             Points.Pref + "_supg. zakaz zk" +
                                             " left outer join " + Points.Pref + "_data.upg_s_kind_nedop n2 on zk.act_num_nedop=n2.nzp_kind and n2.kod_kind=1, " +
                                             Points.Pref + "_data. s_ulica u, " +
                                             Points.Pref + "_data. dom d,  " +
                                             Points.Pref + "_data. kvar k, " +
                                             Points.Pref + "_supg. s_result r,  " +
                                             Points.Pref + "_supg. s_dest ds left outer join " + Points.Pref + "_data.upg_s_kind_nedop n1 on ds.num_nedop=n1.nzp_kind and n1.kod_kind=1 " +
                                             "  Where " + cur_nzp_zk + " = zk.nzp_zk  " +
                                             "  and zk.nzp_zvk=z.nzp_zvk  " +
                                             "  and z.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                                             "  and zk.nzp_dest = ds.nzp_dest " +
                                             "  and zk.nzp_res = r.nzp_res ";
#else
                    sql = "  select " +
                                             "  z.phone, " +
                                             "  z.demand_name, " +
                                             "  n1.name as ned_name, n2.name as act_ned_name, " +
                                             "  (case when n1.name is null then null else zk.nedop_s end) nedop_s, (case when n2.name is null then null else zk.act_s end) act_s, " +
                                             "  (case when zk.nzp_res =5 then null else (case when zk.fact_date is null then null else trim(r.res_name)||' '||zk.fact_date end) end) as result " +
                                             "  From " +
                                             Points.Pref + "_supg: zvk z,  " +
                                             Points.Pref + "_supg: zakaz zk, " +
                                             Points.Pref + "_data: s_ulica u, " +
                                             Points.Pref + "_data: dom d,  " +
                                             Points.Pref + "_data: kvar k, " +
                                             Points.Pref + "_supg: s_dest ds,  " +
                                             Points.Pref + "_supg: s_result r, outer(" + Points.Pref + "_data:upg_s_kind_nedop n1), outer(" + Points.Pref + "_data:upg_s_kind_nedop n2) " +
                                             "  Where " + cur_nzp_zk + " = zk.nzp_zk  " +
                                             "  and zk.nzp_zvk=z.nzp_zvk  " +
                                             "  and z.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                                             "  and zk.nzp_dest = ds.nzp_dest and ds.num_nedop=n1.nzp_kind and n1.kod_kind=1 " +
                                             "  and zk.act_num_nedop=n2.nzp_kind and n2.kod_kind=1 " +
                                             "  and zk.nzp_res = r.nzp_res ";
#endif

                    if (!ExecRead(con_db, out reader, sql, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки GetIncomingJobOrders" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["phone"] != DBNull.Value) phone = Convert.ToString(reader["phone"]).Trim();
                            if (reader["demand_name"] != DBNull.Value) demand_name = Convert.ToString(reader["demand_name"]).Trim();

                            if (reader["ned_name"] != DBNull.Value) ned_name = Convert.ToString(reader["ned_name"]).Trim();
                            if (reader["act_ned_name"] != DBNull.Value) act_ned_name = Convert.ToString(reader["act_ned_name"]).Trim();

                            if (reader["nedop_s"] != DBNull.Value) nedop_s = Convert.ToString(reader["nedop_s"]).Trim();
                            if (reader["act_s"] != DBNull.Value) act_s = Convert.ToString(reader["act_s"]).Trim();

                            if (reader["result"] != DBNull.Value) res = Convert.ToString(reader["result"]).Trim();
                        }
                    }

                    row1[0] = "#" + Tube_Table.Rows[i][1].ToString().Trim() + " от " + Tube_Table.Rows[i][2].ToString().Trim();
                    row1[1] = Tube_Table.Rows[i][3].ToString().Trim() + ", " + phone.Trim() + ", " + demand_name.Trim();
                    row1[2] = Tube_Table.Rows[i][4].ToString().Trim();//comment

                    row1[3] = ned_name;
                    row2[3] = act_ned_name;
                    row1[4] = nedop_s;
                    row2[4] = act_s;
                    row1[5] = res;


                    #region определяем поставщиков
#if PG
                    sql = "Select name_supp from " + Points.Pref + "_kernel. supplier Where  nzp_supp = " + Tube_Table.Rows[i][5].ToString().Trim();
#else
                    sql = "Select name_supp from " + Points.Pref + "_kernel: supplier Where  nzp_supp = " + Tube_Table.Rows[i][5].ToString().Trim();
#endif
                    if (!ExecRead(con_db, out reader, sql, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки GetIncomingJobOrders" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["name_supp"] != DBNull.Value) supp_name = Convert.ToString(reader["name_supp"]).Trim();
                        }
                    }

                    row1[6] = supp_name;

                    #endregion

                    Res_Table.Rows.Add(row1);
                    Res_Table.Rows.Add(row2);

                }

                #endregion

                return Res_Table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetOrderList : " + ex.Message, MonitorLog.typelog.Error, true);
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

                #endregion
            }
        }


        /// <summary>
        /// отчет по количеству заявлений, направленных по услугам за период
        /// </summary>
        /// <param name="nzp_user">номер пользователя</param>
        /// <param name="_nzp">параметр отчета</param>
        /// <param name="s_date">период с</param>
        /// <param name="po_date">период по</param>
        /// <returns></returns>
        public DataTable GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date, out Returns ret)
        {
            ret = Utils.InitReturns();
            string sql = "";
            IDataReader reader = null;
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            DataTable res_table = new DataTable();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            try
            {

                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result) return null;

                string tXX_supg = "t" + finder.nzp_user + "_supg";
                string temp_tXX_supg = "tcalc" + finder.nzp_user + "_supg"; ;

                if (TableInWebCashe(con_web, temp_tXX_supg))
                {
                    ExecSQL(con_web, " Drop table " + temp_tXX_supg, false);
                }

                #region Создание временной таблицы

                try
                {
#if PG
                    ret = ExecSQL(con_web,
                              " Create table public." + temp_tXX_supg +
                              " ( _nzp         integer, " +
                              "   inexec_bf    integer, " +
                              "   crt_p        integer, " +
                              "   crt_pp       integer, " +
                              "   otm_p        integer, " +
                              "   fact_p       integer, " +
                              "   fact_pp      integer, " +
                              "   fact_np      integer,  " +
                              "   plan_p       integer  " +
                              " )", true);
#else
                    ret = ExecSQL(con_web,
                              " Create table webdb." + temp_tXX_supg +
                              " ( _nzp         integer, " +
                              "   inexec_bf    integer, " +
                              "   crt_p        integer, " +
                              "   crt_pp       integer, " +
                              "   otm_p        integer, " +
                              "   fact_p       integer, " +
                              "   fact_pp      integer, " +
                              "   fact_np      integer,  " +
                              "   plan_p       integer  " +
                              " )", true);
#endif
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при создании таблицы supg" + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                string s = Convert.ToDateTime(s_date).ToString("yyyy-MM-dd HH:mm:ss");
                string po = Convert.ToDateTime(po_date).ToString("yyyy-MM-dd HH:mm:ss");

                #if PG
                string s_fact = Convert.ToDateTime(s_date).ToString("yyyy-MM-dd HH:mm:ss");
                string po_fact = Convert.ToDateTime(po_date).ToString("yyyy-MM-dd HH:mm:ss");
                #else
                string s_fact = Convert.ToDateTime(s_date).ToString("yyyy-MM-dd HH");
                string po_fact = Convert.ToDateTime(po_date).ToString("yyyy-MM-dd HH");
                #endif



                // выяснить, выполнялся ли запрос по нарядам-заказам (ExSelZk = true: выполнялся)
                bool ExSelZk = false;

#if PG
                sql = "select count(*) cnt from public." + tXX_supg + " where nzp_zk is not null";
#else
                sql = "select count(*) cnt from " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + tXX_supg + " where nzp_zk is not null";
#endif
                if (ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    if (reader != null)
                    {
                        if (reader.Read())
                            if (reader["cnt"] != DBNull.Value) ExSelZk = (Convert.ToInt32(reader["cnt"]) > 0);
                    }
                }

#if PG
                sql = "Insert into public." + temp_tXX_supg + " select z." + _nzp +
                    //-- Не выполнено к началу периода
                                   ", sum((case when z.order_date< \'" + s + "\' and (z.nzp_res=5 or (z.nzp_res <>5 and z.fact_date >= \'" + s_fact + "\')) then 1 else 0 end)) inexec_bf, " +
                    //-- Направлено за период
                                   " sum((case when z.order_date between \'" + s + "\' and \'" + po + "\' then 1 else 0 end)) crt_p, " +
                    //Направлено за период в т.ч. повторно
                                   " sum((case when z.order_date between \'" + s + "\' and \'" + po + "\' and z.nzp_zk<>z.norm then 1 else 0 end)) crt_pp, " +
                    //Отклонено
                                   " sum((case when z.nzp_res=4 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) otm_p, " +
                    //Выполнено за период
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) fact_p, " +
                    //Выполнено за период, подтверждено жильцом
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' and z.nzp_atts=2 then 1 else 0 end)) fact_pp, " +
                    //Выполнено за период, не подтверждено жильцом
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' and z.nzp_atts=3 then 1 else 0 end)) fact_np, " +
                    //плановый ремонт
                                   " sum((case when z.nzp_res=2 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) plan_p " +
                                   " from public." + tXX_supg + " t, " + Points.Pref + "_supg.zakaz z," + Points.Pref + "_supg. zvk zvk ";
#else
                sql = "Insert into " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + temp_tXX_supg + " select z." + _nzp +
                    //-- Не выполнено к началу периода
                                   ", sum((case when z.order_date< \'" + s + "\' and (z.nzp_res=5 or (z.nzp_res <>5 and z.fact_date >= \'" + s_fact + "\')) then 1 else 0 end)) inexec_bf, " +
                    //-- Направлено за период
                                   " sum((case when z.order_date between \'" + s + "\' and \'" + po + "\' then 1 else 0 end)) crt_p, " +
                    //Направлено за период в т.ч. повторно
                                   " sum((case when z.order_date between \'" + s + "\' and \'" + po + "\' and z.nzp_zk<>z.norm then 1 else 0 end)) crt_pp, " +
                    //Отклонено
                                   " sum((case when z.nzp_res=4 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) otm_p, " +
                    //Выполнено за период
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) fact_p, " +
                    //Выполнено за период, подтверждено жильцом
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' and z.nzp_atts=2 then 1 else 0 end)) fact_pp, " +
                    //Выполнено за период, не подтверждено жильцом
                                   " sum((case when z.nzp_res=3 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' and z.nzp_atts=3 then 1 else 0 end)) fact_np, " +
                    //плановый ремонт
                                   " sum((case when z.nzp_res=2 and z.fact_date between \'" + s_fact + "\' and \'" + po_fact + "\' then 1 else 0 end)) plan_p " +
                                   " from " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + tXX_supg + " t, " + Points.Pref + "_supg:zakaz z," + Points.Pref + "_supg: zvk zvk ";
#endif
#if PG
                if (ExSelZk)
                {
                    sql = sql + " where t.nzp_zk = z.nzp_zk and z.nzp_zvk = zvk.nzp_zvk ";
                }
                else
                {
                    sql = sql + " where t.nzp_zvk = zvk.nzp_zvk and zvk.nzp_zvk = z.nzp_zvk ";
                }
                sql = sql + " and z.order_date < \'" + po + "\' and " +
                            " (z.fact_date is null or z.fact_date >= \'" + s_fact + "\') " +
                            " group by 1";
#else
                if (ExSelZk)
                {
                    sql = sql + " where t.nzp_zk = z.nzp_zk and z.nzp_zvk = zvk.nzp_zvk ";
                }
                else
                {
                    sql = sql + " where t.nzp_zvk = zvk.nzp_zvk and zvk.nzp_zvk = z.nzp_zvk ";
                }
                sql = sql + " and z.order_date < \'" + po + "\' and " +
                            " (z.fact_date is null or z.fact_date >= \'" + s_fact + "\') " +
                            " group by 1";
#endif
                ret = ExecSQL(con_db, sql, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при заполнении данными " + temp_tXX_supg + " в GetCountOrdersServ " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_web.Close();
                    return null;
                }
#if PG
                if (_nzp == "nzp_dest" && _nzp_add == "")
                {
                    sql = " select case when d.nzp_serv = 1000 then '-' else s.service end as service, " +
                                  " SUM(t.inexec_bf) as inexec_bf, " +
                                  " SUM(t.crt_p)   as crt_p, " +
                                  " SUM(t.crt_pp)  as crt_pp, " +
                                  " SUM(t.otm_p)   as otm_p,  " +
                                  " SUM(t.fact_p)  as fact_p, " +
                                  " SUM(t.fact_pp) as fact_pp, " +
                                  " SUM(t.fact_np) as fact_np, " +
                                  " SUM(t.plan_p)  as plan_p " +
                                  " from " + Points.Pref + "_kernel. services s, " + Points.Pref + "_supg. s_dest d, public." + temp_tXX_supg + " t " +
                                  " where t. _nzp = d.nzp_dest and d.nzp_serv = s.nzp_serv group by 1 order by 1";
                }
                if (_nzp == "nzp_dest" && _nzp_add != "")
                {
                    sql = " select d.dest_name as service, " +
                                  " SUM(t.inexec_bf) as inexec_bf, " +
                                  " SUM(t.crt_p)   as crt_p, " +
                                  " SUM(t.crt_pp)  as crt_pp, " +
                                  " SUM(t.otm_p)   as otm_p,  " +
                                  " SUM(t.fact_p)  as fact_p, " +
                                  " SUM(t.fact_pp) as fact_pp, " +
                                  " SUM(t.fact_np) as fact_np, " +
                                  " SUM(t.plan_p)  as plan_p " +
                                  " from " + Points.Pref + "_supg. s_dest d, public." + temp_tXX_supg + " t where t._nzp = d.nzp_dest group by 1 order by 1";
                }
                if (_nzp == "nzp_supp")
                {
                    sql = " select s.name_supp, " +
                                 " SUM(t.inexec_bf) as inexec_bf, " +
                                 " SUM(t.crt_p)   as crt_p, " +
                                 " SUM(t.crt_pp)  as crt_pp, " +
                                 " SUM(t.otm_p)   as otm_p,  " +
                                 " SUM(t.fact_p)  as fact_p, " +
                                 " SUM(t.fact_pp) as fact_pp, " +
                                 " SUM(t.fact_np) as fact_np, " +
                                 " SUM(t.plan_p)  as plan_p " +
                                 " From " + Points.Pref + "_kernel. supplier s, public." + temp_tXX_supg + " t " +
                                 " Where t. _nzp = s.nzp_supp group by 1 order by 1";
                }
#else
                if (_nzp == "nzp_dest" && _nzp_add == "")
                {
                    sql = " select case when d.nzp_serv = 1000 then '-' else s.service end as service, " +
                                  " SUM(t.inexec_bf) as inexec_bf, " +
                                  " SUM(t.crt_p)   as crt_p, " +
                                  " SUM(t.crt_pp)  as crt_pp, " +
                                  " SUM(t.otm_p)   as otm_p,  " +
                                  " SUM(t.fact_p)  as fact_p, " +
                                  " SUM(t.fact_pp) as fact_pp, " +
                                  " SUM(t.fact_np) as fact_np, " +
                                  " SUM(t.plan_p)  as plan_p " +
                                  " from outer(" + Points.Pref + "_kernel: services s, " + Points.Pref + "_supg: s_dest d), " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + temp_tXX_supg + " t where t. _nzp = d.nzp_dest and d.nzp_serv = s.nzp_serv group by 1 order by 1";
                }
                if (_nzp == "nzp_dest" && _nzp_add != "")
                {
                    sql = " select d.dest_name as service, " +
                                  " SUM(t.inexec_bf) as inexec_bf, " +
                                  " SUM(t.crt_p)   as crt_p, " +
                                  " SUM(t.crt_pp)  as crt_pp, " +
                                  " SUM(t.otm_p)   as otm_p,  " +
                                  " SUM(t.fact_p)  as fact_p, " +
                                  " SUM(t.fact_pp) as fact_pp, " +
                                  " SUM(t.fact_np) as fact_np, " +
                                  " SUM(t.plan_p)  as plan_p " +
                                  " from " + Points.Pref + "_supg: s_dest d, " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + temp_tXX_supg + " t where t._nzp = d.nzp_dest group by 1 order by 1";
                }
                if (_nzp == "nzp_supp")
                {
                    sql = " select s.name_supp, " +
                                 " SUM(t.inexec_bf) as inexec_bf, " +
                                 " SUM(t.crt_p)   as crt_p, " +
                                 " SUM(t.crt_pp)  as crt_pp, " +
                                 " SUM(t.otm_p)   as otm_p,  " +
                                 " SUM(t.fact_p)  as fact_p, " +
                                 " SUM(t.fact_pp) as fact_pp, " +
                                 " SUM(t.fact_np) as fact_np, " +
                                 " SUM(t.plan_p)  as plan_p " +
                                 " From " + Points.Pref + "_kernel: supplier s, " + con_web.Database + "@" + DBManager.getServer(con_web) + ":" + temp_tXX_supg + " t " +
                                 " Where t. _nzp = s.nzp_supp group by 1 order by 1";
                }
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetCountOrdersServ" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                try
                {
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetCountOrdersServ " + ex.Message, MonitorLog.typelog.Error, true);
                    reader.Close();
                    return null;
                }

                return res_table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetOrderList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }

        }



        /// <summary>
        /// процедура возвращает журнал выгрузок
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Journal> GetJournal(Journal finder, out Returns ret)
        {
            List<Journal> resJournal = new List<Journal>();
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            IDbConnection conn_db = null;
            ret = Utils.InitReturns();
            //if (finder.nzp_user < 1)
            //{
            //    ret.result = false;
            //    ret.text = "Не определен пользователь";
            //    return null;
            //}

            string dbsupg = "";

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;


                #region Определить БД СУПГ (распределение недопоставок будет выполняться в Комплат)

#if PG
                sql.Append("select * from " + Points.Pref + "_kernel .s_baselist where idtype=6");
#else
                sql.Append("select * from " + Points.Pref + "_kernel :s_baselist where idtype=6");
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    string err_text = "Ошибка при определении ссылки на БД СУПГ ";
                    MonitorLog.WriteLog(err_text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = err_text;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value)
                        {
                            dbsupg = reader["dbname"].ToString().Trim();
                        }
                    }
                }

                if (dbsupg == "")
                {
                    string err_text = "В системе не определена ссылка на БД СУПГ!";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = err_text;
                    return null;
                }

                // Проверить доступ на БД СУПГ
                bool TabExists;
#if PG
                TabExists = (TempTableInWebCashe(conn_db, Points.Pref + "_supg. nedop_kvar"));
#else
                TabExists= (TempTableInWebCashe(conn_db,  dbsupg + ":nedop_kvar"));
#endif
                if (!TabExists)
                {
                    string err_text = "Банк данных " + dbsupg + " недоступен";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Warn, true);
                    ret.text = err_text;
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(" select " +
                           " g.no, g.d_when, date(g.d_begin) d_begin, date(g.d_end) d_end, reg_begin, reg_end, g.crt_count, g.cnc_count, " +
                           " g.status, g.is_actual, " +
                           " u1.comment as uname,  " +
                           " u2.comment as kp_uname, g.kp_when, g.exc_path  " +
                           " from " + Points.Pref + "_supg. jrn_upg_nedop g left outer join " + Points.Pref + "_data.users u2 on g.kp_nzp_user = u2.nzp_user ," + Points.Pref + "_data.users u1 " +
                           " where g.nzp_user = u1.nzp_user AND is_actual > 0 ");
#else
                sql.Append(" select " +
                           " g.no, g.d_when, date(g.d_begin) d_begin, date(g.d_end) d_end, reg_begin, reg_end, g.crt_count, g.cnc_count, " +
                           " g.status, g.is_actual, " +
                           " u1.comment as uname,  " +
                           " u2.comment as kp_uname, g.kp_when, g.exc_path  " +
                           " from " + dbsupg + ":jrn_upg_nedop g, " + Points.Pref + "_data:users u1,  outer(" + Points.Pref + "_data:users u2)  " +
                           " where g.nzp_user = u1.nzp_user " +
                           " and g.kp_nzp_user = u2.nzp_user ");
#endif
                if (finder.number != 0) sql.Append(" and g.no = " + finder.number.ToString());
                if (finder.status == 2)
                    // только готовые к распределению и распределенные
#if PG
                    sql.Append(" and g.status in (1,2)");
                sql.Append(" order by g.d_when desc, g.no desc");
#else
                    sql.Append(" and g.status in (1,2)");
                sql.Append(" order by g.d_when desc, g.no desc");
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetJournal" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Journal jr = new Journal();
                        DateTime d_when;
                        DateTime date;

                        if (reader["no"] != DBNull.Value) jr.number = Convert.ToInt32(reader["no"]);

                        if (reader["d_when"] != DBNull.Value) jr.d_when = Convert.ToString(reader["d_when"]).Trim();
                        else jr.d_when = "";

                        if (DateTime.TryParse(jr.d_when, out d_when))
                        {
                            jr.d_when = d_when.ToString("dd.MM.yyyy");
                        }

                        if (reader["d_begin"] != DBNull.Value) jr.d_begin = Convert.ToString(reader["d_begin"]).Trim();
                        else jr.d_begin = "";

                        if (reader["d_end"] != DBNull.Value) jr.d_end = Convert.ToString(reader["d_end"]).Trim();
                        else jr.d_end = "";

                        if (reader["reg_begin"] != DBNull.Value) jr.doc_begin = Convert.ToString(reader["reg_begin"]).Trim();
                        else jr.doc_begin = "";

                        if (reader["reg_end"] != DBNull.Value) jr.doc_end = Convert.ToString(reader["reg_end"]).Trim();
                        else jr.doc_end = "";

                        if (DateTime.TryParse(jr.d_begin, out date))
                        {
                            jr.d_begin = date.ToString("dd.MM.yyyy");
                        }
                        if (DateTime.TryParse(jr.d_end, out date))
                        {
                            jr.d_end = date.ToString("dd.MM.yyyy");
                        }
                        if (DateTime.TryParse(jr.doc_begin, out date))
                        {
                            jr.doc_begin = date.ToString("dd.MM.yyyy");
                        }
                        if (DateTime.TryParse(jr.doc_end, out date))
                        {
                            jr.doc_end = date.ToString("dd.MM.yyyy");
                        }

                        if (reader["is_actual"] != DBNull.Value) jr.is_actual = Convert.ToInt32(reader["is_actual"]);
                        else jr.is_actual = 0;

                        jr.is_actual_text = "";
                        if (jr.is_actual == 2) jr.is_actual_text = "Акты о недоп.";
                        if (jr.is_actual == 1) jr.is_actual_text = "Наряд-заказ";

                        if (reader["status"] != DBNull.Value) jr.status = Convert.ToInt32(reader["status"]);
                        else jr.status = 0;

                        jr.status_text = "Неопределен";
                        if (jr.status == 1) jr.status_text = "К распределению";
                        if (jr.status == 2) jr.status_text = "Распределено";

                        if (reader["uname"] != DBNull.Value) jr.name = Convert.ToString(reader["uname"]).Trim();
                        else jr.name = "";

                        if (reader["crt_count"] != DBNull.Value) jr.crt_count = Convert.ToInt32(reader["crt_count"]);
                        else jr.crt_count = 0;

                        if (reader["cnc_count"] != DBNull.Value) jr.cnc_count = Convert.ToInt32(reader["cnc_count"]);
                        else jr.cnc_count = 0;

                        if (reader["kp_when"] != DBNull.Value) jr.kp_when = Convert.ToString(reader["kp_when"]).Trim();
                        else jr.kp_when = "";

                        if (reader["kp_uname"] != DBNull.Value) jr.kp_name = Convert.ToString(reader["kp_uname"]).Trim();
                        else jr.kp_name = "";

                        if (reader["exc_path"] != DBNull.Value) jr.exc_path = Convert.ToString(reader["exc_path"]).Trim();
                        else jr.exc_path = "";

                        resJournal.Add(jr);
                    }
                }
                return resJournal;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetOrderList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }

        }
        
        /// <summary>
        /// Размещение недопоставок в лок.банках
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool NedopPlacement(Journal finder, out Returns ret)
        {
            #region Предварительные проверки
            //if (GlobalSettings.WorkOnlyWithCentralBank)
            //{
            //    ret = new Returns(false, "Формирование недопоставок невозможно, т.к. установлен режим работы с центральным банком данных", -1);
            //    return false;
            //}

            //if (finder.kp_nzp_user < 1)
            //{
            //    ret = new Returns(false, "Не определен пользователь", -1);
            //    return false;
            //}
            #endregion

            string sql = "";
            string sql_number = "";
            int cnc_count = 0;
            IDataReader reader = null;
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            string str_date = "mdy(" + Points.CalcMonth.month_.ToString() + ", 1," + Points.CalcMonth.year_.ToString() + ")";
            string dbsupg = "";

            ret = Utils.InitReturns();

            try
            {
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return false;

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;
                
                int nzp_User = finder.nzp_user;
                
                /*int nzp_User = db.GetLocalUser(conn_db, null, new Finder() { nzp_user = finder.kp_nzp_user }, out ret);
                if (!ret.result)
                {
                    string err_text = "Ошибка определения локального пользователя";
                    MonitorLog.WriteLog(err_text + " :" + ret.text, MonitorLog.typelog.Error, true);
                    ret.text = err_text;
                    return false;
                }*/

                #region Определить БД СУПГ (распределение недопоставок будет выполняться в Комплат)

                sql = "select * from " + Points.Pref + sKernelAliasRest + "s_baselist where idtype=6";


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    string err_text = "Ошибка при определении ссылки на БД СУПГ ";
                    MonitorLog.WriteLog(err_text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = err_text;
                    return false;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["dbname"] != DBNull.Value)
                        {
                            dbsupg = reader["dbname"].ToString().Trim();
                        }
                    }
                }

                if (dbsupg == "")
                {
                    string err_text = "В системе не определена ссылка на БД СУПГ!";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = err_text;
                    return false;
                }

                // Проверить доступ на БД СУПГ
                if (!TempTableInWebCashe(conn_db, dbsupg + tableDelimiter + "nedop_kvar"))
                {
                    string err_text = "Распределение недопоставок: Банк данных " + dbsupg + " недоступен";
                    MonitorLog.WriteLog(err_text, MonitorLog.typelog.Warn, true);
                    ret.text = "Распределение недопоставок: БД СУПГ недоступен";
                    return false;
                }


                #endregion


                # region Если не указана конкретная запись журнала, проверить наличие готовых для распределения данных, создать sql_number

                sql_number = " and nzp_jrn = " + finder.number.ToString();

                if ((finder.number == 0) || (finder.number == -1))
                {
                    sql = "select count(*) cnt from " + Points.Pref + sSupgAliasRest + "jrn_upg_nedop where status=1";

                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Не обнаружено данных о недопоставках для распределения" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        return false;
                    }

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if ((reader["cnt"] != DBNull.Value) && (reader["cnt"].ToString() == "0"))
                            {
                                ret.text = "Не обнаружено данных о недопоставках для распределения";
                                MonitorLog.WriteLog("Не обнаружено данных о недопоставках для распределения" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                ret.result = false;
                                return false;
                            }
                        }
                    }
                    sql_number = "";
                }
                #endregion

                List<_Point> prefixs = new List<_Point>();
                prefixs = Points.PointList;

                foreach (_Point items in prefixs)
                {
                    #region Цикл по лок.БД

                    if (items.nzp_wp == 99) continue;

                    // Проверить доступ на БД

                    if (!TempTableInWebCashe(conn_db, items.pref + sDataAliasRest + "nedop_kvar"))
                    {
                        string err_text = "Формирование недопоставок. Банк данных " + items.pref + " недоступен";
                        MonitorLog.WriteLog(err_text, MonitorLog.typelog.Warn, true);
                        ret.text = err_text;
                        return false;
                    }
                    //1. Удалить недопоставки 
                    sql = " delete from " + items.pref + sDataAliasRest + "nedop_kvar" +
                          " where month_calc= " + str_date +
                          " and is_actual in (14,12) and act_no in " +
                          "(select act_no from " + dbsupg + tableDelimiter + "nedop_kvar" +
                          " where month_calc= " + str_date + " and nzp_wp = " + items.nzp_wp + sql_number + ")";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        string err_text = "Ошибка при предварительном удалении недопоставок в локальной БД " + items.point;
                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                        ret.text = err_text;
                        return false;
                    }

                    #region Отметить недопоставки (лиц.счета), которые надо занести в группу лиц.счетов

                    // почистить поле для отметки mark1
                    sql = "UPDATE " + dbsupg + tableDelimiter + "nedop_kvar" +
                          " set mark=0, mark1=0 " +
                          " where month_calc = " + str_date + " and nzp_wp = " + items.nzp_wp.ToString();
                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        // Отметить недопоставки (лиц.счета), которые надо занести в группу перерасчета
                        sql = "UPDATE " + dbsupg + tableDelimiter + "nedop_kvar set mark2=0 " +
                              " where month_calc = " + str_date + " and nzp_wp = " + items.nzp_wp.ToString() + " and mark2<>0 " + sql_number;
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    if (ret.result)
                    {
                        sql = "UPDATE " + dbsupg + tableDelimiter + "nedop_kvar set mark2=1 " +
                            " where " +
                            " month_calc = " + str_date + " and nzp_wp = " + items.nzp_wp.ToString() + sql_number +
                            " and not exists " +
                            "( select nzp_kvar from " + items.pref + sDataAliasRest + "link_group " +
                            " where " +
                            " nzp_kvar = " + dbsupg + tableDelimiter + "nedop_kvar.nzp_kvar and " +
                            " nzp_group=(case when " + dbsupg + tableDelimiter + "nedop_kvar.is_actual=14 then 199 else 198 end)" +
                            ")";
                        ret = ExecSQL(conn_db, sql, true);
                    }

                    if (!ret.result)
                    {
                        string err_text = "Ошибка определения л.с. для добавления в группу л.с. в локальной БД " + items.point;
                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                        ret.text = err_text;
                        return false;
                    }
                    #endregion

                    // Занести недопоставки: Количество недопоставок

                    sql = "select min(dat_s) dat_s, max(dat_po) dat_po, count(*) cnt" +
                          " from " + Points.Pref + sSupgAliasRest + "nedop_kvar" +
                          " where nzp_jrn = " + finder.number + " and nzp_wp = " + items.nzp_wp.ToString();

                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка определения количества созданных недопоставок по банку" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        return false;
                    }

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (
                                ((reader["cnt"] != DBNull.Value) && (reader["cnt"].ToString() != "0")) &&
                                ((reader["dat_s"] != DBNull.Value) && (reader["dat_s"].ToString() != "")) &&
                                ((reader["dat_po"] != DBNull.Value) && (reader["dat_po"].ToString() != ""))
                                )
                            {

                                cnc_count = cnc_count + Convert.ToInt32(reader["cnt"]);
                                DateTime cicle_dat_s = Convert.ToDateTime(reader["dat_s"]);
                                DateTime cicle_dat_po = Convert.ToDateTime(reader["dat_po"]);

                                #region Занести недопоставки с разделением по месяцам
                                DateTime curmonth = System.Convert.ToDateTime(System.Convert.ToDateTime(cicle_dat_s).ToString("01.MM.yyyy"));
                                int nummark = 0;
                                while (curmonth < cicle_dat_po)
                                {
                                    nummark++;

                                    // Отметить записи для размещения в лок.БД
                                    string str_dat_s = "mdy(" + System.Convert.ToDateTime(curmonth).ToString("MM,dd,yyyy") + ")";
                                    DateTime nextmonth = curmonth.AddMonths(1);
                                    string str_dat_po = "mdy(" + System.Convert.ToDateTime(nextmonth).ToString("MM,dd,yyyy") + ")";
                                    string str_end_month = "mdy(" + System.Convert.ToDateTime(nextmonth.AddDays(-1)).ToString("MM,dd,yyyy") + ")";

                                    sql = "UPDATE " + dbsupg + tableDelimiter + "nedop_kvar set mark= " + nummark.ToString() + "," +
                                        " mark_s = (case when dat_s < " + str_dat_s + " then cast(" + str_dat_s + " as timestamp) else dat_s end), " +
                                        " mark_po = (case when dat_po > " + str_dat_po + " then cast(" + str_dat_po + " as timestamp) else dat_po end) " +
                                        " where month_calc = " + str_date + " and nzp_wp = " + items.nzp_wp.ToString() +
                                        " and dat_s < " + str_dat_po + " and dat_po > " + str_dat_s + sql_number;


                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result)
                                    {
                                        string err_text = "Ошибка выполнения формирования недопоставок в локальной БД : ";
                                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                                        ret.text = err_text + items.point;
                                        ret.result = false;
                                        return false;
                                    }

                                    #region Отметить недопоставки (лиц.счета), которые надо занести в группу must_calc
                                    // Отметить недопоставки (лиц.счета), которые надо занести в must_calc

                                    sql = "UPDATE " + dbsupg + tableDelimiter + "nedop_kvar" +
                                          " set mark1= " + nummark.ToString() +
                                          " where  month_calc = " + str_date + " and nzp_wp = " + items.nzp_wp.ToString() +
                                          sql_number +
                                          " and not exists " +
                                          "( select nzp_kvar from " + items.pref + sDataAliasRest + "must_calc " +
                                          " where " +
                                          " nzp_kvar = " + dbsupg + tableDelimiter + "nedop_kvar.nzp_kvar and " +
                                          " nzp_serv = " + dbsupg + tableDelimiter + "nedop_kvar.nzp_serv and " +
                                          " nzp_supp = " + dbsupg + tableDelimiter + "nedop_kvar.nzp_supp and " +
                                          " dat_s = " + dbsupg + tableDelimiter + "nedop_kvar.mark_s and " +
                                          " dat_po= " + dbsupg + tableDelimiter + "nedop_kvar.mark_po and " +
                                          " mark = " + nummark.ToString() +
                                          " and year_=" + Points.CalcMonth.year_.ToString() + " and month_=" + Points.CalcMonth.month_.ToString() +
                                          ")";

                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result)
                                    {
                                        string err_text = "Ошибка определения списка л.с. для перерасчета в локальной БД " + items.point;
                                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                                        ret.text = err_text;
                                        return false;
                                    }
                                    #endregion



                                    sql = "INSERT INTO " + items.pref + sDataAliasRest + "nedop_kvar " +
                                          "(nzp_kvar, act_no, nzp_serv, nzp_kind, tn, dat_s, dat_po, is_actual, dat_when, month_calc, comment, nzp_wp) " +
                                        " select nzp_kvar, act_no, nzp_serv, nzp_kind, tn, mark_s, mark_po, is_actual, now(), month_calc, comment, nzp_wp " +
                                        " from " + Points.Pref + sSupgAliasRest + "nedop_kvar where nzp_wp = " + items.nzp_wp.ToString() + 
                                        " and mark = " + nummark.ToString() +
                                    sql_number;


                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result)
                                    {
                                        string err_text = "Ошибка выполнения формирования недопоставок в локальной БД : ";
                                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                                        ret.text = err_text + items.point;
                                        ret.result = false;
                                        return false;
                                    }

                                    sql = "INSERT INTO " + items.pref + sDataAliasRest + "must_calc " +
                                          "(nzp_kvar, nzp_serv, nzp_supp, year_, month_, dat_s, dat_po, cnt_add) " +
                                        " select distinct nzp_kvar, nzp_serv, nzp_supp, " + Points.CalcMonth.year_.ToString() + ", " + Points.CalcMonth.month_.ToString() +
                                        ", " + str_dat_s + ", " + str_end_month + ", 10 " +
                                        " from " + Points.Pref + sSupgAliasRest + "nedop_kvar" +
                                          " where nzp_wp = " + items.nzp_wp.ToString() + " and mark1= " + nummark.ToString() +
                                    sql_number;


                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result)
                                    {
                                        string err_text = "Ошибка формирования списка перерасчета в локальной БД ";
                                        MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                                        ret.text = err_text + items.point;
                                        ret.result = false;
                                        return false;
                                    }
                                    // Следующий месяц
                                    curmonth = curmonth.AddMonths(1);
                                }
                                #endregion

                                // Добавить в справочник групп (на всякий случай)

                                sql = "delete from " + items.pref + sDataAliasRest + "s_group where nzp_group in (199, 198)";
                                ret = ExecSQL(conn_db, sql, false);
                                sql = "insert into " + items.pref + sDataAliasRest + "s_group (nzp_group, ngroup, txt1, txt2) " +
                                      " values (198, \'Недопоставки УПГ 5.0 (Заявки)\', null, null)";
                                ret = ExecSQL(conn_db, sql, false);
                                sql = "insert into " + items.pref + "_data.s_group (nzp_group, ngroup, txt1, txt2) " +
                                      " values (199, \'Недопоставки УПГ 5.0 (Акты о недопоставке)\', null, null)";
                                ret = ExecSQL(conn_db, sql, false);
                                sql = "INSERT INTO " + items.pref + sDataAliasRest + "link_group (nzp_group, nzp) " +
                                      " select distinct (case when is_actual=14 then 199 else 198 end), nzp_kvar " +
                                      " from " + Points.Pref + "_supg .nedop_kvar " +
                                      " where nzp_wp = " + items.nzp_wp.ToString() + " and mark2=1 " +
                                      sql_number;

                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                {
                                    string err_text = "Ошибка формирования списка лиц.счетов для группы лиц.счетов в локальной БД ";
                                    MonitorLog.WriteLog(err_text + ret.text, MonitorLog.typelog.Error, true);
                                    ret.text = err_text + items.point;
                                    ret.result = false;
                                    return false;
                                }
                            }

                        }
                    }

                    #endregion

                }


                sql = " update " + dbsupg + tableDelimiter + "jrn_upg_nedop" +
                      " set status=2, kp_when=" + sCurDate + ", kp_nzp_user=" + nzp_User + ", cnc_count = " + cnc_count.ToString();
                if (finder.number == 0)
                {
                    sql = sql + " where status = 1";
                }
                else
                {
                    sql = sql + " where no = " + finder.number;
                }


                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  NedopPlacement : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

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

                #endregion
            }
        }

    }
}
