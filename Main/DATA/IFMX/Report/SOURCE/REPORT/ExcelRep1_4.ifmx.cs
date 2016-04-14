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
        public DataTable GetSpravSoderg(Prm prm, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

            if (prm.nzp_serv == 9)
            {
                return GetSpravSoderg9(prm, out ret, Nzp_user);
            }

            if (prm.nzp_serv == 7)
            {
                return GetSpravSoderg7(prm, out ret, Nzp_user);
            }

            if (prm.nzp_serv == 8)
            {
                return GetSpravSoderg8(prm, out ret, Nzp_user);
            }
            if (prm.nzp_serv == 6)
            {
                return GetSpravSoderg6(prm, out ret, Nzp_user);
            }

            ret = Utils.InitReturns();



            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;
            IDataReader reader3;

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
            sql2.Append(" rsum_tarif Decimal(14,2), ");
            sql2.Append(" tarif Decimal(14,3), ");
            sql2.Append(" c_calc Decimal(14,4), ");
            sql2.Append(" c_sn Decimal(14,2), ");
            sql2.Append(" c_sn_odn Decimal(14,2), ");
            sql2.Append(" c_calc_odn Decimal(14,2), ");
            sql2.Append(" sum_nedop Decimal(14,2), ");
            sql2.Append(" reval_k Decimal(14,2),");
            sql2.Append(" reval_d Decimal(14,2), ");
            sql2.Append(" c_nedop Decimal(14,2),");
            sql2.Append(" c_reval_k Decimal(14,2),");
            sql2.Append(" c_reval_d Decimal(14,2), ");
            sql2.Append(" sum_odn Decimal(14,2), ");
            sql2.Append(" c_odn Decimal(14,2), ");
            sql2.Append(" c_reval Decimal(14,2), ");
            sql2.Append(" c_reval_odn Decimal(14,2), ");
            sql2.Append(" pl_kvar Decimal(14,2), ");
            sql2.Append(" sum_charge Decimal(14,2)) ");
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
            sql2.Append(" c_sn Decimal(14,2), ");
            sql2.Append(" c_sn_odn Decimal(14,2), ");
            sql2.Append(" c_calc_odn Decimal(14,2), ");
            sql2.Append(" sum_nedop Decimal(14,2), ");
            sql2.Append(" reval_k Decimal(14,2),");
            sql2.Append(" reval_d Decimal(14,2), ");
            sql2.Append(" c_nedop Decimal(14,2),");
            sql2.Append(" c_reval_k Decimal(14,2),");
            sql2.Append(" c_reval_d Decimal(14,2), ");
            sql2.Append(" sum_odn Decimal(14,2), ");
            sql2.Append(" c_odn Decimal(14,2), ");
            sql2.Append(" c_reval Decimal(14,2), ");
            sql2.Append(" c_reval_odn Decimal(14,2), ");
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
            sql2.Append(" into unlogged sel_kvar_10  from " + tXX_spls + " k ");
         
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

            bool humanServ = false;
            if (prm.nzp_serv == 6 || prm.nzp_serv == 7 || prm.nzp_serv == 9 || prm.nzp_serv == 14)
                humanServ = true;


            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();
                    string sChargeAlias = pref + "_charge_" + (prm.year_ - 2000).ToString("00");


                    ExecSQL(conn_db, " drop table sel_kvar10", false);

                    #region Заполнение sel_kvar10
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create unlogged table sel_kvar10 ( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_dom integer,");
                    sql2.Append(" isol integer default 1,");
                    sql2.Append(" nzp_geu integer,");
                    sql2.Append(" pl_kvar Decimal(14,2),");
                    sql2.Append(" count_gil integer) ");
#else
  sql2.Append(" create temp table sel_kvar10 ( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_dom integer,");
                    sql2.Append(" isol integer default 1,");
                    sql2.Append(" nzp_geu integer,");
                    sql2.Append(" pl_kvar Decimal(14,2),");
                    sql2.Append(" count_gil integer) ");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка создании таблицы " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar, count_gil)");
                    sql2.Append(" select nzp_dom, nzp_geu, nzp_kvar, 0 ");
                    sql2.Append(" from sel_kvar_10 k ");
                    sql2.Append(" where pref='" + pref + "'");
                    if (prm.has_pu == "2")
                        sql2.Append(" and 0 < (select count(*)  " +
                            " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ");
                    if (prm.has_pu == "3")
                        sql2.Append(" and 0 = (select count(*)  " +
                            " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ");
#else
          sql2.Append(" insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar, count_gil)");
                    sql2.Append(" select nzp_dom, nzp_geu, nzp_kvar, 0 ");
                    sql2.Append(" from sel_kvar_10 k ");
                    sql2.Append(" where pref='" + pref + "'");
                    if (prm.has_pu == "2")
                        sql2.Append(" and 0 < (select count(*)  " +
                            " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " d"+
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + 
                            prm.nzp_serv + " and d.nzp_dom=k.nzp_dom) ");
                    if (prm.has_pu == "3")
                        sql2.Append(" and 0 = (select count(*)  " +
                            " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") + " d"+
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" +
                            prm.nzp_serv + " and d.nzp_dom=k.nzp_dom) ");
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

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar10 set count_gil=(select max(val_prm::numeric) ");
                    sql2.Append(" from " + pref + "_data.prm_1 where nzp_prm=5 and is_actual=1 ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date and sel_kvar10.nzp_kvar=nzp)");
#else
           sql2.Append(" update sel_kvar10 set count_gil=(select max(val_prm+0) ");
                    sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=5 and is_actual=1 ");
                    sql2.Append(" and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar10 set pl_kvar=(select max(val_prm::numeric) ");
                    sql2.Append(" from " + pref + "_data.prm_1 where nzp_prm=4 and is_actual=1 ");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date and sel_kvar10.nzp_kvar=nzp)");
