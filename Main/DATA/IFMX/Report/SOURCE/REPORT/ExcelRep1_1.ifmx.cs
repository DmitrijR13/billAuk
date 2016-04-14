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
        public DataTable ProtCalcOdn2(Prm prm, out Returns ret, string Nzp_user)
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


            ExecSQL(conn_db, "drop table t_svodkoef", false);
            sql2.Remove(0, sql2.Length);
            sql2.Append(" create temp table t_svodkoef( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" litera char(20),");
            sql2.Append(" kf_hv Decimal(14,4),");
            sql2.Append(" kf_gv Decimal(14,4),");
            sql2.Append(" kf_gaz Decimal(14,4),");
            sql2.Append(" kf_el Decimal(14,4), ");
            sql2.Append(" pl_mop Decimal(14,4), ");
            sql2.Append(" pl_dom Decimal(14,4) ");
#if PG

            sql2.Append("  ) ");
#else

            sql2.Append("  ) with no log");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            ExecSQL(conn_db, "drop table t_svod", false);
            sql2.Remove(0, sql2.Length);
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" litera char(20),");
            sql2.Append(" kf_hv Decimal(14,4),");
            sql2.Append(" kf_hv_m Decimal(14,4),");
            sql2.Append(" kf_hv_h Decimal(14,4),");
            sql2.Append(" kf_gv Decimal(14,4),");
            sql2.Append(" kf_gv_m Decimal(14,4),");
            sql2.Append(" kf_gv_h Decimal(14,4),");

            sql2.Append(" kf_gaz Decimal(14,4),");
            sql2.Append(" kf_gaz_m Decimal(14,4),");
            sql2.Append(" kf_gaz_h Decimal(14,4),");

            sql2.Append(" date_calc Date,");
            sql2.Append(" kf_el Decimal(14,4), ");
            sql2.Append(" kf_el_m Decimal(14,4), ");
            sql2.Append(" kf_el_h Decimal(14,4), ");
            sql2.Append(" pl_mop Decimal(14,4), ");
            sql2.Append(" pl_dom Decimal(14,4) ");
#if PG

            sql2.Append("  ) ");
#else

            sql2.Append("  ) with no log");
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
                    string db_charge = pref + "_charge_" + (prm.year_ - 2000).ToString("00");

                    ExecRead(conn_db, out reader2, "drop table t_dom", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" Create unlogged table t_dom (nzp_dom integer, litera char(20),");
                    sql2.Append(" pl_dom Decimal(14,2), pl_mop Decimal(14,2))");
#else
                    sql2.Append(" Create temp table t_dom (nzp_dom integer, litera char(20),");
                    sql2.Append(" pl_dom Decimal(14,2), pl_mop Decimal(14,2)) with no log");
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

                    ExecRead(conn_db, out reader2, "create index ix_tmp_sd_011 on t_dom(nzp_dom)", true);
#if PG
                    ExecRead(conn_db, out reader2, "analyze t_dom", true);
#else
                    ExecRead(conn_db, out reader2, "update statistics for table t_dom", true);
