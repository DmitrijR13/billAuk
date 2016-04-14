using FastReport;
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
        //1.3.1
        public List<ZvkFinder> GetMessageList(SupgFinder finder, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReport4_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            //List<_Point> points = Points.PointList;

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

                ExecSQL(con_db, "Drop table " + temp_table_name, false);

                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table " + temp_table_name + " (" +
                                          " nzp_zvk        integer, " +
                                          " zvk_date       timestamp, " +
                                          " nzp_kvar       integer, " +
                                          " address        varchar(255), " +
                                          " demand_name    char(50), " +
                                          " phone          char(50), " +
                                          " comment        text, " +
                                          " nzp_zk         integer, " +
                                          " order_date     timestamp, " +
                                          " nzp_res        integer, " +
                                          " nzp_dest       integer, " +
                                          " nzp_supp       integer, " +
                                          " fact_date      timestamp, " +
                                          " is_replicated  integer, " +
                                          " replno         integer, " +
                                          " replno_date    timestamp, " +
                                          " nzp_atts       integer, " +
                                          " comment_n      varchar(255) " +
                                            " )  "
                                         );
#else
 sql.Append(" create temp table " + temp_table_name + " (" +
                           " nzp_zvk        integer, " +
                           " zvk_date       datetime year to second, " +
                           " nzp_kvar       integer, " +
                           " address        varchar(255), " +
                           " demand_name    char(25), " +
                           " phone          char(25), " +
                           " comment        text, " +
                           " nzp_zk         integer, " +
                           " order_date     datetime year to minute, " +
                           " nzp_res        integer, " +
                           " nzp_dest       integer, " +
                           " nzp_supp       integer, " +
                           " fact_date      datetime year to minute, " +
                           " is_replicated  integer, " +
                           " replno         integer, " +
                           " replno_date    datetime year to minute, " +
                           " nzp_atts       integer, " +
                           " comment_n      varchar(255) " +
                             " ) with no log "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #region заполнение временной таблицы

                // выяснить, выполнялся ли запрос по нарядам-заказам (ExSelZk = true: выполнялся)
                bool ExSelZk = false;
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("select count(*) cnt from public. t" + finder.nzp_user + "_supg  where nzp_zk is not null");
#else
            sql.Append("select count(*) cnt from " + con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg  where nzp_zk is not null");
#endif
                if (ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    if (reader != null)
                    {
                        if (reader.Read())
                            if (reader["cnt"] != DBNull.Value) ExSelZk = (Convert.ToInt32(reader["cnt"]) > 0);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка выборки GetMessageList" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                        " insert  into " + temp_table_name +
                        " ( " +
                        " nzp_zvk, " +
                        " zvk_date, " +
                        " nzp_kvar,  " +
                        " address, " +
                        " demand_name, " +
                        " phone,  " +
                        " comment, " +
                        " nzp_zk,  " +
                        " order_date, " +
                        " nzp_res, " +
                        " nzp_dest, " +
                        " nzp_supp, " +
                        " fact_date, " +
                        " is_replicated, " +
                        " replno, " +
                        " replno_date, " +
                        " nzp_atts, " +
                        " comment_n" +
                        ") " +
                        " select " +
                        " t.nzp_zvk, " +
                        " z.zvk_date, " +
                        " z.nzp_kvar, " +
                        " t.adr address, " +
                        " z.demand_name, " +
                        " replace(z.phone,'-','') phone, " +
                        " z.comment, " +
                        " zk.nzp_zk, " +
                        " zk.order_date, " +
                        " zk.nzp_res," +
                        " zk.nzp_dest, " +
                        " zk.nzp_supp, " +
                        " zk.fact_date, " +
                        " zk.repeated, " +
                        " zk.replno, " +
                        " zk1.order_date, " +
                        " zk.nzp_atts, " +
                        " zk.comment_n " +
                        " from public. t" + finder.nzp_user + "_supg t, " +
                        Points.Pref + "_supg. zvk z," +
                        Points.Pref + "_supg. zakaz zk " +
                        " left outer join " + Points.Pref + "_supg. zakaz zk1 on zk.replno=zk1.nzp_zk  ");
                if (!ExSelZk)
                {
                    sql.Append(
                    " where " +
                    " t.nzp_zvk=z.nzp_zvk " +
                    " and z.nzp_zvk=zk.nzp_zvk ");
                }
                else
                {
                    sql.Append(
                    " where " +
                    " t.nzp_zvk=z.nzp_zvk " +
                    " and t.nzp_zk=zk.nzp_zk ");
                }
#else
            sql.Append(
                    " insert  into " + temp_table_name +
                    " ( " +
                    " nzp_zvk, " +
                    " zvk_date, " +
                    " nzp_kvar,  " +
                    " address, " +
                    " demand_name, " +
                    " phone,  " +
                    " comment, " +
                    " nzp_zk,  " +
                    " order_date, " +
                    " nzp_res, " +
                    " nzp_dest, " +
                    " nzp_supp, " +
                    " fact_date, " +
                    " is_replicated, " +
                    " replno, " +
                    " replno_date, " +
                    " nzp_atts, " +
                    " comment_n" +
                    ") " +
                    " select " +
                    " t.nzp_zvk, " +
                    " z.zvk_date, " +
                    " z.nzp_kvar, " +
                    " t.adr address, " +
                    " z.demand_name, " +
                    " replace(z.phone,'-','') phone, " +
                    " z.comment, " +
                    " zk.nzp_zk, " +
                    " zk.order_date, " +
                    " zk.nzp_res," +
                    " zk.nzp_dest, " +
                    " zk.nzp_supp, " +
                    " zk.fact_date, " +
                    " zk.repeated, " +
                    " zk.replno, " +
                    " zk1.order_date, " +
                    " zk.nzp_atts, " +
                    " zk.comment_n " +
                    " from " +
                    con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                    Points.Pref + "_supg: zvk z," +
                    Points.Pref + "_supg: zakaz zk, " +
                    "outer(" + Points.Pref + "_supg: zakaz zk1) ");
                if (!ExSelZk)
                {
                    sql.Append(
                    " where " +
                    " t.nzp_zvk=z.nzp_zvk " +
                    " and z.nzp_zvk=zk.nzp_zvk " +
                    " and zk.replno=zk1.nzp_zk ");
                }
                else
                {
                    sql.Append(
                    " where " +
                    " t.nzp_zvk=z.nzp_zvk " +
                    " and t.nzp_zk=zk.nzp_zk " +
                    " and zk.replno=zk1.nzp_zk ");
                }
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion


                #region выборка данных для отчета

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                   " select " +
                                   " t.nzp_zvk, " +
                                   " t.zvk_date, " +
                                   " t.address, " +
                                   " replace(t.phone,'-','') phone, " +
                                   " t.demand_name, " +
                                   " t.comment, " +
                                   " case when (v.service is null or v.service='-') then ds.dest_name else v.service end service, " +
                                   " t.nzp_zk, " +
                                   " t.order_date, " +
                                   " rs.res_name result, " +
                                   " t.fact_date, " +
                                   " t.nzp_res, " +
                                   " t.comment_n, " +
                                   " t.nzp_atts, " +
                                   " a.atts_name, " +
                                   " sp.name_supp, " +
                                   " t.replno, " +
                                   " t.replno_date, " +
                                   " t.is_replicated " +
                                   " FROM " +
	                               Points.Pref + "_supg.s_attestation A, " +
	                               Points.Pref + "_supg.s_result rs, " +
	                               Points.Pref + "_supg.s_dest ds  " +
	                               " LEFT OUTER JOIN " + Points.Pref + "_kernel.services v ON ds.nzp_serv = v.nzp_serv, " +
                                   temp_table_name + " t  " +
	                               " LEFT OUTER JOIN " + Points.Pref + "_kernel.supplier sp ON t.nzp_supp = sp.nzp_supp  " +
                                   " WHERE " +
	                               " t.nzp_dest = ds.nzp_dest " +
                                   " AND t.nzp_res = rs.nzp_res " +
                                   " AND t.nzp_atts = A .nzp_atts " +
                                   " ORDER BY " +
	                               " nzp_kvar, " +
	                               " nzp_zvk, " +
	                               " service, " +
                                   " nzp_zk ");
#else
                sql.Append(
                                   " select " +
                                   " t.nzp_zvk, " +
                                   " t.zvk_date, " +
                                   " t.address, " +
                                   " replace(t.phone,'-','') phone, " +
                                   " t.demand_name, " +
                                   " t.comment, " +
                                   " case when (v.service is null or v.service='-') then ds.dest_name else v.service end service, " +
                                   " t.nzp_zk, " +
                                   " t.order_date, " +
                                   " rs.res_name result, " +
                                   " t.fact_date, " +
                                   " t.nzp_res, " +
                                   " t.comment_n, " +
                                   " t.nzp_atts, " +
                                   " a.atts_name, " +
                                   " sp.name_supp, " +
                                   " t.replno, " +
                                   " t.replno_date, " +
                                   " t.is_replicated " +
                                   " from " +
                                   temp_table_name + " t, " +
                                   Points.Pref + "_supg: s_attestation a, " +
                                   " outer(" + Points.Pref + "_kernel:services v)," +
                                   " outer(" + Points.Pref + "_kernel:supplier sp)," +
                                   Points.Pref + "_supg:s_dest ds, " + Points.Pref + "_supg:s_result rs " +
                                   " where " +
                                   " t.nzp_dest=ds.nzp_dest " +
                                   " and ds.nzp_serv=v.nzp_serv " +
                                   " and t.nzp_supp=sp.nzp_supp " +
                                   " and t.nzp_res=rs.nzp_res " +
                                   " and t.nzp_atts=a.nzp_atts " +
                                   " order by nzp_kvar,nzp_zvk,service,nzp_zk");
#endif

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #region запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            ZvkFinder item = new ZvkFinder();
                            if (reader["nzp_zvk"] != DBNull.Value) item.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                            if (reader["zvk_date"] != DBNull.Value) item.zvk_date = Convert.ToString(reader["zvk_date"]).Trim();
                            if (reader["address"] != DBNull.Value) item.adr = Convert.ToString(reader["address"]).Trim();
                            if (reader["phone"] != DBNull.Value) item.phone = Convert.ToString(reader["phone"]).Trim();
                            if (reader["demand_name"] != DBNull.Value) item.fio = Convert.ToString(reader["demand_name"]).Trim();
                            if (reader["comment"] != DBNull.Value) item.comment = Convert.ToString(reader["comment"]).Trim();
                            if (reader["service"] != DBNull.Value) item.service = Convert.ToString(reader["service"]).Trim();
                            if (reader["nzp_zk"] != DBNull.Value) item.nzp_zk = Convert.ToString(reader["nzp_zk"]).Trim();
                            if (reader["order_date"] != DBNull.Value) item.order_date = Convert.ToString(reader["order_date"]).Trim();
                            if (reader["result"] != DBNull.Value) item.result_comment = Convert.ToString(reader["result"]).Trim();
                            if (reader["fact_date"] != DBNull.Value) item.fact_date = Convert.ToString(reader["fact_date"]).Trim();
                            if (reader["comment_n"] != DBNull.Value) item.n_comment = Convert.ToString(reader["comment_n"]).Trim();
                            if (reader["nzp_atts"] != DBNull.Value) item.nzp_atts = Convert.ToString(reader["nzp_atts"]).Trim();
                            if (reader["atts_name"] != DBNull.Value) item.atts_name = Convert.ToString(reader["atts_name"]).Trim();
                            if (reader["name_supp"] != DBNull.Value) item.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                            if (reader["replno"] != DBNull.Value) item.replno = Convert.ToString(reader["replno"]).Trim();
                            if (reader["replno_date"] != DBNull.Value) item.replno_date = Convert.ToString(reader["replno_date"]).Trim();
                            if (reader["is_replicated"] != DBNull.Value) item.replicated = Convert.ToString(reader["is_replicated"]).Trim();
                            if (reader["nzp_res"] != DBNull.Value) item.nzp_res = Convert.ToString(reader["nzp_res"]).Trim();
                            res.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetJoborderPeriodOutstand " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                return res;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJoborderPeriodOutstand : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);

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

        //2.4
        public List<ZvkFinder> GetCountOrderReadres(SupgFinder finder, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReport5_" + finder.nzp_user;
            string temp_table_name2 = "tmpCross_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            List<_Point> points = Points.PointList;

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

                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
                ExecSQL(con_db, " Drop table " + temp_table_name2 + "; ", false);


                #endregion

                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table " + temp_table_name +
                                           " (" +
                                           " nzp_slug   integer, " +
                                           " nzp_geu    integer, " +
                                           " cnt        integer " +
                                           " ) "
                                         );
#else
                sql.Append(" create temp table " + temp_table_name +
                                           " (" +
                                           " nzp_slug   integer, " +
                                           " nzp_geu    integer, " +
                                           " cnt        integer " +
                                           " ) with no log "
                                         );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                //foreach (_Point point in points)
                //{
                #region заполнение временной таблицы

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(
                                            " insert  into " + temp_table_name +
                                            " ( " +
                                            " nzp_slug, " +
                                            " nzp_geu, " +
                                            " cnt " +
                                            ") " +
                                            " select " +
                                            " r.nzp_slug, " +
                                            " k.nzp_geu, " +
                                            " count(*) cnt " +
                                            " from " +
                                            Points.Pref + "_supg. readdress r, " +
                                            Points.Pref + "_supg. zvk z " +
                                            " LEFT OUTER JOIN " + Points.Pref + "_data. kvar k ON z.nzp_kvar = K .nzp_kvar " +
                                            " where " +
                                            " z.nzp_zvk=r.nzp_zvk " +
                                            " and z.nzp_zvk in (select nzp_zvk from public. t" + finder.nzp_user + "_supg) " +
                                            " group by 1,2"
                                                    );
#else
                    sql.Append(
                                                " insert  into " + temp_table_name +
                                                " ( " +
                                                " nzp_slug, " +
                                                " nzp_geu, " +
                                                " cnt " +
                                                ") " +
                                                " select " +
                                                " r.nzp_slug, " +
                                                " k.nzp_geu, " +
                                                " count(*) cnt " +
                                                " from " +
                                                Points.Pref + "_supg: zvk z," +
                                                Points.Pref + "_supg: readdress r, " +
                                                " outer(" + Points.Pref + "_data: kvar k) " +
                                                " where " +
                                                " z.nzp_zvk=r.nzp_zvk " +
                                                " and z.nzp_kvar=k.nzp_kvar " +
                                                " and z.nzp_zvk in (select nzp_zvk from " + con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg) " +
                                                " group by 1,2"
                                                        );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion
                //}

                #region итоговая таблица

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                   " select " +
                                   " t.nzp_slug, " +
                                   " t.nzp_geu, " +
                                   " t.cnt " +
                                   " into unlogged " + temp_table_name2 +
                                   " from " + temp_table_name + " t " +
                                   " union " +
                                   " select distinct t.nzp_slug, " +
                                   " t1.nzp_geu, " +
                                   " 0 " +
                                   " from " + temp_table_name + " t, " +
                                   temp_table_name + " t1 " 
                                   );
#else
                sql.Append(
                                   " select " +
                                   " t.nzp_slug, " +
                                   " t.nzp_geu, " +
                                   " t.cnt " +
                                   " from " + temp_table_name + " t " +
                                   " union " +
                                   " select unique t.nzp_slug, " +
                                   " t1.nzp_geu, " +
                                   " 0 " +
                                   " from " + temp_table_name + " t, " +
                                   temp_table_name + " t1 " +
                                   " into temp " + temp_table_name2 + " with no log "
                                           );
#endif
                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #endregion

                #region выгрузка данных для отчета

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(
                                    " select " +
                                    " t.nzp_slug, " +
                                    " s.slug_name, " +
                                    " t.nzp_geu, " +
                                    " g.geu, " +
                                    " sum(t.cnt) cnt " +
                                    " from " +
                                    Points.Pref + "_supg.s_slug s, " +
                                    temp_table_name2 + " t " +
                                    " LEFT OUTER JOIN " + Points.Pref + "_data.s_geu g ON t.nzp_geu = g.nzp_geu" +
                                    " where " +
                                    " t.nzp_geu=g.nzp_geu " +
                                    " group by 1,2,3,4 " +
                                    " order by 2,4"
                                            );
#else
                sql.Append(
                                    " select " +
                                    " t.nzp_slug, " +
                                    " s.slug_name, " +
                                    " t.nzp_geu, " +
                                    " g.geu, " +
                                    " sum(t.cnt) cnt " +
                                    " from " +
                                    temp_table_name2 + " t, " +
                                    Points.Pref + "_supg:s_slug s, " +
                                    " outer(" + Points.Pref + "_data:s_geu g) " +
                                    " where " +
                                    " t.nzp_geu=g.nzp_geu and t.nzp_slug=s.nzp_slug " +
                                    " group by 1,2,3,4 " +
                                    " order by 2,4"
                                            );
#endif

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #endregion

                #region запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            ZvkFinder item = new ZvkFinder();
                            if (reader["nzp_slug"] != DBNull.Value) item.nzp_slug = Convert.ToString(reader["nzp_slug"]);
                            if (reader["slug_name"] != DBNull.Value) item.slug_name = Convert.ToString(reader["slug_name"]).Trim();
                            if (reader["nzp_geu"] != DBNull.Value) item.nzp_geu = Convert.ToString(reader["nzp_geu"]);
                            if (reader["geu"] != DBNull.Value) item.geu = Convert.ToString(reader["geu"]).Trim();
                            if (reader["cnt"] != DBNull.Value) item.cnt = Convert.ToString(reader["cnt"]).Trim();

                            res.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetJoborderPeriodOutstand " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJoborderPeriodOutstand : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
                ExecSQL(con_db, " Drop table " + temp_table_name2 + "; ", false);

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


        //формирование гистограммы
        public Returns GetSupgStatistics(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();

            #region Создание источника данных для таблицы отчета
            DataTable table = new DataTable();


            //получение данных
            table = GetOrderStat(finder, out ret);

            DataSet FDataSet = new DataSet();
            FDataSet.Tables.Add(table);
            #endregion

            Report report = new Report();
            report.Load(@"template/diagram.frx");
            report.RegisterData(FDataSet);
            report.GetDataSource("Q_master").Enabled = true;
            bool a = report.Prepare();

            string destinationFilename = "";

            try
            {
                destinationFilename = Constants.ExcelDir + finder.nzp_user + "_" + "diagram.fpx";
                report.SavePrepared(destinationFilename);
                ret.text = destinationFilename;
                return ret;
            }
            catch (Exception ex)
            {
                ret.text = "";
                MonitorLog.WriteLog("Ошибка формирования диаграммы \"Аналитика\" " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            return ret;
        }

        //данные для гистограммы
        public DataTable GetOrderStat(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable table1 = new DataTable();
            DataTable table2 = new DataTable();
            table2.TableName = "Q_master";
            table2.Columns.Add("service", typeof(string));
            table2.Columns.Add("value", typeof(Int32));
            table2.Columns.Add("type", typeof(string));
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReportGis_" + finder.nzp_user;
            string temp_table_name2 = "tmpCrossGis_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            List<_Point> points = Points.PointList;

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

                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
                ExecSQL(con_db, " Drop table " + temp_table_name2 + "; ", false);


                #endregion

                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table " + temp_table_name +
                                           " (" +
                                           " nzp   integer, " +
                                           " type_value char(20)," +
                                           " cnt integer " +
                                           " ) "
                                         );
#else
                sql.Append(" create temp table " + temp_table_name +
                                           " (" +
                                           " nzp   integer, " +
                                           " type_value char(20)," +
                                           " cnt integer " +
                                           " ) with no log "
                                         );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                //foreach (_Point point in points)
                //{
                #region заполнение временной таблицы

                sql.Remove(0, sql.Length);

                string SelGroupFld = "0";

                if (finder.group_fld == "nzp_serv") SelGroupFld = "nzp_dest";
                if (finder.group_fld == "nzp_supp") SelGroupFld = "nzp_supp";
                Dictionary<string, string> curCntF = new Dictionary<string, string>();
                curCntF.Add("Выдано", "sum(case when _date >= \'" + finder.ps + "\' then cnt_crt else 0 end)");
                curCntF.Add("Выполнено", "sum(case when _date >= \'" + finder.ps + "\' then cnt_fact else 0 end)");
                curCntF.Add("Отменено", "sum(case when _date >= \'" + finder.ps + "\' then cnt_otm else 0 end)");
                curCntF.Add("Плановый ремонт", "sum(case when _date >= \'" + finder.ps + "\' then cnt_plan else 0 end)");
                curCntF.Add("Не выполнено", "sum(cnt_beg + cnt_crt - cnt_otm - cnt_fact - cnt_plan)");

                foreach (KeyValuePair<string, string> item in curCntF)
                {
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(
                                                   " insert  into " + temp_table_name +
                                                   " ( nzp, type_value, cnt )" +
                                                   " select " +
                                                   SelGroupFld + ", \'" + item.Key + "\', " + item.Value +
                                                   " from " + Points.Pref + "_supg. upg_stat " +
                                                   " where year(_date)=year( \'" + finder.ps + "\') " +
                        //" and nzp_dest=20 "+
                                                   " group by 1,2 "
                                                           );
#else
                        sql.Append(
                                                       " insert  into " + temp_table_name +
                                                       " ( nzp, type_value, cnt )" +
                                                       " select " +
                                                       SelGroupFld + ", \'" + item.Key + "\', " + item.Value +
                                                       " from " + Points.Pref + "_supg: upg_stat " +
                                                       " where year(_date)=year( \'" + finder.ps + "\') " +
                            //" and nzp_dest=20 "+
                                                       " group by 1,2 "
                                                               );
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                }

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion
                //}

                #region итоговая таблица

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(
                                   " select " +
                                   " nzp, " +
                                   " type_value, " +
                                   " sum(cnt) cnt" +    // cnt_crt  выдано
                                   " into unlogged " + temp_table_name2 + " " +
                                   " from " + temp_table_name +
                                   " group by 1,2 "
                                           );
#else
                sql.Append(
                                   " select " +
                                   " nzp, " +
                                   " type_value, " +
                                   " sum(cnt) cnt" +    // cnt_crt  выдано
                                   " from " + temp_table_name +
                                   " group by 1,2 " +
                                   " into temp " + temp_table_name2 + " with no log "
                                           );
#endif
                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #endregion

                #region выгрузка данных для отчета

                string sqlText = "";

#if PG
                if (finder.group_fld == "")
                {
                    sqlText = "select \' Всего \' gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t " +
                              " group by 1,3";
                }
                if (finder.group_fld == "nzp_serv")
                {
                    sqlText = "select s.service gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t, " +
                              "kama_supg.s_dest d, " +
                              Points.Pref + "_kernel.services s " +
                              " where t.nzp = d.nzp_dest and d.nzp_serv = s.nzp_serv" +
                               " group by 1,3";
                }
                if (finder.group_fld == "nzp_supp")
                {
                    sqlText = "select s.name_supp gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t, " +
                              Points.Pref + "_kernel.supplier s " +
                              " where t.nzp = s.nzp_supp" +
                              " group by 1,3";
                }
#else
  if (finder.group_fld == "")
                {
                    sqlText = "select \' Всего \' gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t " +
                              " group by 1,3";
                }
                if (finder.group_fld == "nzp_serv")
                {
                    sqlText = "select s.service gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t, " +
                              "kama_supg:s_dest d, " +
                              Points.Pref + "_kernel:services s " +
                              " where t.nzp = d.nzp_dest and d.nzp_serv = s.nzp_serv" +
                               " group by 1,3";
                }
                if (finder.group_fld == "nzp_supp")
                {
                    sqlText = "select s.name_supp gr_name, sum(cnt), type_value " +
                              " from " + temp_table_name2 + " t, " +
                              Points.Pref + "_kernel:supplier s " +
                              " where t.nzp = s.nzp_supp" +
                              " group by 1,3";
                }
#endif

                sql.Remove(0, sql.Length);
                sql.Append(sqlText);

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #endregion

                #region запись выгрузки

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                try
                {
                    if (reader != null)
                    {
                        table1.Load(reader, LoadOption.OverwriteChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetOrderStat " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                if (table1.Columns.Count > 0)
                {
                    foreach (DataRow row in table1.Rows)
                    {
                        DataRow new_row = table2.NewRow();
                        new_row["service"] = row[0].ToString().Trim();
                        new_row["value"] = Convert.ToInt32(row[1].ToString().Trim());
                        new_row["type"] = row[2].ToString().Trim();
                        table2.Rows.Add(new_row);
                    }
                }

                return table2;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetOrderStat : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
                ExecSQL(con_db, " Drop table " + temp_table_name2 + "; ", false);

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
    }
}
