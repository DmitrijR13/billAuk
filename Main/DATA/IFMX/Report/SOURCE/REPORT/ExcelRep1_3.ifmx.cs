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
        public DataTable GetVedNormPotr(Prm prm, out Returns ret, string Nzp_user)
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
            string where_supp = "";
            string where_serv = "";
            conn_web.Close();

            #region Ограничения на выборку
            if (prm.RolesVal != null)
            {
                if (prm.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and nzp_supp in (" + role.val + ") ";
                        }
                    }
                }
            }
            #endregion


            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();



            ExecRead(conn_db, out reader2, "drop table sel_kvar7", false);

            sql.Remove(0, sql.Length);
            sql.Append(" select * ");
#if PG
            sql.Append(" into unlogged sel_kvar7 from  " + tXX_spls );
#else
            sql.Append(" from  " + tXX_spls + " into temp sel_kvar7 with no log");
#endif
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }

            ExecRead(conn_db, out reader2, "create index ix_tmp01192 on sel_kvar7(nzp_kvar)", true);
#if PG
     ExecRead(conn_db, out reader2, "analyze sel_kvar7", true);
#else
            ExecRead(conn_db, out reader2, "update statistics for table sel_kvar7", true);
#endif

            ExecRead(conn_db, out reader2, "drop table t_svod", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append("create unlogged table t_svod( ");
            sql2.Append("nzp_serv integer,");
            sql2.Append("nzp_measure integer,");
            sql2.Append("norma decimal(14,2),");
            sql2.Append("count_gil integer,");
            sql2.Append("pl_kvar Decimal(14,2),");
            sql2.Append("serv_calc Decimal(14,4),");
            sql2.Append("kfkor Decimal(8,4),");
            sql2.Append("c_calc Decimal(14,4),");
            sql2.Append("frm_name char(100),");
            sql2.Append("tarif Decimal(14,2),");
            sql2.Append("rsum_tarif Decimal(14,2),");
            sql2.Append("reval_k Decimal(14,2),");
            sql2.Append("reval_d Decimal(14,2),");
            sql2.Append("sum_nedop Decimal(14,2),");
            sql2.Append("sum_charge Decimal(14,2)) ");
#else
            sql2.Append("create temp table t_svod( ");
            sql2.Append("nzp_serv integer,");
            sql2.Append("nzp_measure integer,");
            sql2.Append("norma decimal(14,2),");
            sql2.Append("count_gil integer,");
            sql2.Append("pl_kvar Decimal(14,2),");
            sql2.Append("serv_calc Decimal(14,4),");
            sql2.Append("kfkor Decimal(8,4),");
            sql2.Append("c_calc Decimal(14,4),");
            sql2.Append("frm_name char(100),");
            sql2.Append("tarif Decimal(14,2),");
            sql2.Append("rsum_tarif Decimal(14,2),");
            sql2.Append("reval_k Decimal(14,2),");
            sql2.Append("reval_d Decimal(14,2),");
            sql2.Append("sum_nedop Decimal(14,2),");
            sql2.Append("sum_charge Decimal(14,2)) with no log");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }


            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  sel_kvar7 group by 1");
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
                    string sAliasm = pref + "_charge_" + (prm.year_ - 2000).ToString("00");

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create unlogged table t_svod1( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_serv integer,");
                    sql2.Append(" nzp_frm integer,");
                    sql2.Append(" norma decimal(14,2) default 0,");
                    sql2.Append(" count_gil integer default 0,");
                    sql2.Append(" pl_kvar Decimal(14,2) default 0,");
                    sql2.Append(" serv_calc Decimal(14,4) default 0,");
                    sql2.Append(" kfkor Decimal(8,4) default 0,");
                    sql2.Append(" c_calc Decimal(14,4) default 0,");
                    sql2.Append(" tarif Decimal(14,2) default 0,");
                    sql2.Append(" rsum_tarif Decimal(14,2) default 0,");
                    sql2.Append(" reval_k Decimal(14,2) default 0,");
                    sql2.Append(" reval_d Decimal(14,2) default 0,");
                    sql2.Append(" sum_nedop Decimal(14,2) default 0,");
                    sql2.Append(" sum_charge Decimal(14,2) default 0) ");
#else
                    sql2.Append(" create temp table t_svod1( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_serv integer,");
                    sql2.Append(" nzp_frm integer,");
                    sql2.Append(" norma decimal(14,2) default 0,");
                    sql2.Append(" count_gil integer default 0,");
                    sql2.Append(" pl_kvar Decimal(14,2) default 0,");
                    sql2.Append(" serv_calc Decimal(14,4) default 0,");
                    sql2.Append(" kfkor Decimal(8,4) default 0,");
                    sql2.Append(" c_calc Decimal(14,4) default 0,");
                    sql2.Append(" tarif Decimal(14,2) default 0,");
                    sql2.Append(" rsum_tarif Decimal(14,2) default 0,");
                    sql2.Append(" reval_k Decimal(14,2) default 0,");
                    sql2.Append(" reval_d Decimal(14,2) default 0,");
                    sql2.Append(" sum_nedop Decimal(14,2) default 0,");
                    sql2.Append(" sum_charge Decimal(14,2) default 0) with no log");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }
                    //Вытаскиваем начисления
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod1(nzp_kvar, nzp_serv, nzp_frm,  tarif, c_calc, rsum_tarif, reval_k, reval_d, sum_nedop, sum_charge)");
                    // sql2.Append(" select a.nzp_kvar, (case when nzp_supp=612 and nzp_serv in (6,7) then -nzp_serv else nzp_serv end), ");
                    sql2.Append(" select a.nzp_kvar, nzp_serv, ");
                    sql2.Append(" max(nzp_frm), max(tarif),  sum(case when tarif>0 then c_calc else 0 end), sum(rsum_tarif), ");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end)+ ");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +  ");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,  ");
                    sql2.Append(" sum(sum_nedop),  sum(sum_charge) ");
                    sql2.Append(" from " + sAliasm + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7 k ");
                    sql2.Append(" where nzp_serv > 1 and  dat_charge is null");
                    sql2.Append(" and k.nzp_kvar=a.nzp_kvar ");
                    sql2.Append(" group by 1,2");
#else
                 sql2.Append(" insert into t_svod1(nzp_kvar, nzp_serv, nzp_frm,  tarif, c_calc, rsum_tarif, reval_k, reval_d, sum_nedop, sum_charge)");
                    // sql2.Append(" select a.nzp_kvar, (case when nzp_supp=612 and nzp_serv in (6,7) then -nzp_serv else nzp_serv end), ");
                    sql2.Append(" select a.nzp_kvar, nzp_serv, ");
                    sql2.Append(" max(nzp_frm), max(tarif),  sum(case when tarif>0 then c_calc else 0 end), sum(rsum_tarif), ");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end)+ ");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +  ");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,  ");
                    sql2.Append(" sum(sum_nedop),  sum(sum_charge) ");
                    sql2.Append(" from " + sAliasm + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7 k ");
                    sql2.Append(" where nzp_serv > 1 and  dat_charge is null");
                    sql2.Append(" and k.nzp_kvar=a.nzp_kvar ");
                    sql2.Append(" group by 1,2");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    #region Выборка перерасчетов прошлого периода

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, min(extract (year from (dat_s))*12+extract( month from (dat_s))) as month_s,  max(extract (year from (dat_po))*12+extract (month from(dat_po))) as month_po");
                    sql2.Append(" into unlogged t_nedop from " + pref + "_data.nedop_kvar a, sel_kvar7 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1  ");
#else
       sql2.Append(" select a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql2.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar7 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1 into temp t_nedop with no log");
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
          #if PG
          sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + sAliasm + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql2.Append(" group by 1,2");
#else
          sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + sAliasm + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql2.Append(" group by 1,2");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
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
#if PG
                        sql2.Append(" insert into t_svod1 (nzp_kvar, nzp_serv, tarif, sum_nedop, reval_k, reval_d) ");
                        sql2.Append(" select d.nzp_kvar, nzp_serv , 0, ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, t_nedop d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                        sql2.Append(where_serv + where_supp);
                        sql2.Append(" group by 1,2,3");
#else
                sql2.Append(" insert into t_svod1 (nzp_kvar, nzp_serv, tarif, sum_nedop, reval_k, reval_d) ");
                        sql2.Append(" select d.nzp_kvar, nzp_serv , 0, ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, t_nedop d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                        sql2.Append(where_serv + where_supp);
                        sql2.Append(" group by 1,2,3");
#endif
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

                    ExecSQL(conn_db, "drop table t12", false);

                    //Учитываем характеристики на старого поставщика
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, nzp_serv, ");
                    sql2.Append(" sum(case when cnt_stage = 1 and stek=3 then val1  ");
                    sql2.Append(" when cnt_stage = 0 and stek=3 then val2 else 0 end) as serv_calc,");
                    sql2.Append(" sum(case when  cnt1>0 and stek=30 then val1/cnt1 else 0 end) as norma, ");
                    sql2.Append(" sum(case when stek=3 then Round(rashod+dlt_reval,2) else 0 end) as rashod, ");
                    sql2.Append(" sum(case when stek=3 then kf307 else 0 end) as kfkor ");
                    sql2.Append(" into unlogged t12  from " + sAliasm + ".counters_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7  k ");
                    sql2.Append(" where k.nzp_kvar=a.nzp_kvar and dat_charge is null and nzp_type=3 and stek in (3,30)");
                    sql2.Append(" and nzp_serv not in (7,8)  group by 1,2 ");
#else
                    sql2.Append(" select a.nzp_kvar, nzp_serv, ");
                    sql2.Append(" sum(case when cnt_stage = 1 and stek=3 then val1  ");
                    sql2.Append(" when cnt_stage = 0 and stek=3 then val2 else 0 end) as serv_calc,");
                    sql2.Append(" sum(case when  cnt1>0 and stek=30 then val1/cnt1 else 0 end) as norma, ");
                    sql2.Append(" sum(case when stek=3 then Round(rashod+dlt_reval,2) else 0 end) as rashod, ");
                    sql2.Append(" sum(case when stek=3 then kf307 else 0 end) as kfkor ");
                    sql2.Append(" from " + sAliasm + ":counters_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7  k ");
                    sql2.Append(" where k.nzp_kvar=a.nzp_kvar and dat_charge is null and nzp_type=3 and stek in (3,30)");
                    sql2.Append(" and nzp_serv not in (7,8)  group by 1,2 into temp t12 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t12 set norma=(select max(value::int) ");
#if PG
                    sql2.Append(" from " + pref + "_kernel.res_values a, " + pref + "_data.prm_1 p where nzp_prm=2007 ");
#else
             sql2.Append(" from " + pref + "_kernel:res_values a, " + pref + "_data:prm_1 p where nzp_prm=2007 ");
#endif
                    sql2.Append(" and a.nzp_y= p.val_prm::int and is_actual=1");
                    sql2.Append(" and t12.nzp_kvar=nzp and nzp_res=10001 and nzp_x=3 ");
                    sql2.Append(" and p.dat_s<='01." + prm.month_ + "." + prm.year_ + "'");
                    sql2.Append(" and p.dat_po>='01." + prm.month_ + "." + prm.year_ + "')");
                    sql2.Append(" where nzp_serv=7 and norma = 0");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    ExecSQL(conn_db, "create index ix_tmp_serv_101 on t12(nzp_kvar, nzp_serv)", true);
#if PG
                    ExecSQL(conn_db, "analyze t12", true);
#else
                    ExecSQL(conn_db, "update statistics for table t12", true);
#endif


                    ExecSQL(conn_db, "drop table t13", false);
                    //rsh2
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, nzp_serv, max(round(gil::int,0)) as count_gil, max(squ::int) as pl_kvar ");
                    sql2.Append(" into unlogged t13 from " + sAliasm + ".calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7  k ");
                    sql2.Append(" where k.nzp_kvar=a.nzp_kvar and a.tarif >0 group by 1,2 ");
          
#else
              sql2.Append(" select a.nzp_kvar, nzp_serv, max(round(gil,0)) as count_gil, max(squ) as pl_kvar ");
                    sql2.Append(" from " + sAliasm + ":calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar7  k ");
                    sql2.Append(" where k.nzp_kvar=a.nzp_kvar and a.tarif >0 group by 1,2 ");
                    sql2.Append(" into temp t13 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    ExecSQL(conn_db, "create index ix_tmp_serv_102 on t13(nzp_kvar, nzp_serv)", true);
#if PG
                    ExecSQL(conn_db, "analyze t13", true);
#else
      ExecSQL(conn_db, "update statistics for table t13", true);
#endif

                    /* ExecRead(conn_db, out reader2, "drop table t_dopserv", false);

                     sql2.Remove(0, sql2.Length);
                     sql2.Append(" select nzp_kvar, max(nzp_serv) as nzp_serv, abs(nzp_serv) as abs_serv from t_svod1  ");
                     sql2.Append(" where tarif>0 group by 1,3 into temp t_dopserv with no log");
                     ExecRead(conn_db, out reader2, sql2.ToString(), true);

                     ExecRead(conn_db, out reader2, "create index ix_tmp_serv_01 on t_dopserv(nzp_kvar, abs_serv)", false);
                     ExecRead(conn_db, out reader2, "create index ix_tmp_serv_02 on t_dopserv(nzp_serv)", false);
                     ExecRead(conn_db, out reader2, "update statistics for table t_dopserv", false);


                     sql2.Remove(0, sql2.Length);
                     sql2.Append(" update t12 set nzp_serv = (select nzp_serv from t_dopserv  ");
                     sql2.Append(" where t12.nzp_kvar=t_dopserv.nzp_kvar and t12.nzp_serv=t_dopserv.abs_serv)");
                     sql2.Append(" where nzp_kvar in (select nzp_kvar from t_dopserv)");
                     ExecRead(conn_db, out reader2, sql2.ToString(), true);



                     sql2.Remove(0, sql2.Length);
                     sql2.Append(" update t13 set nzp_serv = (select nzp_serv from t_dopserv  ");
                     sql2.Append(" where t13.nzp_kvar=t_dopserv.nzp_kvar and t13.nzp_serv=t_dopserv.abs_serv)");
                     sql2.Append(" where nzp_kvar in (select nzp_kvar from t_dopserv)");
                     ExecRead(conn_db, out reader2, sql2.ToString(), true);

                     ExecRead(conn_db, out reader2, "drop table t_dopserv", true);*/

                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" insert into t_svod1(nzp_kvar, nzp_serv, serv_calc,  norma, c_calc, kfkor)");
                    //sql2.Append(" select nzp_kvar, nzp_serv, serv_calc, norma, rashod,kfkor ");
                    //sql2.Append(" from t12 k ");
                    //ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t_svod1 set (serv_calc,  c_calc)=(0,0)");
                    sql2.Append(" where nzp_serv in (6,7) ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_svod1 set serv_calc= ");
                    sql2.Append(" (select serv_calc  from t12 where t_svod1.nzp_kvar=t12.nzp_kvar and ");
                    sql2.Append(" t_svod1.nzp_serv=t12.nzp_serv ), ");
                    sql2.Append(" norma=");
                    sql2.Append(" (select  norma  from t12 where t_svod1.nzp_kvar=t12.nzp_kvar and ");
                    sql2.Append(" t_svod1.nzp_serv=t12.nzp_serv ), ");
                    sql2.Append(" c_calc=");
                    sql2.Append(" (select  rashod  from t12 where t_svod1.nzp_kvar=t12.nzp_kvar and ");
                    sql2.Append(" t_svod1.nzp_serv=t12.nzp_serv ), ");
                    sql2.Append("kfkor=");
                    sql2.Append(" (select kfkor from t12 where t_svod1.nzp_kvar=t12.nzp_kvar and ");
                    sql2.Append(" t_svod1.nzp_serv=t12.nzp_serv ) ");
                    sql2.Append( "where tarif>0 and nzp_serv in (select nzp_serv from t12 group by 1)");
#else
                    sql2.Append(" update t_svod1 set (serv_calc,  norma, c_calc, kfkor)=");
                    sql2.Append(" ((select serv_calc, norma, rashod, kfkor from t12 where t_svod1.nzp_kvar=t12.nzp_kvar and ");
                    sql2.Append(" t_svod1.nzp_serv=t12.nzp_serv )) where tarif>0 and nzp_serv in (select nzp_serv from t12 group by 1)");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);



                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod1(nzp_kvar, nzp_serv, count_gil,pl_kvar)");
                    sql2.Append(" select nzp_kvar, nzp_serv, count_gil,pl_kvar ");
                    sql2.Append(" from t13 k ");
                    ExecSQL(conn_db, sql2.ToString(), true);



                    ExecSQL(conn_db, "drop table t12", false);
                    ExecSQL(conn_db, "drop table t13", false);
                    ExecSQL(conn_db, "drop table t1", false);
                    ExecSQL(conn_db, "drop table t45", false);
                    ExecSQL(conn_db, "drop table t46", false);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, sum(serv_calc) as serv_calc, sum(c_calc) as c_calc ");
                    sql2.Append(" into unlogged t46 from t_svod1 k where nzp_serv in (6,14) group by 1  ");
#else
               sql2.Append(" select nzp_kvar, sum(serv_calc) as serv_calc, sum(c_calc) as c_calc ");
                    sql2.Append(" from t_svod1 k where nzp_serv in (6,14) group by 1 into temp t46 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, nzp_serv, max(nzp_frm) as nzp_frm  ");
                    sql2.Append(" into unlogged t45 from t_svod1 k where nzp_serv in (6,9,25,10) group by 1,2   ");
#else
                 sql2.Append(" select nzp_kvar, nzp_serv, max(nzp_frm) as nzp_frm  ");
                    sql2.Append(" from t_svod1 k where nzp_serv in (6,9,25,10) group by 1,2 into temp t45 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" update t_svod1 set (nzp_serv,nzp_frm)=((select nzp_serv,nzp_frm ");
                    //sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=6 ))  ");
                    //sql2.Append(" where nzp_serv = 510 ");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_svod1 set nzp_serv=(select nzp_serv");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=25 ),  ");
                    sql2.Append("  nzp_frm=(select  nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=25 )  ");

                    sql2.Append(" where nzp_serv = 515 ");
#else
                    sql2.Append(" update t_svod1 set (nzp_serv,nzp_frm)=((select nzp_serv,nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=25 ))  ");
                    sql2.Append(" where nzp_serv = 515 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_svod1 set nzp_serv=(select nzp_serv ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ),  ");

                    sql2.Append("nzp_frm=(select nzp_frm  ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ),  ");

                    sql2.Append("serv_calc=0, ");
                 //   sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 )),  ");

                    sql2.Append(" c_calc=0 ");
                   // sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");

                    sql2.Append(" where nzp_serv =14 ");
#else
                    sql2.Append(" update t_svod1 set (nzp_serv,nzp_frm,serv_calc,c_calc)=((select nzp_serv,nzp_frm,0,0 ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");
                    sql2.Append(" where nzp_serv =14 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_svod1 set nzp_serv=((select nzp_serv  ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 )),  ");

                    sql2.Append(" nzp_frm=((select  nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");
                    sql2.Append(" where nzp_serv =513 ");
#else
                    sql2.Append(" update t_svod1 set (nzp_serv,nzp_frm)=((select nzp_serv,nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");
                    sql2.Append(" where nzp_serv =513 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);

#if PG
                    sql2.Append(" update t_svod1 set nzp_serv=((select nzp_serv  ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 )),  ");

                    sql2.Append(" nzp_frm=((select  nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");
                    sql2.Append(" where nzp_serv =514 ");
#else
                    sql2.Append(" update t_svod1 set (nzp_serv,nzp_frm)=((select nzp_serv,nzp_frm ");
                    sql2.Append(" from t45 where t_svod1.nzp_kvar=t45.nzp_kvar and t45.nzp_serv=9 ))  ");
                    sql2.Append(" where nzp_serv =514 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_svod1 set serv_calc=((select serv_calc  ");
                    sql2.Append(" from t46 where t_svod1.nzp_kvar=t46.nzp_kvar and t_svod1.tarif>0  )),  ");

                    sql2.Append("c_calc=((select  c_calc ");
                    sql2.Append(" from t46 where t_svod1.nzp_kvar=t46.nzp_kvar and t_svod1.tarif>0  ))  ");
                    sql2.Append(" where nzp_serv = 7 ");
#else
                   sql2.Append(" update t_svod1 set (serv_calc,c_calc)=((select serv_calc, c_calc ");
                    sql2.Append(" from t46 where t_svod1.nzp_kvar=t46.nzp_kvar and t_svod1.tarif>0  ))  ");
                    sql2.Append(" where nzp_serv = 7 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    ExecSQL(conn_db, "drop table t45", true);
                    ExecSQL(conn_db, "drop table t46", true);




                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, nzp_serv, ");
                    sql2.Append(" max(tarif) as tarif, max(nzp_frm) as nzp_frm, max(norma) as norma,");
                    sql2.Append(" max(count_gil) as count_gil, max(pl_kvar) as pl_kvar,");
                    sql2.Append(" max(serv_calc) as serv_calc, max(kfkor) as kfkor,");
                    sql2.Append(" sum(c_calc) as c_calc,");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(sum_charge) as sum_charge");
                    sql2.Append("  into unlogged t1  from t_svod1");
                    sql2.Append(" group by 1,2");
               
#else
             sql2.Append(" select nzp_kvar, nzp_serv, ");
                    sql2.Append(" max(tarif) as tarif, max(nzp_frm) as nzp_frm, max(norma) as norma,");
                    sql2.Append(" max(count_gil) as count_gil, max(pl_kvar) as pl_kvar,");
                    sql2.Append(" max(serv_calc) as serv_calc, max(kfkor) as kfkor,");
                    sql2.Append(" sum(c_calc) as c_calc,");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(sum_charge) as sum_charge");
                    sql2.Append(" from t_svod1");
                    sql2.Append(" group by 1,2");
                    sql2.Append(" into temp t1 with no log;");
#endif

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
                    sql2.Append(" drop table t_svod1");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod(nzp_serv, nzp_measure, frm_name, tarif, norma,count_gil,");
                    sql2.Append(" pl_kvar, serv_calc, kfkor,c_calc,rsum_tarif,reval_k,reval_d,sum_nedop,sum_charge)");
                    sql2.Append(" select nzp_serv, coalesce(nzp_measure,7) as nzp_measure, coalesce(name_frm,'Формула неопределена'), a.tarif, max(norma) as norma,");
                    sql2.Append(" sum(count_gil) as count_gil, sum(coalesce(pl_kvar,0)) as pl_kvar,");
                    sql2.Append(" sum(coalesce(serv_calc,0)) as serv_calc, max(0) as kfkor,");
                    sql2.Append(" sum(coalesce(c_calc,0)) as c_calc, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(sum_charge) as sum_charge ");
                    sql2.Append(" from t1 a left outer join  " + pref + "_kernel.formuls b");
                    sql2.Append(" on  a.nzp_frm=b.nzp_frm ");
                    sql2.Append(" group by 1,2,3,4 ");
#else
           sql2.Append(" insert into t_svod(nzp_serv, nzp_measure, frm_name, tarif, norma,count_gil,");
                    sql2.Append(" pl_kvar, serv_calc, kfkor,c_calc,rsum_tarif,reval_k,reval_d,sum_nedop,sum_charge)");
                    sql2.Append(" select nzp_serv, nvl(nzp_measure,7) as nzp_measure, nvl(name_frm,'Формула неопределена'), a.tarif, max(norma) as norma,");
                    sql2.Append(" sum(count_gil) as count_gil, sum(nvl(pl_kvar,0)) as pl_kvar,");
                    sql2.Append(" sum(nvl(serv_calc,0)) as serv_calc, max(0) as kfkor,");
                    sql2.Append(" sum(nvl(c_calc,0)) as c_calc, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(sum_charge) as sum_charge ");
                    sql2.Append(" from t1 a, outer " + pref + "_kernel:formuls b");
                    sql2.Append(" where a.nzp_frm=b.nzp_frm ");
                    sql2.Append(" group by 1,2,3,4 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                }
            }
            reader.Close();

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select service,");
            sql2.Append(" (case when a.nzp_serv in (-6,-7,6,7,9,14) then 'куб.м.' else measure end ) as measuref, measure, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_kernel");
            sql2.Append( DBManager.getServer(conn_db) + ".s_measure g,   " + Points.Pref + "_kernel");
            sql2.Append(  DBManager.getServer(conn_db) + ".services s");
            sql2.Append(" where a.nzp_measure=g.nzp_measure and abs(a.nzp_serv)=s.nzp_serv  ");
            sql2.Append(" order by nzp_serv, frm_name,tarif ");
#else
            sql2.Append(" select service,");
            sql2.Append(" (case when a.nzp_serv in (-6,-7,6,7,9,14) then 'куб.м.' else measure end ) as measuref, measure, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_kernel");
            sql2.Append("@" + DBManager.getServer(conn_db) + ":s_measure g,   " + Points.Pref + "_kernel");
            sql2.Append("@" + DBManager.getServer(conn_db) + ":services s");
            sql2.Append(" where a.nzp_measure=g.nzp_measure and abs(a.nzp_serv)=s.nzp_serv  ");
            sql2.Append(" order by nzp_serv, frm_name,tarif ");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
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
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
            }
            if (reader2 != null) reader2.Close();

            ExecSQL(conn_db, "drop table t_svod", true);
            ExecSQL(conn_db, "drop table sel_kvar7", true);
            if (reader2 != null) reader2.Close();
            conn_db.Close();




            reader.Close();
            #endregion

            return LocalTable;

        }
    }
}