#else
              sql2.Append(" update sel_kvar10 set pl_kvar=(select max(val_prm+0) ");
                    sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=4 and is_actual=1 ");
                    sql2.Append(" and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar10 set isol=2 ");
                    sql2.Append(" where nzp_kvar in (select nzp ");
                    sql2.Append(" from " + pref + "_data.prm_1 where nzp_prm=3 and is_actual=1 and val_prm='2'");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date )");
#else
                    sql2.Append(" update sel_kvar10 set isol=2 ");
                    sql2.Append(" where nzp_kvar in (select nzp ");
                    sql2.Append(" from " + pref + "_data:prm_1 where nzp_prm=3 and is_actual=1 and val_prm='2'");
                    sql2.Append(" and dat_s<=today and dat_po>=today )");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    #endregion

                    string odnServ = "-1";
                    if (prm.nzp_serv == 6)
                    {
                        odnServ = "510";
                    }
                    else if (prm.nzp_serv == 7)
                    {
                        odnServ = "511";
                    }
                    else if (prm.nzp_serv == 9)
                    {
                        odnServ = "513";
                    }
                    else if (prm.nzp_serv == 25)
                    {
                        odnServ = "515";
                    }
                    else if (prm.nzp_serv == 14)
                    {
                        odnServ = "514";
                    }
                    else if (prm.nzp_serv == 10)
                    {
                        odnServ = "517";
                    }
                    else if (prm.nzp_serv == 414)
                    {
                        odnServ = "518";
                    }

                    ExecSQL(conn_db, "drop table t1", false);
                    #region Добавляем основную услугу
                    if ((prm.nzp_serv == 6) || (prm.nzp_serv == 9) || (prm.nzp_serv == 7)
                        || (prm.nzp_serv == 25) || (prm.nzp_serv == 10) || (prm.nzp_serv == 14)
                        || (prm.nzp_serv == 414))
                    {


                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, ");
                        sql2.Append(" sum(0) as nzp_measure,");
                        sql2.Append(" max(tarif) as tarif, max(0) as count_gil,");
                        sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                        sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k,");
                        sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,");
                        sql2.Append(" sum(case when nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append(" then 0 else rsum_tarif end) as sum_odn, ");
                        sql2.Append(" sum(case when nzp_serv=" + prm.nzp_serv.ToString() + " and tarif>0.0001 ");
                        sql2.Append(" then (reval + sum_nedop)/tarif else 0 end) as c_reval, ");
                        sql2.Append(" sum(case when nzp_serv=" + odnServ + " and tarif>0.0001 ");
                        sql2.Append(" then (reval + sum_nedop)/tarif else 0 end) as c_reval_odn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append("  then c_calc else 0 end) as c_calc, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + odnServ);
                        sql2.Append("  then c_calc else 0 end) as c_calc_odn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append("  then c_sn else 0 end) as c_sn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + odnServ);
                        sql2.Append("  then c_calc else 0 end) as c_sn_odn, ");
                        sql2.Append(" sum(sum_charge) as sum_charge, max(cast (0 as decimal(14,2))) as pl_kvar ");
                        sql2.Append(" into  unlogged t1  from  " + sChargeAlias + ".charge_");
                        sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                        sql2.Append(" where nzp_serv in (" + prm.nzp_serv.ToString() + ", " + odnServ + ") ");
                        sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                        sql2.Append(" and dat_charge is null ");
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                        }
                        sql2.Append(" group by 1,2,3,4");
                    