#endif

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

                    #region Площадь дома
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_dom set pl_dom = (select max(val_prm::numeric) from " + pref + "_data.prm_2 a ");
                    sql2.Append(" where val_prm~E'^\\d[0-9, ]{19}' and nzp_prm=40 and a.nzp=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update t_dom set pl_dom = (select max(val_prm+0) from " + pref + "_data:prm_2 a ");
                    sql2.Append(" where nzp_prm=40 and a.nzp=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
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
                    //Для секционных домов добавляем площадь соседних сеций
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_dom set pl_dom = (select max(val_prm::numeric) from " + pref + "_data.prm_2 a, ");
                    sql2.Append(pref + "_data.link_dom_lit b, " + pref + "_data.link_dom_lit d ");
                    sql2.Append(" where val_prm~E'^\\d[0-9, ]{19}' and nzp_prm=40 and a.nzp=b.nzp_dom and  b.nzp_dom_base=d.nzp_dom_base ");
                    sql2.Append(" and d.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                    sql2.Append(" update t_dom set pl_dom = (select max(val_prm+0) from " + pref + "_data:prm_2 a, ");
                    sql2.Append(pref + "_data:link_dom_lit b, " + pref + "_data:link_dom_lit d ");
                    sql2.Append(" where nzp_prm=40 and a.nzp=b.nzp_dom and  b.nzp_dom_base=d.nzp_dom_base ");
                    sql2.Append(" and d.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=today and dat_po>=today)");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
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


                    #endregion

                    #region Площадь мест общего пользования дома
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_dom set pl_mop = (select max(val_prm::numeric) from " + pref + "_data.prm_2 a ");
                    sql2.Append(" where val_prm~E'^\\d[0-9, ]{19}' and nzp_prm=2049 and a.nzp=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update t_dom set pl_mop = (select max(val_prm+0) from " + pref + "_data:prm_2 a ");
                    sql2.Append(" where nzp_prm=2049 and a.nzp=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
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

                    //Для секционных домов добавляем площадь соседних сеций
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_dom set pl_mop = (select max(val_prm::numeric) from " + pref + "_data.prm_2 a, ");
                    sql2.Append(pref + "_data.link_dom_lit b, " + pref + "_data.link_dom_lit d ");
                    sql2.Append(" where val_prm~E'^\\d[0-9, ]{19}' and nzp_prm=2049 and a.nzp=b.nzp_dom and  b.nzp_dom_base=d.nzp_dom_base ");
                    sql2.Append(" and d.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                    sql2.Append(" update t_dom set pl_mop = (select max(val_prm+0) from " + pref + "_data:prm_2 a, ");
                    sql2.Append(pref + "_data:link_dom_lit b, " + pref + "_data:link_dom_lit d ");
                    sql2.Append(" where nzp_prm=2049 and a.nzp=b.nzp_dom and  b.nzp_dom_base=d.nzp_dom_base ");
                    sql2.Append(" and d.nzp_dom=t_dom.nzp_dom  ");
                    sql2.Append(" and is_actual=1  ");
                    sql2.Append(" and dat_s<=today and dat_po>=today)");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
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

                    #endregion

                    #region Коэффициенты коррекции

                    ExecSQL(conn_db, "drop table t_koef", false);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" select td.nzp_dom, litera, nzp_serv, sum(case when stek=1 and val1 >0 then val1 ");
                    sql2.Append(" when stek=2 and val1>0 then val1 ");
                    sql2.Append(" else 0 end) as dpu,  ");
                    sql2.Append(" sum(case when stek=3 then val1+val2+dlt_reval+dlt_real_charge else 0 end) as kv_rash, ");
                    sql2.Append(" sum(case when stek=3 then gil1 else 0 end) as gil_dom, ");
                    sql2.Append(" sum(case when stek=3 then squ1 else 0 end) as pl_dom, sum(0) as is_mop, ");
                    sql2.Append(" max(nzp_counter) as nzp_counter ");
#if PG
                    sql2.Append(" into unlogged t_koef from " + db_charge + ".counters_" + prm.month_.ToString("00") + " a,  t_dom td  ");
                    sql2.Append(" where a.nzp_dom=td.nzp_dom and nzp_type = 1 ");
                    sql2.Append(" and stek in (1,2,3)  group by 1,2,3 ");
#else
                    sql2.Append(" from " + db_charge + ":counters_" + prm.month_.ToString("00") + " a,  t_dom td  ");
                    sql2.Append(" where a.nzp_dom=td.nzp_dom and nzp_type = 1 ");
                    sql2.Append(" and stek in (1,2,3)  group by 1,2,3 into temp t_koef with no log");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }


                    //Проставляем счетчики МОП
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_koef set is_mop =1 , kv_rash =0 ");
                    sql2.Append(" where  0< (select count(*) ");
                    sql2.Append(" from " + db_charge + ".counters_" + prm.month_.ToString("00") + " a,");
                    sql2.Append(pref + "_data.prm_17 b");
                    sql2.Append(" where nzp_prm=2068 and a.nzp_counter=b.nzp ");
                    sql2.Append(" and t_koef.nzp_dom = a.nzp_dom and stek in (1,2)  ");
                    sql2.Append(" and is_actual=1 and b.dat_s<=current_date and b.dat_po>=current_date) ");
#else
                    sql2.Append(" update t_koef set (is_mop, kv_rash) = (1,0) ");
                    sql2.Append(" where  0< (select count(*) ");
                    sql2.Append(" from " + db_charge + ":counters_" + prm.month_.ToString("00") + " a,");
                    sql2.Append(pref + "_data:prm_17 b");
                    sql2.Append(" where nzp_prm=2068 and a.nzp_counter=b.nzp ");
                    sql2.Append(" and t_koef.nzp_dom = a.nzp_dom and stek in (1,2)  ");
                    sql2.Append(" and is_actual=1 and b.dat_s<=today and b.dat_po>=today) ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table t_realkoef", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_dom, litera, nzp_serv, max(case when coalesce(dpu,0) - coalesce(kv_rash,0) >0 then  ");
                    sql2.Append(" (coalesce(dpu,0) - coalesce(kv_rash,0))/pl_dom else 0 end) as kf_m ");
                    sql2.Append(" into unlogged t_realkoef from t_koef  ");
                    sql2.Append(" where pl_dom>0 and nzp_counter > 0 group by 1,2,3 ");
#else
                    sql2.Append(" select nzp_dom, litera, nzp_serv, max(case when nvl(dpu,0) - nvl(kv_rash,0) >0 then  ");
                    sql2.Append(" (nvl(dpu,0) - nvl(kv_rash,0))/pl_dom else 0 end) as kf_m ");
                    sql2.Append(" from t_koef  ");
                    sql2.Append(" where pl_dom>0 and nzp_counter > 0 group by 1,2,3 into temp t_realkoef with no log");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }



                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod (nzp_dom, litera, kf_hv, kf_hv_h, kf_gv,");
                    sql2.Append(" kf_gv_h, kf_el, kf_el_h, kf_gaz, kf_gaz_h)");
                    sql2.Append(" select   td.nzp_dom, litera,  ");
                    //sql2.Append(" max(case when nzp_serv = 6 and kf307>0 then kf307 else 0 end) as koef_hv, ");
                    sql2.Append(" max(0) as koef_hv, ");
                    sql2.Append(" min(case when nzp_serv = 6 and kf307<0 then kf307 else 0 end) as koef_hv_h, ");
                    //sql2.Append(" max(case when nzp_serv = 9 and kf307>0 then kf307 else 0 end) as koef_gv, ");
                    sql2.Append(" max(0) as koef_gv, ");
                    sql2.Append(" min(case when nzp_serv = 9 and kf307<0 then kf307 else 0 end) as koef_gv_h, ");
                    //sql2.Append(" max(case when nzp_serv = 25 and kf307>0 then kf307 else 0 end) as koef_el,  ");
                    sql2.Append(" max(0) as koef_el,  ");
                    sql2.Append(" min(case when nzp_serv = 25 and kf307<0 then kf307 else 0 end) as koef_el_h,  ");
                    sql2.Append(" max(case when nzp_serv = 10 and kf307>0 then kf307 else 0 end) as koef_gaz,  ");
                    sql2.Append(" min(case when nzp_serv = 10 and kf307<0 then kf307 else 0 end) as koef_gaz_h  ");
#if PG
                    sql2.Append(" from " + db_charge + ".counters_" + prm.month_.ToString("00") + " a,  t_dom td  ");
#else
                    sql2.Append(" from " + db_charge + ":counters_" + prm.month_.ToString("00") + " a,  t_dom td  ");
#endif
                    sql2.Append(" where a.nzp_dom=td.nzp_dom  ");
                    sql2.Append(" and stek = 3  group by 1,2");
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
                    sql2.Append(" insert into t_svod (nzp_dom, litera, kf_hv_m, kf_gv_m,");
                    sql2.Append(" kf_el_m, kf_gaz_m)");
                    sql2.Append(" select   nzp_dom, litera,  ");
                    sql2.Append(" sum(case when nzp_serv=6 then kf_m else 0 end),  ");
                    sql2.Append(" sum(case when nzp_serv=9 then kf_m else 0 end),  ");
                    sql2.Append(" sum(case when nzp_serv=25 then kf_m else 0 end),  ");
                    sql2.Append(" sum(case when nzp_serv=10 then kf_m else 0 end)  ");
                    sql2.Append(" from t_realkoef  ");
                    sql2.Append(" group by 1,2 ");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }

                    ExecSQL(conn_db, "drop table t_koef", true);
                    ExecSQL(conn_db, "drop table t_realkoef", true);

                    #endregion


                    #region коэффициент РДН
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svodkoef (nzp_dom,   kf_el, pl_dom, pl_mop)");                   
#if PG
                    sql2.Append(" select td.nzp_dom, value::numeric, pl_dom, pl_mop ");
                    sql2.Append(" from " + pref + "_data.prm_2 a, " + pref + "_kernel.res_values r, ");
                    sql2.Append(" t_dom td  ");
                    sql2.Append(" where a.nzp_prm=2050 and a.nzp=td.nzp_dom ");
                    sql2.Append(" and r.nzp_res=3010 and r.nzp_y=a.val_prm::int  ");
#else
                    sql2.Append(" select td.nzp_dom, value, pl_dom, pl_mop ");
                    sql2.Append(" from " + pref + "_data:prm_2 a, " + pref + "_kernel:res_values r, ");
                    sql2.Append(" t_dom td  ");
                    sql2.Append(" where a.nzp_prm=2050 and a.nzp=td.nzp_dom ");
                    sql2.Append(" and r.nzp_res=3010 and r.nzp_y=a.val_prm  ");
#endif
                    sql2.Append(" and is_actual<>100 and dat_s<='01." + prm.month_ + "." + prm.year_ + "' ");
                    sql2.Append(" and dat_po>='01." + prm.month_ + "." + prm.year_ + "' ");

                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql2.Remove(0, sql2.Length);
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svodkoef (nzp_dom,   kf_hv, kf_gv, pl_dom, pl_mop)");
#if PG
                    sql2.Append(" select td.nzp_dom,   max(case when nzp_x=1 then value::numeric else 0 end), ");
                    sql2.Append(" max(case when nzp_x=2 then value::numeric else 0 end), max(pl_dom), max(pl_mop) ");
                    sql2.Append(" from " + pref + "_kernel.res_values r, t_dom td  ");
#else
                    sql2.Append(" select td.nzp_dom,   max(case when nzp_x=1 then value+0 else 0 end), ");
                    sql2.Append(" max(case when nzp_x=2 then value+0 else 0 end), max(pl_dom), max(pl_mop) ");
                    sql2.Append(" from " + pref + "_kernel:res_values r, t_dom td  ");
#endif
                    sql2.Append(" where nzp_res = 9979");
                    sql2.Append(" group by 1");

                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql2.Remove(0, sql2.Length);
                        ret.result = false;
                        return null;
                    }

                    #endregion

                    #region Дата расчета
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod (nzp_dom, litera, date_calc)");
                    sql2.Append(" select td.nzp_dom,  litera, min(dat_calc) ");
#if PG
                    sql2.Append(" from " + db_charge + ".kvar_calc_" + prm.month_.ToString("00") + " a, ");
                    sql2.Append(Points.Pref + "_data.kvar k, t_dom td ");
#else
                    sql2.Append(" from " + db_charge + ":kvar_calc_" + prm.month_.ToString("00") + " a, ");
                    sql2.Append(Points.Pref + "_data:kvar k, t_dom td ");
#endif
                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar and k.nzp_dom=td.nzp_dom ");
                    sql2.Append(" group by 1,2 ");

                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }
                    #endregion
                    ExecRead(conn_db, out reader2, "drop table t_dom", true);


                }
            }
            reader.Close();


            ExecSQL(conn_db, "drop table ts1", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_dom, max(kf_el) as kf_el, max(kf_hv) as kf_hv, ");
            sql2.Append(" max(kf_gv) as kf_gv, max(kf_gaz) as kf_gaz ");
            sql2.Append(" into unlogged ts1 from t_svod group by 1 ");
#else
            sql2.Append("select nzp_dom, max(kf_el) as kf_el, max(kf_hv) as kf_hv,");
            sql2.Append("max(kf_gv) as kf_gv, max(kf_gaz) as kf_gaz ");
            sql2.Append("from t_svod group by 1 into temp ts1 with no log");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);


            ExecSQL(conn_db, "drop table t_adr", true);

            #region Проставляем нормативные коэффициенты расчитанные по дому
            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svodkoef set kf_el = ");
#if PG
            sql2.Append(" kf_el*(case when coalesce(pl_dom,0)>0.001 ");
            sql2.Append(" then coalesce(pl_mop,0)/(coalesce(pl_dom,0)-coalesce(pl_mop,0)) else 0 end) ");

#else
            sql2.Append(" kf_el*(case when nvl(pl_dom,0)>0.001 ");
            sql2.Append(" then nvl(pl_mop,0)/(nvl(pl_dom,0)-nvl(pl_mop,0)) else 0 end) ");
#endif
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svodkoef set kf_hv = ");
#if PG
            sql2.Append(" kf_hv*(case when coalesce(pl_dom,0)>0.001 ");
            sql2.Append(" then coalesce(pl_mop,0)/(coalesce(pl_dom,0)-coalesce(pl_mop,0)) else 0 end) ");
#else
            sql2.Append(" kf_hv*(case when nvl(pl_dom,0)>0.001 ");
            sql2.Append(" then nvl(pl_mop,0)/(nvl(pl_dom,0)-nvl(pl_mop,0)) else 0 end) ");
#endif
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svodkoef set kf_gv = ");
#if PG
            sql2.Append(" kf_gv*(case when coalesce(pl_dom,0)>0.001 ");
            sql2.Append(" then coalesce(pl_mop,0)/(coalesce(pl_dom,0)-coalesce(pl_mop,0)) else 0 end) ");
#else
            sql2.Append(" kf_gv*(case when nvl(pl_dom,0)>0.001 ");
            sql2.Append(" then nvl(pl_mop,0)/(nvl(pl_dom,0)-nvl(pl_mop,0)) else 0 end) ");
#endif
            ExecSQL(conn_db, sql2.ToString(), true);



            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_el = ");
            sql2.Append("(select max(kf_el) from t_svodkoef where t_svodkoef.nzp_dom=t_svod.nzp_dom ) ");
            sql2.Append("where kf_el is not null and kf_el = 0");
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_hv = ");
            sql2.Append("(select max(kf_hv) from t_svodkoef where t_svodkoef.nzp_dom=t_svod.nzp_dom ) ");
            sql2.Append("where kf_hv is not null and kf_hv  = 0");
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gv = ");
            sql2.Append("(select max(kf_gv) from t_svodkoef where t_svodkoef.nzp_dom=t_svod.nzp_dom ) ");
            sql2.Append("where kf_gv is not null and kf_gv = 0");
            ExecSQL(conn_db, sql2.ToString(), true);



            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_el_m = (select max(kf_el) ");
            sql2.Append(" from ts1 where t_svod.nzp_dom=ts1.nzp_dom) where kf_el_m = 0");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);


            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_hv_m = (select max(kf_hv) ");
            sql2.Append(" from ts1 where t_svod.nzp_dom=ts1.nzp_dom) where kf_hv_m = 0");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gv_m = (select max(kf_gv) ");
            sql2.Append(" from ts1 where t_svod.nzp_dom=ts1.nzp_dom) where kf_gv_m = 0");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gaz_m = (select max(kf_gaz) ");
            sql2.Append(" from ts1 where t_svod.nzp_dom=ts1.nzp_dom) where kf_gaz_m = 0");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);
            ExecSQL(conn_db, "drop table ts1", true);
            #endregion


            #region Вычисляем коэффициенты исходя из площади дома


            /*   sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_el_m = (select max(kf_el) ");
            sql2.Append(" from t_svodkoef where t_svod.nzp_dom=t_svodkoef.nzp_dom) where kf_el_m = 0");
            ExecSQL(conn_db, sql2.ToString(), true);


            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_hv_m = (select max(kf_hv) ");
            sql2.Append(" from t_svodkoef where t_svod.nzp_dom=t_svodkoef.nzp_dom) where kf_hv_m = 0");
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gv_m = (select max(kf_gv) ");
            sql2.Append(" from t_svodkoef where t_svod.nzp_dom=t_svodkoef.nzp_dom) where kf_gv_m = 0");
            ExecSQL(conn_db, sql2.ToString(), true);*/


            #endregion

            #region Обнуляем квадратнометровые с случае отрицательного коэффициента
            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_el_m = 0 where kf_el_h<-0.0001");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);
            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_hv_m = 0 where kf_hv_h<-0.0001");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gv_m = 0 where kf_gv_h<-0.0001");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append("update t_svod set kf_gaz_m = 0 where kf_gaz_h<-0.0001");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);
            #endregion

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select geu, area, ulica, idom, ndom , nkor,  litera, max(coalesce(kf_hv,0)) as kf_hv,");
            sql2.Append(" max(coalesce(kf_hv_m,0)) as kf_hv_m, min(coalesce(kf_hv_h,0)) as kf_hv_h, ");
            sql2.Append(" max(coalesce(kf_gv,0)) as kf_gv, max(coalesce(kf_gv_m,0)) as kf_gv_m, min(coalesce(kf_gv_h,0)) as kf_gv_h, ");
            sql2.Append(" max(coalesce(kf_el,0)) as kf_el, max(coalesce(kf_el_m,0)) as kf_el_m, ");
            sql2.Append(" min(coalesce(kf_el_h,0)) as kf_el_h, max(coalesce(kf_gaz_m,0)) as kf_gaz_m, ");
            sql2.Append(" min(coalesce(kf_gaz_h,0)) as kf_gaz_h, max(date_calc) as date_calc");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica su,");
            sql2.Append(Points.Pref + "_data.s_geu sg," + Points.Pref + "_data.s_area sa  ");
            sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul ");
            sql2.Append(" and d.nzp_geu=sg.nzp_geu and d.nzp_area=sa.nzp_area  ");
            sql2.Append(" group by 1,2,3,4,5,6,7 ");
            sql2.Append(" order by area, geu, ulica, idom, ndom, nkor, litera  ");
#else
            sql2.Append(" select geu, area, ulica, idom, ndom , nkor,  litera, max(nvl(kf_hv,0)) as kf_hv,");
            sql2.Append(" max(nvl(kf_hv_m,0)) as kf_hv_m, min(nvl(kf_hv_h,0)) as kf_hv_h, ");
            sql2.Append(" max(nvl(kf_gv,0)) as kf_gv, max(nvl(kf_gv_m,0)) as kf_gv_m, min(nvl(kf_gv_h,0)) as kf_gv_h, ");
            sql2.Append(" max(nvl(kf_el,0)) as kf_el, max(nvl(kf_el_m,0)) as kf_el_m, ");
            sql2.Append(" min(nvl(kf_el_h,0)) as kf_el_h, max(nvl(kf_gaz_m,0)) as kf_gaz_m, ");
            sql2.Append(" min(nvl(kf_gaz_h,0)) as kf_gaz_h, max(date_calc) as date_calc");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica su,");
            sql2.Append(Points.Pref + "_data:s_geu sg," + Points.Pref + "_data:s_area sa  ");
            sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul ");
            sql2.Append(" and d.nzp_geu=sg.nzp_geu and d.nzp_area=sa.nzp_area  ");
            sql2.Append(" group by 1,2,3,4,5,6,7 ");
            sql2.Append(" order by area, geu, ulica, idom, ndom, nkor, litera  ");

#endif
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
