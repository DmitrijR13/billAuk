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
        public DataTable GetSpravSoderg7(Prm prm, out Returns ret, string Nzp_user)
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

            ExecRead(conn_db, out reader2, "drop table t_svod", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" nzp_kvar integer,");
            sql2.Append(" nzp_geu integer,");
            sql2.Append(" name_frm char(100),");
            sql2.Append(" nzp_measure integer, ");
            sql2.Append(" count_gil integer,");
            sql2.Append(" rsum_tarif numeric(14,2), ");
            sql2.Append(" tarif numeric(14,3), ");
            sql2.Append(" c_calc numeric(14,4), ");
            sql2.Append(" sum_nedop numeric(14,2), ");
            sql2.Append(" reval_k numeric(14,2),");
            sql2.Append(" reval_d numeric(14,2), ");
            sql2.Append(" c_nedop numeric(14,4),");
            sql2.Append(" c_reval_k numeric(14,4),");
            sql2.Append(" c_reval_d numeric(14,4), ");
            sql2.Append(" pl_kvar numeric(14,2), ");
            sql2.Append(" sum_charge numeric(14,2))");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" nzp_kvar integer,");
            sql2.Append(" nzp_geu integer,");
            sql2.Append(" name_frm char(100),");
            sql2.Append(" nzp_measure integer, ");
            sql2.Append(" count_gil integer,");
            sql2.Append(" rsum_tarif Decimal(14,2), ");
            sql2.Append(" tarif Decimal(14,3), ");
            sql2.Append(" c_calc Decimal(14,4), ");
            sql2.Append(" sum_nedop Decimal(14,2), ");
            sql2.Append(" reval_k Decimal(14,2),");
            sql2.Append(" reval_d Decimal(14,2), ");
            sql2.Append(" c_nedop Decimal(14,4),");
            sql2.Append(" c_reval_k Decimal(14,4),");
            sql2.Append(" c_reval_d Decimal(14,4), ");
            sql2.Append(" pl_kvar Decimal(14,2), ");
            sql2.Append(" sum_charge Decimal(14,2)) with no log");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecRead(conn_db, out reader2, "drop table sel_kvar_10", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_dom, nzp_geu, nzp_kvar, pref ");
            sql2.Append(" into unlogged sel_kvar_10 from " + tXX_spls + " k ");
#else
            sql2.Append(" select nzp_dom, nzp_geu, nzp_kvar, pref ");
            sql2.Append(" from " + tXX_spls + " k ");
            sql2.Append(" into temp sel_kvar_10 with no log");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            conn_web.Close();

            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  sel_kvar_10 group by 1");

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }




            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();
                    string sChargeAlias = pref + "_charge_" + (prm.year_ - 2000).ToString("00");


                    ExecSQL(conn_db, " drop table sel_kvar10", false);

                    #region Заполнение sel_kvar10
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create temp table sel_kvar10 ( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_dom integer,");
                    sql2.Append(" isol integer default 1,");
                    sql2.Append(" nzp_geu integer,");
#if PG
                    sql2.Append(" pl_kvar numeric(14,2),");
#else
                    sql2.Append(" pl_kvar Decimal(14,2),");
#endif
                    sql2.Append(" count_gil integer) ");
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка создании таблицы " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar, count_gil)");
                    sql2.Append(" select nzp_dom, nzp_geu, nzp_kvar, 0 ");
                    sql2.Append(" from sel_kvar_10 k ");
                    sql2.Append(" where pref='" + pref + "'");
#if PG
                    if (prm.has_pu == "2")
                        sql2.Append(" and 0 < (select count(*)  " +
                            " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ");
                    if (prm.has_pu == "3")
                        sql2.Append(" and 0 = (select count(*)  " +
                            " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ");
#else
                    if (prm.has_pu == "2")
                        sql2.Append(" and 0 < (select count(*)  " +
                            " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") +" d"+
                            " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 and d.nzp_serv=" + prm.nzp_serv + 
                            " and k.nzp_dom=d.nzp_dom ) ");
                    if (prm.has_pu == "3")
                        sql2.Append(" and 0 = (select count(*)  " +
                            " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " d"+
                            " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 "+
                            " and d.nzp_dom=k.nzp_dom and d.nzp_serv=" + prm.nzp_serv + 
                            ") ");
#endif

                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("create index ix_tmpk_02 on sel_kvar10(nzp_kvar)");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append("analyze sel_kvar10 ");