#else
   sql2.Append(" select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, ");
                        sql2.Append(" sum(0) as nzp_measure,");
                        sql2.Append(" max(tarif) as tarif, max(0) as count_gil,");
                        sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                        sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k,");
                        sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,");
                        sql2.Append(" sum(case when nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append(" then 0 else rsum_tarif end) as sum_odn, ");
                        sql2.Append(" sum(case when nzp_serv=" + prm.nzp_serv.ToString() + " and tarif>0.0001 ");
                        sql2.Append(" then (reval + sum_nedop)/tarif else 0 end) as c_reval, ");
                        sql2.Append(" sum(case when nzp_serv=" + odnServ + " and tarif>0.0001 ");
                        sql2.Append(" then (reval + sum_nedop)/tarif else 0 end) as c_reval_odn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append("  then c_calc else 0 end) as c_calc, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + odnServ);
                        sql2.Append("  then c_calc else 0 end) as c_calc_odn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + prm.nzp_serv.ToString());
                        sql2.Append("  then c_sn else 0 end) as c_sn, ");
                        sql2.Append("  max(case when tarif>0 and nzp_serv=" + odnServ);
                        sql2.Append("  then c_calc else 0 end) as c_sn_odn, ");
                        sql2.Append(" sum(sum_charge) as sum_charge, max(cast (0 as decimal(14,2))) as pl_kvar ");
                        sql2.Append(" from  " + sChargeAlias + ":charge_");
                        sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                        sql2.Append(" where nzp_serv in (" + prm.nzp_serv.ToString() + ", " + odnServ + ") ");
                        sql2.Append(" and a.nzp_kvar=b.nzp_kvar ");
                        sql2.Append(" and dat_charge is null ");
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                        }
                        sql2.Append(" group by 1,2,3,4");
                        sql2.Append(" into temp t1 with no log");
#endif




                    }
                    else
                    {
                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, ");
                        sql2.Append(" sum(0) as nzp_measure, ");
                        sql2.Append(" max(tarif) as tarif, max(0) as count_gil,");
                        sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                        sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k,");
                        sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,");
                        sql2.Append(" sum(sum_charge) as sum_charge, max(case when tarif>0 then c_calc else 0 end) as c_calc, ");
                        sql2.Append(" max(case when tarif>0 then c_calc else 0 end) as c_sn,");
                        sql2.Append(" sum(case when tarif >0 then (sum_nedop+reval)/tarif else 0 end) as c_reval, ");
                        sql2.Append(" sum(0) as c_reval_odn, max(0) as c_calc_odn, ");
                        sql2.Append(" max(cast (0 as decimal(14,2))) as pl_kvar, sum(0) as sum_odn, sum(0) as c_sn_odn ");
                        sql2.Append("  into  unlogged t1  from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                        sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                        sql2.Append(" where nzp_serv=" + prm.nzp_serv.ToString() + " and a.nzp_kvar=b.nzp_kvar ");
                        sql2.Append(" and dat_charge is null  ");
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                        }
                        sql2.Append(" group by 1,2,3,4");
                        
#else
                        sql2.Append(" select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, ");
                        sql2.Append(" sum(0) as nzp_measure, ");
                        sql2.Append(" max(tarif) as tarif, max(0) as count_gil,");
                        sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                        sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k,");
                        sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) +");
                        sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d,");
                        sql2.Append(" sum(sum_charge) as sum_charge, max(case when tarif>0 then c_calc else 0 end) as c_calc, ");
                        sql2.Append(" max(case when tarif>0 then c_calc else 0 end) as c_sn,");
                        sql2.Append(" sum(case when tarif >0 then (sum_nedop+reval)/tarif else 0 end) as c_reval, ");
                        sql2.Append(" sum(0) as c_reval_odn, max(0) as c_calc_odn, ");
                        sql2.Append(" max(cast (0 as decimal(14,2))) as pl_kvar, sum(0) as sum_odn, sum(0) as c_sn_odn ");
                        sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                        sql2.Append(prm.month_.ToString("00") + " a, sel_kvar10 b");
                        sql2.Append(" where nzp_serv=" + prm.nzp_serv.ToString() + " and a.nzp_kvar=b.nzp_kvar ");
                        sql2.Append(" and dat_charge is null  ");
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql2.Append(" and a.nzp_supp = " + prm.nzp_key);
                        }
                        sql2.Append(" group by 1,2,3,4");
                        sql2.Append(" into temp t1 with no log");
#endif
                    }
                    ExecSQL(conn_db, sql2.ToString(), true);


                    string sql7 = " UPDATE t1 set reval_k = reval_k - coalesce((SELECT reval  from(SELECT nzp_dom, a.nzp_kvar, sum(sum_rcl) as reval from " + sChargeAlias + ".perekidka " +
                   " a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base where month_ = " +
                   prm.month_.ToString() + "  AND d.comment = 'Выравнивание сальдо' and nzp_serv in (" + prm.nzp_serv.ToString() + ", " + odnServ + ") group by 1,2) t " +
                   " where t1.nzp_dom = t.nzp_dom and t1.nzp_kvar = t.nzp_kvar), 0)";

                    if (!ExecSQL(conn_db, sql7.ToString(), true).result)
                        return null;

                    //rsh2
