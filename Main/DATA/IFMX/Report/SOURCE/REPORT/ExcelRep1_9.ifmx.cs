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

        public DataTable GetGroupSprav(Prm prm, out Returns ret, string Nzp_user)
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
            sql2.Append(" type_kv integer,");
            sql2.Append(" count_gil integer,");
            sql2.Append(" pl_isol Decimal(14,2), ");
            sql2.Append(" pl_komm Decimal(14,2), ");
            sql2.Append(" rsum_tarif Decimal(14,2), ");
            sql2.Append(" c_calc Decimal(14,2), ");
            sql2.Append(" sum_nedop Decimal(14,2), ");
            sql2.Append(" reval_k Decimal(14,2),");
            sql2.Append(" reval_d Decimal(14,2), ");
            sql2.Append(" sum_money Decimal(14,2), ");
            sql2.Append(" sum_charge Decimal(14,2))");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" type_kv integer,");
            sql2.Append(" count_gil integer,");
            sql2.Append(" pl_isol Decimal(14,2), ");
            sql2.Append(" pl_komm Decimal(14,2), ");
            sql2.Append(" rsum_tarif Decimal(14,2), ");
            sql2.Append(" c_calc Decimal(14,2), ");
            sql2.Append(" sum_nedop Decimal(14,2), ");
            sql2.Append(" reval_k Decimal(14,2),");
            sql2.Append(" reval_d Decimal(14,2), ");
            sql2.Append(" sum_money Decimal(14,2), ");
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

            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  t" + Nzp_user + "_spls group by 1");

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
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

                    #region Заполнение sel_kvar11
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create temp table sel_kvar11( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" pl_kom " + DBManager.sDecimalType + "(14,2),");
                    sql2.Append(" type_kv integer,");
                    sql2.Append(" isol integer,");
                    sql2.Append(" pl_calc " + DBManager.sDecimalType + "(14,2),");
                    sql2.Append(" count_gil integer)"+DBManager.sUnlogTempTable);
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into sel_kvar11( nzp_kvar, count_gil, type_kv, isol)");
                    sql2.Append(" select  nzp_kvar, 0, 0, 1 ");
                    sql2.Append(" from " + tXX_spls + " k ");
                    sql2.Append(" where pref='" + pref + "'");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("create index ix_tmpk_01 on sel_kvar11(nzp_kvar)");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);

                    sql2.Append(DBManager.sUpdStat + "  sel_kvar11 ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //коммунальные
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar11 set pl_kom=(select sum(val_prm" + DBManager.sConvToNum + ")");
                    sql2.Append(" from " + pref + DBManager.sDataAliasRest + "prm_1 where sel_kvar11.nzp_kvar=nzp and nzp_prm=6 and is_actual=1 ");
                    sql2.Append(" and dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + ")");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Изолированные /коммунальные
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar11 set isol=2");
                    sql2.Append(" where nzp_kvar in (select nzp from ");
                    sql2.Append(pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm=3 and val_prm='2' ");
                    sql2.Append(" and is_actual=1 and dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + ")");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar11 set type_kv=(select max(val_prm+0) ");
                    sql2.Append(" from " + pref + DBManager.sDataAliasRest + "prm_1 where sel_kvar11.nzp_kvar=nzp and nzp_prm=2009 and is_actual=1 ");
                    sql2.Append(" and dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + ")");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Временно потом из calc_gku
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar11 set pl_calc=(select max(squ) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar=sel_kvar11.nzp_kvar and nzp_serv=17  and a.tarif>0 ), ");
                    sql2.Append(" count_gil=(select max(gil) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar=sel_kvar11.nzp_kvar and nzp_serv=17  and a.tarif>0 ) ");
#else
                    sql2.Append(" update sel_kvar11 set (pl_calc, count_gil)=((select max(squ), max(gil) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar=sel_kvar11.nzp_kvar and nzp_serv=17  and a.tarif>0 ))");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    /////////////////
                    #endregion

                    //type_kv, isol, b.pl_kom , max(count_gil) as count_gil,
                    ExecSQL(conn_db, "drop table t21", false);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create temp table t21 (nzp_kvar integer, ");
                    sql2.Append(" rsum_tarif "+DBManager.sDecimalType+"(14,2),");
                    sql2.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2),");
                    sql2.Append(" reval_k " + DBManager.sDecimalType + "(14,2),");
                    sql2.Append(" reval_d " + DBManager.sDecimalType + "(14,2),");
                    sql2.Append(" sum_charge "+DBManager.sDecimalType+"(14,2),");
                    sql2.Append(" sum_money  " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable);
                    ExecSQL(conn_db, sql2.ToString(), true);
                    
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t21(nzp_kvar, rsum_tarif, sum_nedop, reval_k,reval_d, sum_charge, sum_money) ");
                    sql2.Append(" select a.nzp_kvar, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) + ");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) as reval_k,");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) +");
                    sql2.Append(" sum(case when real_charge>0 then real_charge else 0 end) as reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money  ");
                    sql2.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, sel_kvar11 b");
                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" and dat_charge is null  ");
                    sql2.Append(" group by 1 ");
                    ExecSQL(conn_db, sql2.ToString(), true);




                    #region Выборка перерасчетов прошлого периода
                    ExecSQL(conn_db, "drop table t_nedop", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
                    sql2.Append(" into unlogged t_nedop from " + pref + "_data.nedop_kvar a, sel_kvar11 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1");
#else
                    sql2.Append(" select a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql2.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar11 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1 into temp t_nedop with no log");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
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
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00"));
                    sql2.Append(".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql2.Append(" group by 1,2");
#else
                    sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00"));
                    sql2.Append(":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
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
                        sql2.Append(" insert into t21 (nzp_kvar, sum_nedop, reval_k, reval_d) ");
                        sql2.Append(" select  b.nzp_kvar, sum(sum_nedop-sum_nedop_p),  ");
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
                        sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001 and nzp_serv=17");
                        sql2.Append(" group by 1");
                        ExecSQL(conn_db, sql2.ToString(), true);
                    }
                    reader2.Close();

                    ExecSQL(conn_db, "drop table t_nedop", true);

                    #endregion


                    ExecSQL(conn_db, "drop table t1", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, type_kv, isol, b.pl_kom , max(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, max(pl_calc) as pl_calc, sum(sum_money) as sum_money ");
                    sql2.Append(" into unlogged t1 from  t21 a, sel_kvar11 b");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" group by 1,2,3,4 ");
#else
                    sql2.Append(" select a.nzp_kvar, type_kv, isol, b.pl_kom , max(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
                    sql2.Append(" sum(reval_k) as reval_k,");
                    sql2.Append(" sum(reval_d) as reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, max(pl_calc) as pl_calc, sum(sum_money) as sum_money ");
                    sql2.Append(" from  t21 a, sel_kvar11 b");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar ");
                    sql2.Append(" group by 1,2,3,4 ");
                    sql2.Append(" into temp t1 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table t21", true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(DBManager.sUpdStat + "  t1");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //По всем
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(type_kv, pl_isol, pl_komm, count_gil, rsum_tarif, ");
                    sql2.Append(" sum_nedop, reval_k, reval_d, sum_charge, sum_money )");
                    sql2.Append(" select type_kv, sum(case when isol=1 then pl_calc else 0 end), ");
                    sql2.Append(" sum(case when isol=2 then pl_calc else 0 end), sum(count_gil), sum(rsum_tarif),  ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  sum(reval_k) as reval_k, sum(reval_d) as reval_d,");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) ");
                    sql2.Append(" from t1 a ");
                    sql2.Append(" group by 1");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, " drop table sel_kvar11", true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" drop table t1");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                }

            }


#if PG
            string baseData = Points.Pref + "_data" + DBManager.getServer(conn_db);
            string baseKernel = Points.Pref + "_kernel" + DBManager.getServer(conn_db);
#else
            string baseData = Points.Pref + "_data" + "@" + DBManager.getServer(conn_db);
            string baseKernel = Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db);
#endif

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select name_y, ");
            sql2.Append(" sum(coalesce(pl_isol,0)) as pl_isol,sum(coalesce(pl_komm,0)) as pl_komm, sum(coalesce(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(coalesce(rsum_tarif,0)) as rsum_tarif,  ");
            sql2.Append(" sum(coalesce(sum_nedop,0)) as sum_nedop,  sum(-1*coalesce(reval_k,0)) as reval_k, sum(coalesce(reval_d,0)) as reval_d,");
            sql2.Append(" sum(0) as subsidy, sum(coalesce(sum_charge,0)) as sum_charge,  sum(coalesce(sum_money,0)) as sum_money ");
            sql2.Append(" from t_svod a, " + baseKernel + ".res_y r ");
            sql2.Append(" where a.type_kv=r.nzp_y and nzp_res=3001");
            sql2.Append(" group by  1 ");
            sql2.Append(" order by name_y  ");
#else
            sql2.Append(" select name_y, ");
            sql2.Append(" sum(nvl(pl_isol,0)) as pl_isol,sum(nvl(pl_komm,0)) as pl_komm, sum(nvl(count_gil,0)) as count_gil, ");
            sql2.Append(" sum(nvl(rsum_tarif,0)) as rsum_tarif,  ");
            sql2.Append(" sum(nvl(sum_nedop,0)) as sum_nedop,  sum(-1*nvl(reval_k,0)) as reval_k, sum(nvl(reval_d,0)) as reval_d,");
            sql2.Append(" sum(0) as subsidy, sum(nvl(sum_charge,0)) as sum_charge,  sum(nvl(sum_money,0)) as sum_money ");
            sql2.Append(" from t_svod a, " + baseKernel + ":res_y r ");
            sql2.Append(" where a.type_kv=r.nzp_y and nzp_res=3001");
            sql2.Append(" group by  1 ");
            sql2.Append(" order by name_y  ");
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