#else
                    sql2.Append("update statistics for table sel_kvar10 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" update sel_kvar10 set count_gil=(select max(val_prm+0) ");
                    //sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=5 and is_actual=1 ");
                    //sql2.Append(" and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" update sel_kvar10 set pl_kvar=(select max(val_prm+0) ");
                    //sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=4 and is_actual=1 ");
                    //sql2.Append(" and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" update sel_kvar10 set pl_kvar=(select max(val_prm+0) ");
                    //sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=4 and is_actual=1 ");
                    //sql2.Append(" and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    //rsh2
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar10 set  ");
                    sql2.Append(" pl_kvar = (select max(squ) ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
                                + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=sel_kvar10.nzp_kvar  and d.tarif>0 ");
                    sql2.Append(" and 7 = d.nzp_serv ");
                    if (prm.nzp_key > -1) sql2.Append(" and d.nzp_supp = " + prm.nzp_key);//Добавляем поставщика
                    sql2.Append(" ), ");
                    sql2.Append(" count_gil = (select  max(gil) ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
                                + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=sel_kvar10.nzp_kvar  and d.tarif>0 ");
                    sql2.Append(" and 7 = d.nzp_serv ");
                    if (prm.nzp_key > -1) sql2.Append(" and d.nzp_supp = " + prm.nzp_key);//Добавляем поставщика
                    sql2.Append(" ) ");
#else
                    sql2.Append(" update sel_kvar10 set (pl_kvar, count_gil) = ((select max(squ), max(gil) ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_"
                        + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=sel_kvar10.nzp_kvar  and d.tarif>0 ");
                    sql2.Append(" and 7 = d.nzp_serv ");
                    if (prm.nzp_key > -1) sql2.Append(" and d.nzp_supp = " + prm.nzp_key);//Добавляем поставщика
                    sql2.Append(" ))");
#endif





                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar10 set isol=2 where nzp_kvar in (select nzp ");
#if PG
                    sql2.Append(" from " + pref + "_data.prm_1 where nzp_prm=3 and is_actual=1 and val_prm='2' ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date )");
#else
                    sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=3 and is_actual=1 and val_prm='2' ");
                    sql2.Append(" and dat_s<=today and dat_po>=today )");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    #endregion


                    ExecSQL(conn_db, "drop table t1", false);
                    #region Добавляем основную услугу


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" select nzp_geu, nzp_dom, a.nzp_kvar, max(nzp_frm) as nzp_frm, ");
                    sql2.Append(" max(pl_kvar) as pl_kvar, sum(0) as nzp_measure,");
                    sql2.Append(" max(tarif) as tarif, max(count_gil) as count_gil,");
                    sql2.Append(" sum(sum_charge) as sum_charge, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) +");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k,");
                    sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,");
                    sql2.Append(" sum(c_calc) as c_calc");
#if PG
                    sql2.Append(" into unlogged t1 from  " + sChargeAlias + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                    sql2.Append(" where nzp_serv = 7 ");
                    sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" and dat_charge is null  ");
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                    }
                    sql2.Append(" group by 1,2,3");
#else
                    sql2.Append(" from  " + sChargeAlias + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                    sql2.Append(" where nzp_serv = 7 ");
                    sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" and dat_charge is null  ");
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                    }
                    sql2.Append(" group by 1,2,3");
                    sql2.Append(" into temp t1 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);





                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set pl_kvar = (select max(pl_kvar) ");
                    sql2.Append(" from  sel_kvar10 d");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar ");
                    sql2.Append(" and isol=2 )");
                    sql2.Append(" where nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table t_upd", false);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" select a.nzp_kvar, ");
                    sql2.Append(" sum(case when f.nzp_measure=2 then c_sn else c_calc end) as c_calc");
#if PG
                    sql2.Append(" into unlogged t_upd from  " + sChargeAlias + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b, " + pref + "_kernel.formuls f");
                    sql2.Append(" where nzp_serv = 7  and a.nzp_frm=f.nzp_frm  ");
                    sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" and dat_charge is null ");
                    sql2.Append(" group by 1");
#else
                    sql2.Append(" from  " + sChargeAlias + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b, " + pref + "_kernel:formuls f");
                    sql2.Append(" where nzp_serv = 7  and a.nzp_frm=f.nzp_frm  ");
                    sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" and dat_charge is null ");
                    sql2.Append(" group by 1");
                    sql2.Append(" into temp t_upd with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set c_calc =  (select sum(c_calc) from t_upd where t1.nzp_kvar=t_upd.nzp_kvar) ");
                    ExecSQL(conn_db, sql2.ToString(), true);
                    ExecSQL(conn_db, "drop table t_upd", true);


                    #region Выборка перерасчетов прошлого периода
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
                    sql2.Append(" into unlogged t_nedop from " + pref + "_data.nedop_kvar a, sel_kvar10 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1,2,3 ");
#else
                    sql2.Append(" select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql2.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar10 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1,2,3 into temp t_nedop with no log");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
#if PG
                        + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#else
                        + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
#endif
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql2.Append(" and year_*12+month_>=month_s and  year_*12+month_<=month_po ");
                    sql2.Append(" group by 1,2");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }
                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                        sql2.Remove(0, sql2.Length);
                        sql2.Append(" insert into t1 (nzp_geu, nzp_dom, nzp_kvar, nzp_frm, tarif, sum_nedop, reval_k, reval_d)   ");
                        sql2.Append(" select nzp_geu, nzp_dom, b.nzp_kvar, 0, 0, ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
#if PG
                        sql2.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#else
                        sql2.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#endif
                        sql2.Append(" b, t_nedop d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                        sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                        sql2.Append(" and nzp_serv = 7");
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql2.Append(" and nzp_supp = " + prm.nzp_key);
                        }
                        sql2.Append(" group by 1,2,3,4,5");
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }

                    }
                    reader2.Close();

                    ExecSQL(conn_db, "drop table t_nedop", true);
                    #endregion
                    #endregion


                    ExecRead(conn_db, out reader2, "drop table t2", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif) as tarif ");
                    sql2.Append(" into unlogged t2 from t1   ");
                    sql2.Append(" group by 1 ");
#else
                    sql2.Append(" select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif) as tarif ");
                    sql2.Append(" from t1   ");
                    sql2.Append(" group by 1 into temp t2 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set nzp_frm  = (select max(nzp_frm) ");
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar) ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create index ixtmm_01 on t1(nzp_frm)");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" analyze t1");
#else
                    sql2.Append(" update statistics for table t1");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set nzp_measure= coalesce((select max(nzp_measure) from " + pref + "_kernel.formuls f ");
#else
                    sql2.Append(" update t1 set nzp_measure= nvl((select max(nzp_measure) from " + pref + "_kernel:formuls f ");
#endif
                    sql2.Append(" where  t1.nzp_frm=f.nzp_frm),0) ");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set tarif  = coalesce((select max(tarif) ");
#else
                    sql2.Append(" update t1 set tarif  = nvl((select max(tarif) ");
#endif
                    sql2.Append(" from t2 ");
                    sql2.Append(" where t1.nzp_kvar=t2.nzp_kvar),0) ");
                    sql2.Append(" where tarif =0  ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set tarif  = (select max(tarif_hv_gv) ");
#if PG
                    sql2.Append(" from " + Points.Pref + "_data.a_trf_smr a");
                    sql2.Append(" where t1.nzp_frm=a.nzp_frm and is_priv>0 ) ");
                    sql2.Append(" where nzp_measure=2 and nzp_frm in (select ");
                    sql2.Append(" nzp_frm from " + Points.Pref + "_data.a_trf_smr ");
#else
                    sql2.Append(" from " + Points.Pref + "_data:a_trf_smr a");
                    sql2.Append(" where t1.nzp_frm=a.nzp_frm and is_priv>0 ) ");
                    sql2.Append(" where nzp_measure=2 and nzp_frm in (select ");
                    sql2.Append(" nzp_frm from " + Points.Pref + "_data:a_trf_smr ");
#endif
                    sql2.Append(" where is_priv>0) ");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table t2", false);



                    DateTime date = new DateTime(prm.year_, prm.month_, 01);
                    DateTime date_1 = date.AddMonths(1);
                    string date_s = date.ToString("dd.MM.yyyy");
                    string date_po = date_1.ToString("dd.MM.yyyy");


                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set nzp_dom = (select nzp_dom_base from " + pref + "_data.link_dom_lit a");
                    sql2.Append(" where a.nzp_dom=t1.nzp_dom) ");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                    sql2.Append(" update t1 set nzp_dom = (select nzp_dom_base from " + pref + "_data:link_dom_lit a");
                    sql2.Append(" where a.nzp_dom=t1.nzp_dom) ");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    //По всем
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, ");
                    sql2.Append(" count_gil, rsum_tarif, tarif, sum_nedop, c_nedop, reval_k, reval_d, ");
                    sql2.Append(" c_reval_k, c_reval_d, sum_charge, c_calc,  pl_kvar )");
                    sql2.Append(" select nzp_geu,  a.nzp_dom,  a.nzp_kvar, ");
#if PG
                    sql2.Append(" trim(coalesce(name_frm,'Не определена формула'))||' . '||a.tarif as name_frm, ");
#else
                    sql2.Append(" trim(nvl(name_frm,'Не определена формула'))||' : '||a.tarif as name_frm, ");
#endif
                    sql2.Append(" f.nzp_measure, max(count_gil), sum(rsum_tarif), ");
                    sql2.Append(" max(a.tarif) as tarif, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(case when a.tarif>0 then sum_nedop/a.tarif else 0 end) as c_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(case when a.tarif>0 then -1*reval_k/a.tarif else 0 end) as c_reval_k,");
                    sql2.Append(" sum(case when a.tarif>0 then reval_d/a.tarif else 0 end) as c_reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, ");
                    sql2.Append(" sum(c_calc) as c_calc, ");
                    sql2.Append(" max(pl_kvar) as pl_kvar ");
#if PG
                    sql2.Append(" from t1 a left outer join " + pref + "_kernel.formuls f on (a.nzp_frm=f.nzp_frm)");
                    sql2.Append(" where  abs(coalesce(rsum_tarif,0))+  ");
                    sql2.Append(" abs(coalesce(sum_nedop,0)) + abs(coalesce(reval_k,0))+ ");
                    sql2.Append(" abs(coalesce(reval_d,0)) + ");
                    sql2.Append(" abs(coalesce(sum_charge,0))+ abs(coalesce(pl_kvar,0))>0.001 ");
#else
                    sql2.Append(" from t1 a, outer " + pref + "_kernel:formuls f ");
                    sql2.Append(" where a.nzp_frm=f.nzp_frm  and abs(nvl(rsum_tarif,0))+  ");
                    sql2.Append(" abs(nvl(sum_nedop,0)) + abs(nvl(reval_k,0))+ ");
                    sql2.Append(" abs(nvl(reval_d,0)) + ");
                    sql2.Append(" abs(nvl(sum_charge,0))+ abs(nvl(pl_kvar,0))>0.001 ");
#endif
                    sql2.Append(" group by 1,2,3,4,5");
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }



                    ExecSQL(conn_db, "drop table t1", true);


                }

            }
            reader.Close();

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_dom, nzp_kvar, nzp_geu into unlogged t_dom from t_svod group by 1,2,3 ; ");
#else
            sql2.Append(" select nzp_dom, nzp_kvar, nzp_geu from t_svod group by 1,2,3 into temp t_dom with no log; ");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select name_frm into unlogged t_frm from t_svod group by 1; ");
#else
            sql2.Append(" select name_frm from t_svod group by 1 into temp t_frm with no log; ");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" insert into t_svod(nzp_geu,  nzp_dom, nzp_kvar, name_frm) ");
            sql2.Append(" select nzp_geu,  nzp_dom, nzp_kvar, name_frm from t_dom, t_frm ");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            ExecRead(conn_db, out reader2, "drop table t_dom", true);
            ExecRead(conn_db, out reader2, "drop table t_frm", true);
            ExecRead(conn_db, out reader2, "drop table sel_kvar_10", true);

            DbTables tables = new DbTables(conn_db);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" select geu, ulica, ndom, Replace(nkor,'-','') as nkor, idom,  ");
#if PG
            sql2.Append(" name_frm,sum(coalesce(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(coalesce(rsum_tarif,0)) as rsum_tarif,  ");
            sql2.Append(" max(coalesce(a.tarif,0)) as tarif ,");
            sql2.Append(" max(coalesce(a.nzp_measure,0)) as nzp_measure,");
            sql2.Append(" sum(coalesce(sum_nedop,0)) as sum_nedop,  ");
            sql2.Append(" sum(-1*coalesce(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(coalesce(reval_d,0)) as reval_d,");
            sql2.Append(" sum(coalesce(c_nedop,0)) as c_nedop,  ");
            sql2.Append(" sum(coalesce(c_reval_k,0)) as c_reval_k, ");
            sql2.Append(" sum(coalesce(c_reval_d,0)) as c_reval_d,");
            sql2.Append(" sum(coalesce(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(coalesce(c_calc,0)) as c_calc, ");
            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar ");
#else
            sql2.Append(" name_frm,sum(nvl(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(nvl(rsum_tarif,0)) as rsum_tarif,  ");
            sql2.Append(" max(nvl(a.tarif,0)) as tarif ,");
            sql2.Append(" max(nvl(a.nzp_measure,0)) as nzp_measure,");
            sql2.Append(" sum(nvl(sum_nedop,0)) as sum_nedop,  ");
            sql2.Append(" sum(-1*nvl(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(nvl(reval_d,0)) as reval_d,");
            sql2.Append(" sum(nvl(c_nedop,0)) as c_nedop,  ");
            sql2.Append(" sum(nvl(c_reval_k,0)) as c_reval_k, ");
            sql2.Append(" sum(nvl(c_reval_d,0)) as c_reval_d,");
            sql2.Append(" sum(nvl(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(nvl(c_calc,0)) as c_calc, ");
            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar ");
#endif
            sql2.Append(" from t_svod a, " + tables.dom + " d, " + tables.ulica + " su, " + tables.geu + " sg ");
            sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul and a.nzp_geu=sg.nzp_geu ");
            sql2.Append(" group by  1,2,3,4,5,6");
            sql2.Append(" order by geu, ulica, idom, ndom, nkor, name_frm  ");
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
            ExecSQL(conn_db, "drop table t_svod", true);

            conn_db.Close();




            reader.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }
    }
}