#if PG
                    string and = " ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        and = " and d.nzp_supp = " + prm.nzp_key;
                   
                    }
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set pl_kvar = ((select max(squ)  ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
                        + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and " + prm.nzp_serv + "=d.nzp_serv " +and);
                    sql2.Append(" )), ");

                    sql2.Append(" count_gil = ((  select max(gil) ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_"
                        + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and " + prm.nzp_serv + "=d.nzp_serv " + and);
                    sql2.Append(" ))");

                    ExecSQL(conn_db, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t1 set pl_kvar = (select max(pl_kvar) ");
                    sql2.Append(" from  sel_kvar10 d");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar ");
                    sql2.Append(" and isol=2 )");
                    sql2.Append(" where (" + prm.nzp_serv + " in (select nzp_serv from " + pref + "_kernel.s_counts where nzp_serv<>8) or ");
                    sql2.Append(" " + prm.nzp_serv + " in (select nzp_serv from " + pref + "_kernel.serv_odn where nzp_serv<>512)) and ");
                    sql2.Append(" nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ");
                    ExecSQL(conn_db, sql2.ToString(), true);
#else
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set (pl_kvar, count_gil) = ((select max(squ), max(gil) ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_"
                        + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and " + prm.nzp_serv + "=d.nzp_serv ");
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql2.Append(" and d.nzp_supp = " + prm.nzp_key);
                    }
                    sql2.Append(" ))");
                    ExecSQL(conn_db, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set pl_kvar = (select max(pl_kvar) ");
                    sql2.Append(" from  sel_kvar10 d");
                    sql2.Append(" where d.nzp_kvar=t1.nzp_kvar ");
                    sql2.Append(" and isol=2 )");
                    sql2.Append(" where (" + prm.nzp_serv + " in (select nzp_serv from " + pref + "_kernel:s_counts where nzp_serv<>8) or ");
                    sql2.Append(" " + prm.nzp_serv + " in (select nzp_serv from " + pref + "_kernel:serv_odn where nzp_serv<>512)) and ");
                    sql2.Append(" nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ");
                    ExecSQL(conn_db, sql2.ToString(), true);
#endif



                    #region Выборка перерасчетов прошлого периода
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min( extract ( year from (dat_s))*12+EXTRACT ( month from (dat_s))) as month_s," +
                                "  max(extract (year from (dat_po))*12+EXTRACT (month from (dat_po))) as month_po");
                    sql2.Append(" into unlogged t_nedop from " + pref + "_data.nedop_kvar a, sel_kvar10 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1,2,3   ");
#else
                 sql2.Append(" select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
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
#if PG
                    sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql2.Append(" and year_*12+month_>=month_s and  year_*12+month_<=month_po ");
                    sql2.Append(" group by 1,2");
#else
                  sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar ");
                    sql2.Append(" and year_*12+month_>=month_s and  year_*12+month_<=month_po ");
                    sql2.Append(" group by 1,2");
#endif
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
                        sql2.Append(" select nzp_geu, nzp_dom, b.nzp_kvar, 0, 0,  ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, t_nedop d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                        sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                        sql2.Append(" and nzp_serv in (" + prm.nzp_serv.ToString() + "," + odnServ + ")");
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
                    sql2.Append(" select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif) as tarif, ");
                    sql2.Append(" max(pl_kvar) as pl_kvar, max(count_gil) as count_gil  ");
                    sql2.Append(" into unlogged t2  from t1  where nzp_frm>0 ");
                    sql2.Append(" group by 1  ");
#else
                    sql2.Append(" select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif) as tarif, ");
                    sql2.Append(" max(pl_kvar) as pl_kvar, max(count_gil) as count_gil  ");
                    sql2.Append(" from t1  where nzp_frm>0 ");
                    sql2.Append(" group by 1 into temp t2 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set nzp_frm = (select max(nzp_frm) ");
              
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar),  ");

                    sql2.Append("  tarif = (select  ");
                    sql2.Append(" max(tarif) ");
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar), ");

                    sql2.Append("  pl_kvar = (select  ");
                    sql2.Append("  sum(pl_kvar) ");
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar), ");

                    sql2.Append("  count_gil = (select ");
                    sql2.Append(" sum(count_gil)");
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar) ");
#else
                sql2.Append(" update t1 set (nzp_frm, tarif, pl_kvar, count_gil) = ((select max(nzp_frm), ");
                    sql2.Append(" max(tarif), sum(pl_kvar) as pl_kvar, sum(count_gil) as count_gil ");
                    sql2.Append(" from t2 where t1.nzp_kvar=t2.nzp_kvar)) ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);
                    ExecSQL(conn_db, "drop table t2", false);

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

                    DateTime date = new DateTime(prm.year_, prm.month_, 01);
                    DateTime date_1 = date.AddMonths(1);
                    string date_s = date.ToString("dd.MM.yyyy");
                    string date_po = date_1.ToString("dd.MM.yyyy");


                    #region Вычисление норматива для человечьих услуг
                    if (humanServ)
                    {
                        ExecSQL(conn_db, "drop table t_norma", false);

                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" create unlogged table t_norma(nzp_kvar integer,");
                        sql2.Append(" nzp_serv integer, val_prm integer,  ");
                        sql2.Append(" tip_har integer, norma char(20), gaz decimal(14,2)) ;");
#else
                     sql2.Append(" create temp table t_norma(nzp_kvar integer,");
                        sql2.Append(" nzp_serv integer, val_prm integer,  ");
                        sql2.Append(" tip_har integer, norma char(20), gaz decimal(14,2)) with no log;");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка создания таблицы  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }



                        sql2.Remove(0, sql2.Length);
                        sql2.Append("select nzp_dom from sel_kvar10 group by 1; ");
                        if (!ExecRead(conn_db, out reader3, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка получения списка домов  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }
                        while (reader3.Read())
                        {
                            if (prm.nzp_serv == 6) //хвс 
                            {

                                sql2.Remove(0, sql2.Length);
#if PG
                                sql2.Append(" insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append("   Select  s.nzp_kvar, 6 as nzp_serv,  p.val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data.prm_1 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_kvar = p.nzp ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append("  and t.nzp_kvar=a.nzp_kvar");
                                sql2.Append(" and p.nzp_prm =7  ");
                                sql2.Append("and a.nzp_serv =6  ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#else
                  sql2.Append(" insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append("   Select  s.nzp_kvar, 6 as nzp_serv,  p.val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data:prm_1 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_kvar = p.nzp ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append("  and t.nzp_kvar=a.nzp_kvar");
                                sql2.Append(" and p.nzp_prm =7  ");
                                sql2.Append("and a.nzp_serv =6  ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#endif
                                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи  в таблицу  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                    conn_db.Close();
                                    ret.result = false;
                                    return null;
                                }
                            }

                            if (prm.nzp_serv == 7) //кан 
                            {

                                sql2.Remove(0, sql2.Length);
#if PG
                                sql2.Append(" insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select  s.nzp_kvar, a.nzp_serv, p.val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data.prm_1 p, t1 t,  sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_kvar = p.nzp ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and p.nzp_prm = 2007 ");
                                sql2.Append(" and t.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and a.nzp_serv =7 ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#else
                     sql2.Append(" insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select  s.nzp_kvar, a.nzp_serv, p.val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data:prm_1 p, t1 t,  sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_kvar = p.nzp ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and p.nzp_prm = 2007 ");
                                sql2.Append(" and t.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and a.nzp_serv =7 ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#endif
                                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи  в таблицу  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                    conn_db.Close();
                                    ret.result = false;
                                    return null;
                                }
                            }

                            if (prm.nzp_serv == 9) //гвс 
                            {

                                sql2.Remove(0, sql2.Length);
#if PG
                                sql2.Append("      insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select  s.nzp_kvar, a.nzp_serv, val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data.prm_2 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_serv=9   ");
                                sql2.Append(" and p.nzp_prm = 38  ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and a.nzp_kvar=t.nzp_kvar ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#else
             sql2.Append("      insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select  s.nzp_kvar, a.nzp_serv, val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data:prm_2 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_serv=9   ");
                                sql2.Append(" and p.nzp_prm = 38  ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and a.nzp_kvar=t.nzp_kvar ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#endif
                                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи  в таблицу  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                    conn_db.Close();
                                    ret.result = false;
                                    return null;
                                }
                            }

                            if (prm.nzp_serv == 14) //хвс бойлер
                            {

                                sql2.Remove(0, sql2.Length);
#if PG
                                sql2.Append("      insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select s.nzp_kvar, a.nzp_serv,  val_prm::int ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data.prm_2 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_serv=14   ");
                                sql2.Append(" and val_prm~E'[0-9][0-9 ]{19}'  "); 
                                sql2.Append(" and p.nzp_prm = 38  ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and a.nzp_kvar=t.nzp_kvar ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#else
                                sql2.Append("      insert into   t_norma(nzp_kvar, nzp_serv, val_prm) ");
                                sql2.Append(" Select s.nzp_kvar, a.nzp_serv,  val_prm ");
                                sql2.Append("From " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                                sql2.Append(prm.month_.ToString("00") + " a, " + pref + "_data:prm_2 p, t1 t, sel_kvar10  s  ");
                                sql2.Append(" Where a.nzp_serv=14   ");
                                sql2.Append(" and p.nzp_prm = 38  ");
                                sql2.Append(" and s.nzp_kvar=a.nzp_kvar ");
                                sql2.Append(" and s.nzp_dom=" + Convert.ToInt32(reader3["nzp_dom"]) + " ");
                                sql2.Append(" and a.nzp_kvar=t.nzp_kvar ");
                                sql2.Append(" and p.is_actual <> 100  ");
                                sql2.Append(" and p.dat_s  < '" + date_po + "'  ");
                                sql2.Append("  and p.dat_po >= '" + date_s + "'  ");
                                sql2.Append(" Group by 1,2,3; ");
#endif
                                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                                {
                                    MonitorLog.WriteLog("Ошибка записи  в таблицу  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                    conn_db.Close();
                                    ret.result = false;
                                    return null;
                                }
                            }



                        }
                        reader3.Close();
                        // --> nzp_res                                                                                         
                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append("  Update t_norma ");
                        sql2.Append(" Set tip_har = (Select val_prm::int val_prm From " + pref + "_data.prm_13 ");
                        sql2.Append(" Where nzp_prm = 172 and is_actual <> 100 and dat_s  <'" + date_po + "' and dat_po >='" + date_s + "')   ");
                        sql2.Append(" where nzp_serv in (6,7,9,14); ");
#else
                        sql2.Append("  Update t_norma ");
                        sql2.Append(" Set tip_har = (Select val_prm+0 val_prm From " + pref + "_data:prm_13 ");
                        sql2.Append(" Where nzp_prm = 172 and is_actual <> 100 and dat_s  <'" + date_po + "' and dat_po >='" + date_s + "')   ");
                        sql2.Append(" where nzp_serv in (6,7,9,14); ");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка обновления таблицы  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }

                        if (prm.nzp_serv == 6 || prm.nzp_serv == 7) //--хвс, кан
                        {

                            sql2.Remove(0, sql2.Length);
#if PG
                            sql2.Append(" Update t_norma  Set ");
                            sql2.Append(" norma = ( Select value From " + Points.Pref + "_kernel.res_values ");
                            sql2.Append("  Where nzp_res = tip_har ");
                            sql2.Append(" and nzp_y = val_prm ");
                            sql2.Append("      and nzp_x = (case when nzp_serv=6 then 1 else 3 end)  ) ");
                            sql2.Append("  Where  nzp_serv in (6,7) ");
                            sql2.Append("  and val_prm > 0 ");
                            sql2.Append(" and tip_har in ( Select nzp_res From " + Points.Pref + "_kernel.res_values ); ");
#else
                            sql2.Append(" Update t_norma  Set ");
                            sql2.Append(" norma = ( Select value From " + Points.Pref + "_kernel:res_values ");
                            sql2.Append("  Where nzp_res = tip_har ");
                            sql2.Append(" and nzp_y = val_prm ");
                            sql2.Append("      and nzp_x = (case when nzp_serv=6 then 1 else 3 end)  ) ");
                            sql2.Append("  Where  nzp_serv in (6,7) ");
                            sql2.Append("  and val_prm > 0 ");
                            sql2.Append(" and tip_har in ( Select nzp_res From " + Points.Pref + "_kernel:res_values ); ");
#endif
                            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка обновления таблицы  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }

                        }
                        if (prm.nzp_serv == 9 || prm.nzp_serv == 14 || prm.nzp_serv == 414) //гвс, хвс бойлер
                        {

                            sql2.Remove(0, sql2.Length);
#if PG
                            sql2.Append("   Update t_norma  Set  ");
                            sql2.Append(" norma = ( Select distinct value From " + Points.Pref + "_kernel.res_values ");
                            sql2.Append("  Where nzp_res = tip_har  ");
                            sql2.Append("  and nzp_y = val_prm  ");
                            sql2.Append("      and nzp_x = 2  )  ");
                            sql2.Append("  Where     nzp_serv in (9,14)   and val_prm > 0  ");
                            sql2.Append("  and tip_har in ( Select nzp_res From " + Points.Pref + "_kernel.res_values ); ");
#else
      sql2.Append("   Update t_norma  Set  ");
                            sql2.Append(" norma = ( Select value From " + Points.Pref + "_kernel:res_values ");
                            sql2.Append("  Where nzp_res = tip_har  ");
                            sql2.Append("  and nzp_y = val_prm  ");
                            sql2.Append("      and nzp_x = 2  )  ");
                            sql2.Append("  Where     nzp_serv in (9,14)   and val_prm > 0  ");
                            sql2.Append("  and tip_har in ( Select nzp_res From " + Points.Pref + "_kernel:res_values ); ");
#endif
                            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка обновления таблицы  " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }

                        }



                    }
                    #endregion

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set nzp_measure= coalesce((select max(nzp_measure) from " + pref + "_kernel.formuls f ");
                    sql2.Append(" where  t1.nzp_frm=f.nzp_frm),0) ");
#else
       sql2.Append(" update t1 set nzp_measure= nvl((select max(nzp_measure) from " + pref + "_kernel:formuls f ");
                    sql2.Append(" where  t1.nzp_frm=f.nzp_frm),0) ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t1 set tarif= coalesce((select max(tarif_hv_gv) from " + Points.Pref + "_data.a_trf_smr f ");
                    sql2.Append(" where  t1.nzp_frm=f.nzp_frm and is_priv=1 ),0) ");
                    sql2.Append(" where  nzp_frm in (select nzp_frm from  " + Points.Pref + "_data.a_trf_smr  ");
                    sql2.Append(" where  is_priv=1) and nzp_measure<>3 ");
#else
            sql2.Append(" update t1 set tarif= nvl((select max(tarif_hv_gv) from " + Points.Pref + "_data:a_trf_smr f ");
                    sql2.Append(" where  t1.nzp_frm=f.nzp_frm and is_priv=1 ),0) ");
                    sql2.Append(" where  nzp_frm in (select nzp_frm from  " + Points.Pref + "_data:a_trf_smr  ");
                    sql2.Append(" where  is_priv=1) and nzp_measure<>3 ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

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
#if PG
                    sql2.Append(" insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, ");
                    sql2.Append(" count_gil, rsum_tarif, tarif, ");
                    sql2.Append(" sum_nedop, c_nedop, reval_k, reval_d, c_reval_k, c_reval_d, sum_charge, ");
                    sql2.Append(" c_calc, c_sn, c_sn_odn, c_reval, c_calc_odn, ");
                    sql2.Append(" c_reval_odn, pl_kvar, sum_odn )");
                    sql2.Append(" select nzp_geu,  a.nzp_dom,  a.nzp_kvar, ");
                    sql2.Append(" trim(coalesce(name_frm,'Не определена формула'))||' . '||a.tarif as name_frm, ");
                    sql2.Append(" f.nzp_measure, max(count_gil), sum(rsum_tarif), ");
                    sql2.Append(" max(a.tarif) as tarif, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(case when a.tarif>0 then sum_nedop/a.tarif else 0 end) as c_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(case when a.tarif>0 then -1*reval_k/a.tarif else 0 end) as c_reval_k,");
                    sql2.Append(" sum(case when a.tarif>0 then reval_d/a.tarif else 0 end) as c_reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(c_calc) as c_calc, sum(c_sn) as c_sn,");
                    sql2.Append(" sum(c_sn_odn) as c_sn_odn, sum(c_reval) as c_reval, sum(c_calc_odn) as c_calc_odn, ");
                    sql2.Append(" sum(c_reval_odn) as c_reval_odn, max(pl_kvar) as pl_kvar, ");
                    sql2.Append(" sum(sum_odn) as sum_odn ");
                    sql2.Append(" from t1 a left outer join " + pref + "_kernel.formuls f ");
                    sql2.Append(" on a.nzp_frm=f.nzp_frm  where  abs(coalesce(sum_nedop,0))+abs(coalesce(a.tarif,0))+");
                    sql2.Append(" abs(coalesce(reval_k,0)) + abs(coalesce(reval_d,0))+abs(coalesce(sum_charge,0))+");
                    sql2.Append(" abs(coalesce(pl_kvar,0))>0.001 ");
                    sql2.Append(" group by 1,2,3,4,5");
#else
                    sql2.Append(" insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, ");
                    sql2.Append(" count_gil, rsum_tarif, tarif, ");
                    sql2.Append(" sum_nedop, c_nedop, reval_k, reval_d, c_reval_k, c_reval_d, sum_charge, ");
                    sql2.Append(" c_calc, c_sn, c_sn_odn, c_reval, c_calc_odn, ");
                    sql2.Append(" c_reval_odn, pl_kvar, sum_odn )");
                    sql2.Append(" select nzp_geu,  a.nzp_dom,  a.nzp_kvar, ");
                    sql2.Append(" trim(nvl(name_frm,'Не определена формула'))||' : '||a.tarif as name_frm, ");
                    sql2.Append(" f.nzp_measure, max(count_gil), sum(rsum_tarif), ");
                    sql2.Append(" max(a.tarif) as tarif, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(case when a.tarif>0 then sum_nedop/a.tarif else 0 end) as c_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(case when a.tarif>0 then -1*reval_k/a.tarif else 0 end) as c_reval_k,");
                    sql2.Append(" sum(case when a.tarif>0 then reval_d/a.tarif else 0 end) as c_reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(c_calc) as c_calc, sum(c_sn) as c_sn,");
                    sql2.Append(" sum(c_sn_odn) as c_sn_odn, sum(c_reval) as c_reval, sum(c_calc_odn) as c_calc_odn, ");
                    sql2.Append(" sum(c_reval_odn) as c_reval_odn, max(pl_kvar) as pl_kvar, ");
                    sql2.Append(" sum(sum_odn) as sum_odn ");
                    sql2.Append(" from t1 a, outer " + pref + "_kernel:formuls f ");
                    sql2.Append(" where a.nzp_frm=f.nzp_frm  and abs(nvl(sum_nedop,0))+abs(nvl(a.tarif,0))+");
                    sql2.Append(" abs(nvl(reval_k,0)) + abs(nvl(reval_d,0))+abs(nvl(sum_charge,0))+");
                    sql2.Append(" abs(nvl(pl_kvar,0))>0.001 ");
                    sql2.Append(" group by 1,2,3,4,5");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }



                    ExecSQL(conn_db, "drop table t1", true);
                    if (humanServ)
                        ExecSQL(conn_db, "drop table t_norma", true);


                }

            }
            reader.Close();

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_dom, nzp_kvar, nzp_geu  into unlogged  t_dom from t_svod group by 1,2,3   ; ");
#else
sql2.Append(" select nzp_dom, nzp_kvar, nzp_geu from t_svod group by 1,2,3 into temp t_dom with no log; ");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select name_frm into unlogged t_frm from t_svod group by 1   ; ");
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
            sql2.Append(" UPDATE t_svod SET sum_charge = coalesce(rsum_tarif, 0) - coalesce(sum_nedop, 0) + coalesce(reval_k, 0) + coalesce(reval_d, 0)");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select geu, ulica, ndom, Replace(nkor,'-','') as nkor, idom,  ");
            sql2.Append(" name_frm,sum(coalesce(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(case when coalesce(nzp_measure,0)=2 then coalesce(c_sn,0) else  coalesce(c_calc,0) end) as volume, ");
            sql2.Append(" sum(coalesce(rsum_tarif,0)) as rsum_tarif,  ");
#else
            sql2.Append(" select geu, ulica, ndom, Replace(nkor,'-','') as nkor, idom,  ");
            sql2.Append(" name_frm,sum(nvl(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(case when nvl(nzp_measure,0)=2 then nvl(c_sn,0) else  nvl(c_calc,0) end) as volume, ");
            sql2.Append(" sum(nvl(rsum_tarif,0)) as rsum_tarif,  ");
#endif
            if (prm.nzp_serv == 10)
            {
#if PG
                sql2.Append(" max(case when nzp_measure=2 then coalesce(a.tarif,0)/13 else coalesce(a.tarif,0) end) as tarif ,   ");
#else
       sql2.Append(" max(case when nzp_measure=2 then nvl(a.tarif,0)/13 else nvl(a.tarif,0) end) as tarif ,   ");
#endif
            }
            else
            {
#if PG
                sql2.Append(" max(coalesce(a.tarif,0)) as tarif ,");
#else
           sql2.Append(" max(nvl(a.tarif,0)) as tarif ,");
#endif
            }
#if PG
            sql2.Append(" max(coalesce(a.nzp_measure,0)) as nzp_measure,");
            sql2.Append(" sum(coalesce(sum_nedop,0)) as sum_nedop,  ");
            sql2.Append(" sum(-1*coalesce(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(coalesce(reval_d,0)) as reval_d,");
            sql2.Append(" sum(coalesce(c_nedop,0)) as c_nedop,  ");
            sql2.Append(" sum(coalesce(c_reval_k,0)) as c_reval_k, ");
            sql2.Append(" sum(coalesce(c_reval_d,0)) as c_reval_d,");
            sql2.Append(" sum(coalesce(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(coalesce(c_calc,0)) as c_calc, ");
            sql2.Append(" sum(coalesce(c_calc_odn,0)) as c_calc_odn, ");
            sql2.Append(" sum(coalesce(c_reval,0)) as c_reval, ");
            sql2.Append(" sum(coalesce(c_reval_odn,0)) as c_reval_odn, ");
            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar, ");
            sql2.Append(" sum(coalesce(sum_odn,0)) as sum_odn ");
            sql2.Append(" from t_svod a, " + tables.dom + " d, " + tables.ulica + " su, " + tables.geu + " sg ");
            sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul and a.nzp_geu=sg.nzp_geu ");
            sql2.Append(" group by  1,2,3,4,5,6");
            sql2.Append(" order by geu, ulica, idom, ndom, nkor, name_frm  ");
#else
    sql2.Append(" max(nvl(a.nzp_measure,0)) as nzp_measure,");
            sql2.Append(" sum(nvl(sum_nedop,0)) as sum_nedop,  ");
            sql2.Append(" sum(-1*nvl(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(nvl(reval_d,0)) as reval_d,");
            sql2.Append(" sum(nvl(c_nedop,0)) as c_nedop,  ");
            sql2.Append(" sum(nvl(c_reval_k,0)) as c_reval_k, ");
            sql2.Append(" sum(nvl(c_reval_d,0)) as c_reval_d,");
            sql2.Append(" sum(nvl(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(nvl(c_calc,0)) as c_calc, ");
            sql2.Append(" sum(nvl(c_calc_odn,0)) as c_calc_odn, ");
            sql2.Append(" sum(nvl(c_reval,0)) as c_reval, ");
            sql2.Append(" sum(nvl(c_reval_odn,0)) as c_reval_odn, ");
            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar, ");
            sql2.Append(" sum(nvl(sum_odn,0)) as sum_odn ");
            sql2.Append(" from t_svod a, " + tables.dom + " d, " + tables.ulica + " su, " + tables.geu + " sg ");
            sql2.Append(" where a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul and a.nzp_geu=sg.nzp_geu ");
            sql2.Append(" group by  1,2,3,4,5,6");
            sql2.Append(" order by geu, ulica, idom, ndom, nkor, name_frm  ");
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
            ExecSQL(conn_db, "drop table t_svod", true);

            conn_db.Close();




            reader.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }
    }
}
