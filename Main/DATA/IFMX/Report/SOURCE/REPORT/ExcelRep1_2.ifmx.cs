using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep : ExcelRepClient
    {
        public DataTable ProtCalcOdn(Prm prm, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;

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
            StringBuilder sql2 = new StringBuilder();
#if PG
            string tXX_spls = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string pref = "";

            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();

            ExecRead(conn_db, out reader2, "drop table t_adr", false);
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_dom,nzp_kvar, nzp_geu, nzp_area, pref into unlogged t_adr from " + tXX_spls);
#else
            sql2.Append(" select nzp_dom,nzp_kvar, nzp_geu, nzp_area, pref from " + tXX_spls);
            sql2.Append(" into temp t_adr with no log");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            ExecRead(conn_db, out reader2, "drop table t_svod", false);
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" geu char(20),");
            sql2.Append(" area char(50),");
            sql2.Append(" ulica char(50),");
            sql2.Append(" ndom char(10),");
            sql2.Append(" nkor char(10),");
            sql2.Append(" idom integer,");
            sql2.Append(" litera char(20),");
            sql2.Append(" kf_hv Decimal(14,4),");
            sql2.Append(" kf_gv Decimal(14,4),");
            sql2.Append(" kf_el Decimal(14,4)) ");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" geu char(20),");
            sql2.Append(" area char(50),");
            sql2.Append(" ulica char(50),");
            sql2.Append(" ndom char(10),");
            sql2.Append(" nkor char(10),");
            sql2.Append(" idom integer,");
            sql2.Append(" litera char(20),");
            sql2.Append(" kf_hv Decimal(14,4),");
            sql2.Append(" kf_gv Decimal(14,4),");
            sql2.Append(" kf_el Decimal(14,4)) with no log");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  t_adr group by 1");

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }
            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();


                    ExecRead(conn_db, out reader2, "drop table t_dom", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" Create unlogged table t_dom (nzp_dom integer, litera char(20))");
#else
                    sql2.Append(" Create temp table t_dom (nzp_dom integer, litera char(20)) with no log");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_dom (nzp_dom) select nzp_dom from t_adr where pref='" + pref + "' group by 1");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_dom set litera = (select max(val_prm) from " + pref + "_data.prm_1 a, ");
                    sql2.Append(pref + "_data.kvar k  where nzp_prm=2002 and k.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1 and k.nzp_kvar=a.nzp ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update t_dom set litera = (select max(val_prm) from " + pref + "_data:prm_1 a, ");
                    sql2.Append(pref + "_data:kvar k  where nzp_prm=2002 and k.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1 and k.nzp_kvar=a.nzp ");
                    sql2.Append(" and dat_s<=today and dat_po>=today)");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }



                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod (geu, area, ulica, idom, ndom , nkor,  litera, kf_hv, kf_gv, kf_el)");
                    sql2.Append(" select   geu, area, ulica, idom, ndom, nkor, litera,  ");
                    sql2.Append(" max(case when nzp_serv = 6 then kf307 end) as koef_hv, ");
                    sql2.Append(" max(case when nzp_serv = 9 then kf307 end) as koef_gv, ");
                    sql2.Append(" max(case when nzp_serv = 25 then kf307 end) as koef_el  ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".counters_" +
                        prm.month_.ToString("00") + " a, " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica su,");
                    sql2.Append(Points.Pref + "_data.s_geu sg," + Points.Pref + "_data.s_area sa, t_dom td  ");
                    sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul and a.nzp_dom=td.nzp_dom  ");
                    sql2.Append(" and d.nzp_area=sa.nzp_area and d.nzp_geu=sg.nzp_geu  ");
                    sql2.Append(" and stek = 3  group by 1,2,3,4,5,6,7");
#else
                    sql2.Append(" insert into t_svod (geu, area, ulica, idom, ndom , nkor,  litera, kf_hv, kf_gv, kf_el)");
                    sql2.Append(" select   geu, area, ulica, idom, ndom, nkor, litera,  ");
                    sql2.Append(" max(case when nzp_serv = 6 then kf307 end) as koef_hv, ");
                    sql2.Append(" max(case when nzp_serv = 9 then kf307 end) as koef_gv, ");
                    sql2.Append(" max(case when nzp_serv = 25 then kf307 end) as koef_el  ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":counters_" +
                        prm.month_.ToString("00") + " a, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica su,");
                    sql2.Append(Points.Pref + "_data:s_geu sg," + Points.Pref + "_data:s_area sa, t_dom td  ");
                    sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul and a.nzp_dom=td.nzp_dom  ");
                    sql2.Append(" and d.nzp_area=sa.nzp_area and d.nzp_geu=sg.nzp_geu  ");
                    sql2.Append(" and stek = 3  group by 1,2,3,4,5,6,7");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }

                    ExecRead(conn_db, out reader2, "drop table t_dom", true);



                }
            }
            reader.Close();
            sql2.Remove(0, sql2.Length);
            sql2.Append("drop table t_adr");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" select geu, area, ulica, idom, ndom , nkor,  litera, kf_hv, kf_gv, kf_el ");
            sql2.Append(" from t_svod a ");
            sql2.Append(" order by area, geu, ulica, idom, ndom, nkor, litera  ");
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return null;
            }


            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            if (reader2 != null)
            {
                try
                {

                    LocalTable.Load(reader2, LoadOption.OverwriteChanges);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_web.Close();
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
            }
            if (reader2 != null) reader2.Close();
            sql2.Remove(0, sql2.Length);
            sql2.Append("drop table t_svod");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            conn_db.Close();




            reader.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }
    }
}
