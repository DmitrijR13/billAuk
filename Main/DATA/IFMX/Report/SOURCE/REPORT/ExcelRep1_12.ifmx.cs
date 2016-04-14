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
        private const string defaultPgSchema = "public";
        public DataTable GetDomNachReport(List<Prm> listprm, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


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

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();
            DataTable LocalTable = new DataTable();

            string pref = "";
#if PG
            string tXX_spdom = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spdom = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string dat = "01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString();
            //string db_fin;



            #region Подготовка


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return null;
            }

            ExecSQL(conn_db, "drop table t_seldom1", false);

            sql.Remove(0, sql.Length);
            sql.Append(" select nzp_dom, pref, nzp_kvar, ulica, ndom, idom ");
#if PG
            sql.Append("  into unlogged  t_seldom1 from  " + tXX_spdom + " where pref is not null");
#else
            sql.Append(" from  " + tXX_spdom + " where pref is not null");
#endif
            if (listprm[0].nzp_area > -1)
                sql2.Append(" and sp.nzp_area=" + listprm[0].nzp_area.ToString());
            if (listprm[0].nzp_geu > -1)
                sql2.Append(" and sp.nzp_geu=" + listprm[0].nzp_geu.ToString());
#if PG

#else
  sql.Append(" into temp t_seldom1 with no log");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            conn_web.Close();

            #region Создаем временные таблички
            ExecSQL(conn_db, " drop table t_svod ", false);
            ExecSQL(conn_db, " drop table t_nachdom1 ", false);
            ExecSQL(conn_db, " drop table t_servs ", false);
            ExecSQL(conn_db, " drop table t_nachdom ", false);
            ExecSQL(conn_db, " drop table t_temp_kvar ", false);



            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" nzp_dom integer,  ");
            sql2.Append(" pref char(20),  ");
            sql2.Append(" pl_kvar numeric(14,2)  default 0,  ");
            sql2.Append(" count_gil integer default 0,  ");
            sql2.Append(" count_ls integer  default 0)  ");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_dom integer,  ");
            sql2.Append(" pref char(20),  ");
            sql2.Append(" pl_kvar Decimal(14,2)  default 0,  ");
            sql2.Append(" count_gil integer default 0,  ");
            sql2.Append(" count_ls integer  default 0) with no log  ");
#endif

            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_nachdom( ");
            sql2.Append(" nzp_dom integer,  ");
            sql2.Append(" nzp_serv integer,  ");
            sql2.Append(" nzp_supp integer,  ");
            sql2.Append(" sum_insaldo numeric(14,2) default 0,  ");
            sql2.Append(" sum_insaldo_odn numeric(14,2) default 0,  ");
            sql2.Append(" rsum_tarif numeric(14,2) default 0,  ");
            sql2.Append(" sum_odn numeric(14,2) default 0,  ");
            sql2.Append(" sum_nedop numeric(14,2) default 0,  ");
            sql2.Append(" reval_k numeric(14,2) default 0,  ");
            sql2.Append(" reval_d numeric(14,2) default 0,  ");
            sql2.Append(" real_charge numeric(14,2) default 0,  ");
            sql2.Append(" sum_charge numeric(14,2) default 0, ");
            sql2.Append(" sum_charge_odn numeric(14,2) default 0, ");
            sql2.Append(" sum_money numeric(14,2) default 0, ");
            sql2.Append(" sum_money_odn numeric(14,2) default 0, ");
            sql2.Append(" sum_outsaldo_odn numeric(14,2) default 0, ");
            sql2.Append(" sum_outsaldo numeric(14,2) default 0)   ");
#else
            sql2.Append(" create temp table t_nachdom( ");
            sql2.Append(" nzp_dom integer,  ");
            sql2.Append(" nzp_serv integer,  ");
            sql2.Append(" nzp_supp integer,  ");
            sql2.Append(" sum_insaldo Decimal(14,2) default 0,  ");
            sql2.Append(" sum_insaldo_odn Decimal(14,2) default 0,  ");
            sql2.Append(" rsum_tarif Decimal(14,2) default 0,  ");
            sql2.Append(" sum_odn Decimal(14,2) default 0,  ");
            sql2.Append(" sum_nedop Decimal(14,2) default 0,  ");
            sql2.Append(" reval_k Decimal(14,2) default 0,  ");
            sql2.Append(" reval_d Decimal(14,2) default 0,  ");
            sql2.Append(" real_charge Decimal(14,2) default 0,  ");
            sql2.Append(" sum_charge Decimal(14,2) default 0, ");
            sql2.Append(" sum_charge_odn Decimal(14,2) default 0, ");
            sql2.Append(" sum_money Decimal(14,2) default 0, ");
            sql2.Append(" sum_money_odn Decimal(14,2) default 0, ");
            sql2.Append(" sum_outsaldo_odn Decimal(14,2) default 0, ");
            sql2.Append(" sum_outsaldo Decimal(14,2) default 0) with no log   ");
#endif
            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            #endregion


            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from t_seldom1  group by 1");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }



            while (reader.Read())
            {

                pref = reader["pref"].ToString().Trim();
                #region Заносим список домов
                sql2.Remove(0, sql2.Length);
                sql2.Append(" insert into t_svod( nzp_dom, pref) ");
                sql2.Append(" select nzp_dom, sp.pref ");
                sql2.Append(" from t_seldom1 sp  where sp.pref='" + pref + "' group by 1,2");

                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                #endregion

                #region Вычисляем площадь по дому
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update  t_svod  set pl_kvar= ");
                sql2.Append(" (select sum(Replace(p.val_prm, ',','.')::numeric) as pl_kvar ");
                sql2.Append(" from " + pref + "_data.kvar k, " + pref + "_data.prm_1 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=4 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#else
            sql2.Append(" update  t_svod  set pl_kvar= ");
                sql2.Append(" (select sum(Replace(p.val_prm, ',','.')+0) as pl_kvar ");
                sql2.Append(" from " + pref + "_data:kvar k, " + pref + "_data:prm_1 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=4 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#endif
                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                #region Вычисляем количество жильцов по дому
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_svod set count_gil = ");
                sql2.Append(" (select sum(val_prm::numeric) as pl_kvar ");
                sql2.Append(" from " + pref + "_data.kvar k, " + pref + "_data.prm_1 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=5 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#else
     sql2.Append(" update t_svod set count_gil = ");
                sql2.Append(" (select sum(val_prm+0) as pl_kvar ");
                sql2.Append(" from " + pref + "_data:kvar k, " + pref + "_data:prm_1 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=5 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#endif

                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                #region Вычисляем количество лицевых счетов
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_svod set count_ls = ");
                sql2.Append(" (select count(distinct k.num_ls) as pl_kvar ");
                sql2.Append(" from " + pref + "_data.kvar k, " + pref + "_data.prm_3 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=51 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "' and val_prm='1')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#else
     sql2.Append(" update t_svod set count_ls = ");
                sql2.Append(" (select count(unique k.num_ls) as pl_kvar ");
                sql2.Append(" from " + pref + "_data:kvar k, " + pref + "_data:prm_3 p ");
                sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm=51 and k.nzp_dom=t_svod.nzp_dom ");
                sql2.Append(" and is_actual=1 and dat_s<='" + dat + "' and dat_po>='" + dat + "' and val_prm='1')");
                sql2.Append(" where t_svod.pref='" + pref + "'");
#endif
                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                #region Подсчет начислений





#if PG
                string db_charge = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00");
                // if (DBManager.getServer(conn_db).Trim() != "") db_charge = db_charge  + DBManager.getServer(conn_db);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" insert into t_nachdom(nzp_dom, nzp_serv, nzp_supp, sum_insaldo, ");
                sql2.Append(" sum_charge, sum_odn, sum_money, sum_outsaldo, sum_nedop, rsum_tarif, ");
                sql2.Append(" real_charge,reval_k, reval_d, sum_insaldo_odn, sum_charge_odn, sum_money_odn, sum_outsaldo_odn)");
                sql2.Append(" select nzp_dom, nzp_serv, nzp_supp, sum(sum_insaldo), sum(rsum_tarif) + sum(reval) - sum(sum_nedop), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then  rsum_tarif else 0 end), ");
                sql2.Append(" sum(sum_money), sum(sum_outsaldo), sum(sum_nedop),");
                sql2.Append(" sum(rsum_tarif),");
                sql2.Append(" sum(izm_saldo) as real_charge,");
                sql2.Append(" sum(case when reval<0 then reval else 0 end ) +");
                sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end ) as reval_k,");
                sql2.Append(" sum(case when reval<0 then 0 else reval  end ) + ");
                sql2.Append(" sum(case when real_charge<0 then 0 else real_charge  end ) as reval_d,");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_insaldo else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_charge else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_money else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_outsaldo else 0 end) ");
                sql2.Append(" from  " + db_charge + ".charge_" + listprm[0].month_.ToString("00") + " d, t_seldom1 sp ");
                sql2.Append(" where d.nzp_kvar=sp.nzp_kvar ");
                sql2.Append(" and nzp_serv>1 and dat_charge is null");
                sql2.Append(" group by 1,2,3 ");
#else
                string db_charge = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00");
                if (DBManager.getServer(conn_db).Trim() != "") db_charge = db_charge + "@" + DBManager.getServer(conn_db);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" insert into t_nachdom(nzp_dom, nzp_serv, nzp_supp, sum_insaldo, ");
                sql2.Append(" sum_charge, sum_odn, sum_money, sum_outsaldo, sum_nedop, rsum_tarif, ");
                sql2.Append(" real_charge,reval_k, reval_d, sum_insaldo_odn, sum_charge_odn, sum_money_odn, sum_outsaldo_odn)");
                sql2.Append(" select nzp_dom, nzp_serv, nzp_supp, sum(sum_insaldo), sum(sum_charge), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then  rsum_tarif else 0 end), ");
                sql2.Append(" sum(sum_money), sum(sum_outsaldo), sum(sum_nedop),");
                sql2.Append(" sum(rsum_tarif),");
                sql2.Append(" sum(izm_saldo) as real_charge,");
                sql2.Append(" sum(case when reval<0 then reval else 0 end ) +");
                sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end ) as reval_k,");
                sql2.Append(" sum(case when reval<0 then 0 else reval  end ) + ");
                sql2.Append(" sum(case when real_charge<0 then 0 else real_charge  end ) as reval_d,");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_insaldo else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_charge else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_money else 0 end), ");
                sql2.Append(" sum(case when d.nzp_serv>509 and d.nzp_serv<518 then sum_outsaldo else 0 end) ");
                sql2.Append(" from  " + db_charge + ":charge_" + listprm[0].month_.ToString("00") + " d, t_seldom1 sp ");
                sql2.Append(" where d.nzp_kvar=sp.nzp_kvar " + (!listprm[0].isLoadParamInfo ? " and d.nzp_serv not in (206) " : ""));
                sql2.Append(" and nzp_serv>1 and dat_charge is null");
                sql2.Append(" group by 1,2,3 ");
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
                sql2.Append(" create temp table t_temp_kvar( ");
                sql2.Append(" nzp_dom integer,  ");
                sql2.Append(" nzp_serv integer,  ");
                sql2.Append(" sum_rcl numeric(14,2) default 0)  ");
                ExecSQL(conn_db, sql2.ToString(), true);


                sql2.Remove(0, sql2.Length);
                sql2.Append(" INSERT INTO t_temp_kvar");
                sql2.Append(" SELECT k.nzp_dom, p.nzp_serv, sum(p.sum_rcl) ");
                sql2.Append(" FROM  " + db_charge + ".perekidka p ");
                sql2.Append(" INNER JOIN bill01_data.kvar k on k.nzp_kvar = p.nzp_kvar ");
                sql2.Append(" where month_ = " + listprm[0].month_ + " group by 1,2");
                ExecSQL(conn_db, sql2.ToString(), true);


                sql2.Remove(0, sql2.Length);
                sql2.Append(" UPDATE t_nachdom SET sum_charge = sum_charge + ");
                sql2.Append(" coalesce((SELECT sum_rcl FROM  t_temp_kvar");
                sql2.Append(" where t_temp_kvar.nzp_dom = t_nachdom.nzp_dom and t_temp_kvar.nzp_serv = t_nachdom.nzp_serv), 0)");
                if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                //Жигулевск недопоставка как перекидка
                sql.Remove(0, sql.Length);
                sql.Append("insert into t_nachdom (nzp_dom, nzp_serv,  sum_nedop, reval_k ) ");
                sql.Append(" select  nzp_dom, nzp_serv, ");
                sql.Append(" -sum(sum_rcl) as vozv, ");
                sql.Append(" -sum(sum_rcl) as reval_k ");
                sql.Append(" from " + db_charge + DBManager.tableDelimiter + "perekidka a, t_seldom1 b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and type_rcl = 101 and month_=" + listprm[0].month_.ToString("00"));
                sql.Append(" and abs(sum_rcl)>0.001 ");
                sql.Append(" group by 1,2 ");
                ExecSQL(conn_db, sql.ToString(), true);


                #region Выборка перерасчетов прошлого периода
                ExecRead(conn_db, out reader2, "drop table t_nedop", false);
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" select nzp_dom, a.nzp_kvar, min(extract (year from (dat_s))*12+ extract ( month from (dat_s))) as month_s,  max( extract (year from(dat_po))*12+extract( month from (dat_po))) as month_po");
                sql2.Append(" into unlogged t_nedop  from " + pref + "_data.nedop_kvar a, t_seldom1 sp ");
                sql2.Append(" where a.nzp_kvar=sp.nzp_kvar and month_calc='01." + listprm[0].month_.ToString("00") + "." +
                    listprm[0].year_.ToString("0000") + "'  ");
                sql2.Append(" group by 1,2  ");
#else
                sql2.Append(" select nzp_dom, a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
                sql2.Append(" from " + pref + "_data:nedop_kvar a, t_seldom1 sp ");
                sql2.Append(" where a.nzp_kvar=sp.nzp_kvar and month_calc='01." + listprm[0].month_.ToString("00") + "." +
                    listprm[0].year_.ToString("0000") + "'  ");
                sql2.Append(" group by 1,2 into temp t_nedop with no log");
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
                sql2.Append(" from " + db_charge + ".lnk_charge_" + listprm[0].month_.ToString("00") + " b, t_nedop d ");
                sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                sql2.Append(" group by 1,2");
#else
         sql2.Append(" select month_, year_ ");
                sql2.Append(" from " + db_charge + ":lnk_charge_" + listprm[0].month_.ToString("00") + " b, t_nedop d ");
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
#if PG
                    sql2.Append(" insert into t_nachdom (nzp_dom,nzp_serv,  nzp_supp, sum_nedop,reval_k, reval_d) ");
                    sql2.Append(" select nzp_dom,nzp_serv, nzp_supp,");
                    sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                    sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                    sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                    sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                    sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                    sql2.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                    sql2.Append(" b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                    sql2.Append(listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + "')");
                    sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                    sql2.Append(" group by 1,2,3");
#else
                    sql2.Append(" insert into t_nachdom (nzp_dom,nzp_serv,  nzp_supp, sum_nedop,reval_k, reval_d) ");
                    sql2.Append(" select nzp_dom,nzp_serv, nzp_supp,");
                    sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                    sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                    sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                    sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                    sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                    sql2.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                    sql2.Append(" b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                    sql2.Append(listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + "')");
                    sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
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





                #endregion


            }
            if (reader != null) reader.Close();

            ExecSQL(conn_db, "create index ix_tmp_tnch_011 on t_nachdom(nzp_dom,nzp_supp,nzp_serv)", true);
#if PG
            ExecSQL(conn_db, "analyze t_nachdom", true);
#else
            ExecSQL(conn_db, "update statistics for table t_nachdom", true);
#endif


            ExecSQL(conn_db, "drop table t12", false);
            sql2.Remove(0, sql2.Length);
            sql2.Append(" select nzp_dom, nzp_supp  ");
#if PG
            sql2.Append(" into unlogged  t12 from t_nachdom ");
            sql2.Append(" where nzp_serv = 9 group by 1,2   ");
#else
    sql2.Append(" from t_nachdom ");
            sql2.Append(" where nzp_serv = 9 group by 1,2 into temp t12 with no log  ");
#endif
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" update t_nachdom set nzp_serv=9 ");
            sql2.Append(" where nzp_serv = 14 and 0<(select count(*) ");
            sql2.Append(" from t12 where t12.nzp_supp=t_nachdom.nzp_supp and t12.nzp_dom=t_nachdom.nzp_dom) ");
            ExecSQL(conn_db, sql2.ToString(), true);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" update t_nachdom set nzp_serv=513 ");
            sql2.Append(" where nzp_serv = 514 and 0<(select count(*) ");
            sql2.Append(" from t12 where t12.nzp_supp=t_nachdom.nzp_supp and t12.nzp_dom=t_nachdom.nzp_dom) ");
            ExecSQL(conn_db, sql2.ToString(), true);

            ExecSQL(conn_db, "drop table t12", true);


            #region Получение итоговых данных
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_serv ");
            sql2.Append(" into unlogged  t_servs  from  t_nachdom");
            sql2.Append(" group by 1  ");
#else
       sql2.Append(" select nzp_serv ");
            sql2.Append(" from  t_nachdom");
            sql2.Append(" group by 1 into temp t_servs with no log");
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
            sql2.Append(" insert into t_nachdom(nzp_dom, nzp_serv, sum_insaldo, rsum_tarif, sum_odn,");
            sql2.Append(" sum_charge, sum_money, sum_outsaldo, sum_nedop, reval_k, reval_d, real_charge,");
            sql2.Append(" sum_charge_odn, sum_insaldo_odn, sum_outsaldo_odn, sum_money_odn )");
            sql2.Append(" select nzp_dom, nzp_serv, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0");
            sql2.Append(" from  t_servs t, t_seldom1 sp group by 1,2 ");
            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            //Объединение услуг
            /*  if (listprm[0].service == "union")
              {
                  sql2.Remove(0, sql2.Length);
                  sql2.Append(" update t_nachdom set nzp_serv = (select max(nzp_serv_base) from " + Points.Pref + "_kernel:service_union ");
                  sql2.Append(" where nzp_serv_uni = nzp_serv)");
                  sql2.Append(" where nzp_serv in (select nzp_serv_uni from  " + pref + "_kernel:service_union )");
                  if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                  {
                      MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                      reader.Close();
                      conn_db.Close();
                      conn_web.Close();
                      ret.result = false;
                      return null;
                  }
              }*/

            sql2.Remove(0, sql2.Length);

#if PG
            sql2.Append(" select nzp_dom, nzp_serv, sum(sum_insaldo) as sum_insaldo,");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, ");
            sql2.Append(" sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop) as sum_nedop,");
            sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, sum(sum_odn) as sum_odn,   ");
            sql2.Append(" sum(sum_insaldo_odn) as sum_insaldo_odn, sum(sum_charge_odn) as sum_charge_odn, ");
            sql2.Append(" sum(sum_outsaldo_odn) as sum_outsaldo_odn,  sum(real_charge) as real_charge, ");
            sql2.Append(" sum(sum_money_odn)  as sum_money_odn ");
            sql2.Append(" into unlogged t_nachdom1  from  t_nachdom ");
            sql2.Append(" group by 1,2 ");

#else
            sql2.Append(" select nzp_dom, nzp_serv, sum(sum_insaldo) as sum_insaldo,");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, ");
            sql2.Append(" sum(sum_outsaldo) as sum_outsaldo, sum(sum_nedop) as sum_nedop,");
            sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, sum(sum_odn) as sum_odn,   ");
            sql2.Append(" sum(sum_insaldo_odn) as sum_insaldo_odn, sum(sum_charge_odn) as sum_charge_odn, ");
            sql2.Append(" sum(sum_outsaldo_odn) as sum_outsaldo_odn,  sum(real_charge) as real_charge, ");
            sql2.Append(" sum(sum_money_odn)  as sum_money_odn ");
            sql2.Append(" from  t_nachdom ");
            sql2.Append(" group by 1,2 ");
            sql2.Append(" into temp t_nachdom1 with no log");
#endif

            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion




            #region Вывод списка в таблицу
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select 1 as order_, a.nzp_serv, trim(ulica)||'@'||trim(ndom) as nzp_dom, coalesce(ulica,'') as ulica, ");
            sql2.Append(" coalesce(ndom, '') as ndom, coalesce(idom,0) as idom,   ");
            sql2.Append(" service, ");
            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar, sum(coalesce(count_gil,0)) as count_gil, sum(coalesce(count_ls,0)) as count_ls,  ");
            sql2.Append(" sum(coalesce(sum_insaldo,0)) as sum_insaldo, sum(coalesce(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(coalesce(sum_money,0)) as sum_money, sum(coalesce(sum_outsaldo,0)) as sum_outsaldo, sum(coalesce(rsum_tarif,0)) as rsum_tarif, ");
            sql2.Append(" sum(coalesce(sum_nedop,0)) as sum_nedop, sum(-1*coalesce(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(coalesce(reval_d,0)) as reval_d, sum(coalesce(sum_odn,0)) as sum_odn, sum(coalesce(sum_insaldo_odn,0)) as sum_insaldo_odn, ");
            sql2.Append(" sum(coalesce(sum_charge_odn,0)) as sum_charge_odn, sum(coalesce(sum_outsaldo_odn,0)) as sum_outsaldo_odn, ");
            sql2.Append(" sum(0) as real_charge,  sum(coalesce(sum_money_odn,0)) as sum_money_odn");
            sql2.Append(" from  " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica su ,t_svod b, ");
            sql2.Append(" t_nachdom1 a,  " + Points.Pref + "_kernel.services s ");
            sql2.Append(" where d.nzp_dom=b.nzp_dom and a.nzp_serv=s.nzp_serv and d.nzp_ul=su.nzp_ul and a.nzp_dom=b.nzp_dom ");
            sql2.Append(" group by 1,2,3,4,5,6,7");
            sql2.Append(" union all ");
            sql2.Append(" select 2 as order_,a.nzp_serv, '0' as nzp_dom, 'Итого ' as ulica, ' ' as ndom, 0 as idom, service,");
            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar, sum(coalesce(count_gil,0)) as count_gil, sum(coalesce(count_ls,0)) as count_ls,  ");
            sql2.Append(" sum(coalesce(sum_insaldo,0)) as sum_insaldo, sum(coalesce(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(coalesce(sum_money,0)) as sum_money, sum(coalesce(sum_outsaldo,0)) as sum_outsaldo, sum(coalesce(rsum_tarif,0)) as rsum_tarif, ");
            sql2.Append(" sum(coalesce(sum_nedop,0)) as sum_nedop, sum(-1*coalesce(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(coalesce(reval_d,0)) as reval_d, sum(coalesce(sum_odn,0)) as sum_odn, sum(coalesce(sum_insaldo_odn,0)) as sum_insaldo_odn, ");
            sql2.Append(" sum(coalesce(sum_charge_odn,0)) as sum_charge_odn, sum(coalesce(sum_outsaldo_odn,0)) as sum_outsaldo_odn, ");
            sql2.Append(" sum(0) as real_charge,  sum(coalesce(sum_money_odn,0)) as sum_money_odn");
            sql2.Append(" from  t_svod b, t_nachdom1 a,  " + Points.Pref + "_kernel.services s ");
            sql2.Append(" where a.nzp_dom=b.nzp_dom and a.nzp_serv=s.nzp_serv  ");
            sql2.Append(" group by 1,2,3,4,5,6,7");
            sql2.Append(" order by 1,4,6,5,3,7  ");
#else
        sql2.Append(" select 1 as order_, a.nzp_serv, trim(ulica)||'@'||trim(ndom) as nzp_dom, nvl(ulica,'') as ulica, ");
            sql2.Append(" nvl(ndom, '') as ndom, nvl(idom,0) as idom,   ");
            sql2.Append(" service, ");
            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar, sum(nvl(count_gil,0)) as count_gil, sum(nvl(count_ls,0)) as count_ls,  ");
            sql2.Append(" sum(nvl(sum_insaldo,0)) as sum_insaldo, sum(nvl(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(nvl(sum_money,0)) as sum_money, sum(nvl(sum_outsaldo,0)) as sum_outsaldo, sum(nvl(rsum_tarif,0)) as rsum_tarif, ");
            sql2.Append(" sum(nvl(sum_nedop,0)) as sum_nedop, sum(-1*nvl(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(nvl(reval_d,0)) as reval_d, sum(nvl(sum_odn,0)) as sum_odn, sum(nvl(sum_insaldo_odn,0)) as sum_insaldo_odn, ");
            sql2.Append(" sum(nvl(sum_charge_odn,0)) as sum_charge_odn, sum(nvl(sum_outsaldo_odn,0)) as sum_outsaldo_odn, ");
            sql2.Append(" sum(0) as real_charge,  sum(nvl(sum_money_odn,0)) as sum_money_odn");
            sql2.Append(" from  " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica su ,t_svod b, ");
            sql2.Append(" t_nachdom1 a,  " + Points.Pref + "_kernel:services s ");
            sql2.Append(" where d.nzp_dom=b.nzp_dom and a.nzp_serv=s.nzp_serv and d.nzp_ul=su.nzp_ul and a.nzp_dom=b.nzp_dom ");
            sql2.Append(" group by 1,2,3,4,5,6,7");
            sql2.Append(" union all ");
            sql2.Append(" select 2 as order_,a.nzp_serv, '0' as nzp_dom, 'Итого ' as ulica, ' ' as ndom, 0 as idom, service,");
            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar, sum(nvl(count_gil,0)) as count_gil, sum(nvl(count_ls,0)) as count_ls,  ");
            sql2.Append(" sum(nvl(sum_insaldo,0)) as sum_insaldo, sum(nvl(sum_charge,0)) as sum_charge, ");
            sql2.Append(" sum(nvl(sum_money,0)) as sum_money, sum(nvl(sum_outsaldo,0)) as sum_outsaldo, sum(nvl(rsum_tarif,0)) as rsum_tarif, ");
            sql2.Append(" sum(nvl(sum_nedop,0)) as sum_nedop, sum(-1*nvl(reval_k,0)) as reval_k, ");
            sql2.Append(" sum(nvl(reval_d,0)) as reval_d, sum(nvl(sum_odn,0)) as sum_odn, sum(nvl(sum_insaldo_odn,0)) as sum_insaldo_odn, ");
            sql2.Append(" sum(nvl(sum_charge_odn,0)) as sum_charge_odn, sum(nvl(sum_outsaldo_odn,0)) as sum_outsaldo_odn, ");
            sql2.Append(" sum(0) as real_charge,  sum(nvl(sum_money_odn,0)) as sum_money_odn");
            sql2.Append(" from  t_svod b, t_nachdom1 a,  " + Points.Pref + "_kernel:services s ");
            sql2.Append(" where a.nzp_dom=b.nzp_dom and a.nzp_serv=s.nzp_serv  ");
            sql2.Append(" group by 1,2,3,4,5,6,7");
            sql2.Append(" order by 1,4,6,5,3,7  ");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return null;
            }


            try
            {
                if (reader2 != null)
                {

                    try
                    {
                        //заполнение DataTable
                        //DataTable Data_Table = new DataTable();

                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;


                        LocalTable.Load(reader2, LoadOption.OverwriteChanges);

                        //if (LocalTable.Rows.Count == 0) LocalTable = Data_Table.Copy();
                        //else
                        //{
                        //    for (int i = 0; i < Data_Table.Rows.Count; i++)
                        //        LocalTable.ImportRow(Data_Table.Rows[i]);
                        //}

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        conn_db.Close();
                        return null;
                    }
                    if (reader2 != null) reader2.Close();

                    #region Удаление временных таблиц
                    ExecRead(conn_db, out reader2, "drop table t_seldom1 ", true);
                    ExecRead(conn_db, out reader2, "drop table t_svod ", true);
                    ExecRead(conn_db, out reader2, "drop table t_nachdom1 ", true);
                    ExecRead(conn_db, out reader2, "drop table t_nachdom ", true);
                    ExecRead(conn_db, out reader2, "drop table t_servs ", true);
                    ExecRead(conn_db, out reader2, "drop table t_temp_kvar", true);
                    #endregion


                    conn_db.Close();
                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! DomNach ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    conn_db.Close(); //закрыть соединение с основной базой     
                    conn_web.Close(); //закрыть соединение с основной базой     
                    return null;
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

                if (reader != null)
                {
                    reader.Close();
                }

                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            #endregion





            sql.Remove(0, sql.Length);
            reader.Close();
            conn_web.Close(); //закрыть соединение с основной базой     
            return LocalTable;


        }

        public DataTable GetSaldoReport10_14_3(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDataReader reader;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();
#if PG
            string tXX_spdom = defaultPgSchema + "." + "t" + Nzp_user + "_spdom";
#else
            string tXX_spdom = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spdom";
#endif
            string dat = "01." + prm_.month_.ToString("00") + "." + prm_.year_.ToString();
            conn_web.Close();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return null;
            }

            #endregion

            string where_serv = "";
            string where_supp = "";
            string where_adr = "";


            if (prm_.RolesVal != null)
            {
                if (prm_.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm_.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and d.nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and d.nzp_supp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_area)
                                where_adr += " and d.nzp_area in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_geu)
                                where_adr += " and d.nzp_geu in (" + role.val + ") ";

                        }
                    }
                }
            }

            string where_ls = "";
            if (prm_.dopParams != null && prm_.dopParams.Count != 0 && prm_.dopParams[0].name_prm == "ByLs" && prm_.dopParams[0].val_prm == "true")
            {
                string tXX_spls = "t" + Nzp_user + "_spls";
                where_ls = " and nzp_dom in (select distinct nzp_dom from " + tXX_spls + ") ";
            }

            #region Выборка статистики из готовых начислений

            sql.Remove(0, sql.Length);
            sql.Append(" select area, service, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo, ");
            sql.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql.Append(" sum(rsum_lgota) as rsum_lgota, ");
            sql.Append(" sum(sum_real) as sum_real, ");
            sql.Append(" sum(reval+real_charge) as reval, ");
            sql.Append(" sum(sum_nedop+sum_lgota-rsum_lgota) as sum_nedop, ");
            sql.Append(" sum(sum_nedop_p) as sum_last_nedop, ");
            sql.Append(" sum(sum_money) as sum_money, ");
            sql.Append(" sum(money_from) as money_from, ");
            sql.Append(" sum(money_del) as money_del, ");
            sql.Append(" sum(sum_outsaldo) as sum_outsaldo");
#if PG
            sql.Append(" from  " + Points.Pref + "_fin_" + (prm_.year_ - 2000).ToString("00") + ".fn_ukrgudom d ");
            sql.Append(" inner join " + Points.Pref + "_data.s_area sa on d.nzp_area=sa.nzp_area ");
            sql.Append(" inner join " + Points.Pref + "_kernel.services se on d.nzp_serv=se.nzp_serv ");
#else
            sql.Append(" from  " + Points.Pref + "_fin_" + (prm_.year_ - 2000).ToString("00") + ":fn_ukrgudom d, " +
                Points.Pref + "_data:s_area sa, ");
            sql.Append(Points.Pref + "_kernel:services se ");
#endif
            sql.Append(" where month_=" + prm_.month_.ToString("00"));
            sql.Append(where_serv);
            sql.Append(where_supp);
            sql.Append(where_adr);
            sql.Append(where_ls);
            sql.Append(" group by 1,2 order by 1,2 ");

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }


            try
            {
                if (reader != null)
                {

                    try
                    {
                        //заполнение DataTable


                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;


                        Data_Table.Load(reader, LoadOption.OverwriteChanges);

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        conn_db.Close();
                        conn_web.Close();
                        return null;
                    }
                    if (reader != null) reader.Close();
                    return Data_Table;


                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! DomNach ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    conn_db.Close(); //закрыть соединение с основной базой     
                    return null;
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

                if (reader != null)
                {
                    reader.Close();
                }

                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            #endregion


        }

        public DataTable GetSaldoReport10_14_1(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDataReader reader;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();

#if PG
            string tXX_spdom = defaultPgSchema + "." + "t" + Nzp_user + "_spdom";
#else
            string tXX_spdom = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spdom";
#endif
            string dat = "01." + prm_.month_.ToString("00") + "." + prm_.year_.ToString();
            conn_web.Close();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return null;
            }

            #endregion

            string where_serv = "";
            string where_supp = "";
            string where_adr = "";

            string where_ls = "";
            if (prm_.dopParams != null && prm_.dopParams.Count != 0 && prm_.dopParams[0].name_prm == "ByLs" && prm_.dopParams[0].val_prm == "true")
            {
                string tXX_spls = "t" + Nzp_user + "_spls";
                where_ls = " and nzp_dom in (select distinct nzp_dom from " + tXX_spls + ") ";
            }

            if (prm_.RolesVal != null)
            {
                if (prm_.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm_.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and d.nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and d.nzp_supp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_area)
                                where_adr += " and d.nzp_area in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_geu)
                                where_adr += " and d.nzp_geu in (" + role.val + ") ";


                        }
                    }
                }
            }



            #region Выборка статистики из готовых начислений

            sql.Remove(0, sql.Length);
            sql.Append(" select area, service, name_supp, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo, ");
            sql.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql.Append(" sum(rsum_lgota) as rsum_lgota, ");
            sql.Append(" sum(sum_real) as sum_real, ");
            sql.Append(" sum(reval+real_charge) as reval, ");
            sql.Append(" sum(sum_nedop+sum_lgota-rsum_lgota) as sum_nedop, ");
            sql.Append(" sum(sum_nedop_p) as sum_last_nedop, ");
            sql.Append(" sum(sum_money) as sum_money, ");
            sql.Append(" sum(money_from) as money_from, ");
            sql.Append(" sum(money_del) as money_del, ");
            sql.Append(" sum(sum_outsaldo) as sum_outsaldo");
            //sql.Append(" from  " + Points.Pref + "_fin_" + (prm_.year_ - 2000).ToString("00") + ":fn_ukrgudom d, " + tXX_spdom + " sp, ");
#if PG
            sql.Append(" from  " + Points.Pref + "_fin_" + (prm_.year_ - 2000).ToString("00") + ".fn_ukrgudom d, " +
            Points.Pref + "_data.s_area sa, ");
            sql.Append(Points.Pref + "_kernel.services se, " + Points.Pref + "_kernel.supplier su ");
#else
            sql.Append(" from  " + Points.Pref + "_fin_" + (prm_.year_ - 2000).ToString("00") + ":fn_ukrgudom d, " +
                Points.Pref + "_data:s_area sa, ");
            sql.Append(Points.Pref + "_kernel:services se, " + Points.Pref + "_kernel:supplier su ");
#endif
            sql.Append(" where d.nzp_area=sa.nzp_area and d.nzp_serv=se.nzp_serv and d.nzp_supp=su.nzp_supp ");
            sql.Append(" and month_=" + prm_.month_.ToString("00"));
            sql.Append(where_serv);
            sql.Append(where_supp);
            sql.Append(where_adr);
            sql.Append(where_ls);
            sql.Append(" group by 1,2,3 order by 1,2,3 ");

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }


            try
            {
                if (reader != null)
                {

                    try
                    {
                        //заполнение DataTable


                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;


                        Data_Table.Load(reader, LoadOption.OverwriteChanges);

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        conn_db.Close();
                        conn_web.Close();
                        return null;
                    }
                    if (reader != null) reader.Close();
                    return Data_Table;


                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! DomNach ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    conn_db.Close(); //закрыть соединение с основной базой     
                    return null;
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

                if (reader != null)
                {
                    reader.Close();
                }

                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            #endregion


        }

        public DataTable GetCharges(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

            ret = Utils.InitReturns();

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader;
            StringBuilder sql = new StringBuilder();
            DataTable Data_Table = new DataTable();
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            ExecSQL(conn_db, " Drop table t_charges ", false);
            ret = ExecSQL(conn_db,
                    " Create temp table t_charges " +
                    " ( name_supp  char(100), " +
                    "   sum_real numeric(14,2), " +
                    "   sum_charge numeric(14,2), " +
                    "   sum_money numeric(14,2), " +
                    "   typek char(100) " +
                    " ) With no log "
                    , true);
            if (!ret.result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            string where = "";
            if (prm_.RolesVal != null)
            {
                if (prm_.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm_.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_geu)
                                where += " and g.nzp_geu in (" + role.val + ") ";
                        }
                    }
                }
            }


            DateTime dt;
            DateTime.TryParse(prm_.dat_calc, out dt);
            DateTime dt2 = dt.AddMonths(1);
            foreach (_Point zap in Points.PointList)
            {
#if PG
                string t_charge = zap.pref + "_charge_" + (dt.Year - 2000).ToString("00") +   DBManager.getServer(conn_db) + ".charge_" + dt.Month.ToString("00");
                string t_charge2 = zap.pref + "_charge_" + (dt2.Year - 2000).ToString("00") +   DBManager.getServer(conn_db) + ".charge_" + dt2.Month.ToString("00");
                string t_kvar = zap.pref + "_data@" + DBManager.getServer(conn_db) + ". kvar";
                string t_supplier = zap.pref + "_kernel@" + DBManager.getServer(conn_db) + ".supplier";
#else
        string t_charge = zap.pref + "_charge_" + (dt.Year - 2000).ToString("00") + "@" + DBManager.getServer(conn_db) + ":charge_" + dt.Month.ToString("00");
                string t_charge2 = zap.pref + "_charge_" + (dt2.Year - 2000).ToString("00") + "@" + DBManager.getServer(conn_db) + ":charge_" + dt2.Month.ToString("00");
                string t_kvar = zap.pref + "_data@" + DBManager.getServer(conn_db) + ": kvar";
                string t_supplier = zap.pref + "_kernel@" + DBManager.getServer(conn_db) + ":supplier";
#endif
                if (!TempTableInWebCashe(conn_db, t_charge)) continue;

                #region Выборка
                sql.Remove(0, sql.Length);
                sql.Append("insert into t_charges (name_supp, typek,sum_real,sum_charge, sum_money) ");
                sql.Append(" select s.name_supp, (case when k.typek = 3 then 'Арендаторы' else 'Жилые квартиры' end) typek, sum(sum_real) sum_real, sum(sum_charge) sum_charge, 0 ");
                sql.Append(" from " + t_charge + " ch, " + t_kvar + " k, " + t_supplier + " s");
                sql.Append(" where ch.dat_charge is null and ch.nzp_serv > 1 ");
                sql.Append(" and s.nzp_supp = ch.nzp_supp and k.nzp_kvar = ch.nzp_kvar ");
                sql.Append(" group by 1, 2");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("insert into t_charges (name_supp, typek,sum_real,sum_charge, sum_money) ");
                sql.Append(" select s.name_supp, (case when k.typek = 3 then 'Арендаторы' else 'Жилые квартиры' end) typek, 0,0,  sum(sum_money) sum_money ");
                sql.Append(" from " + t_charge2 + " ch, " + t_kvar + " k, " + t_supplier + " s");
                sql.Append(" where ch.dat_charge is null and ch.nzp_serv > 1 ");
                sql.Append(" and s.nzp_supp = ch.nzp_supp and k.nzp_kvar = ch.nzp_kvar ");
                sql.Append(" group by 1, 2");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
            }
            ExecSQL(conn_db, " Create index ix_t_charges on t_charges (name_supp) ", true);
            ExecSQL(conn_db, " Update statistics for table t_charges ", true);

            sql.Remove(0, sql.Length);
            sql.Append("select name_supp,typek, sum(sum_real) sum_real, sum(sum_charge) sum_charge, sum(sum_money) sum_money from t_charges group by name_supp, typek order by typek, name_supp");
            if (!ExecRead(conn_db, out reader,
                sql.ToString()
                , true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }


            try
            {
                if (reader != null)
                {

                    try
                    {
                        //заполнение DataTable
                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;


                        Data_Table.Load(reader, LoadOption.OverwriteChanges);

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        conn_db.Close();
                        return null;
                    }
                    if (reader != null) reader.Close();
                    return Data_Table;


                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    conn_db.Close(); //закрыть соединение с основной базой     
                    return null;
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

                if (reader != null)
                {
                    reader.Close();
                }

                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
                #endregion


        }

     
        public DataTable GetSpravHasDolg(string nzp_kvar, string year_, string month_, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            DataTable Data_Table = new DataTable();
            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();

            try
            {


                #region Выборка по локальным банкам
                DataTable LocalTable = new DataTable();

                #region Получаем префикс базы данных
                #region Подключение к БД
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
                IDataReader reader;
                IDataReader reader2;

                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД " +
                            Constants.cons_Webdata, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" select pref ");
                sql.Append(" from  t" + Nzp_user + "_spls where nzp_kvar= " + nzp_kvar + "group by 1");

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                if (reader.Read() == false)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;

                }
                string pref = Convert.ToString(reader["pref"]).Trim();
                reader.Close();
                conn_web.Close();

                if (pref.Trim() == "")
                {
                    MonitorLog.WriteLog("Ошибка выборки префикса ЛС " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                #endregion


                #region Выборка лицевого счета
                IDbConnection conn_db;
                conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД " +
                                Constants.cons_Kernel, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }

                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append("select ulica, ndom, nkor, nkvar, nkvar_n, fio, pkod10, sum_insaldo, sum_charge, sum_money, sum_outsaldo ");
                sql2.Append("from " + pref + "_charge_" + (System.Convert.ToInt32(year_) - 2000).ToString("00") +
                            ".charge_" + System.Convert.ToInt32(month_).ToString("00") + " a,");
                sql2.Append(Points.Pref + "_data.kvar k, ");
                sql2.Append(Points.Pref + "_data.dom d, ");
                sql2.Append(Points.Pref + "_data.s_ulica su ");
                sql2.Append(" where a.nzp_kvar=k.nzp_kvar ");
                sql2.Append(" and d.nzp_dom=k.nzp_dom and d.nzp_ul=su.nzp_ul");
                sql2.Append(" and k.nzp_kvar=" + nzp_kvar);
                sql2.Append(" and nzp_serv=1");
#else
             sql2.Append("select ulica, ndom, nkor, nkvar, nkvar_n, fio, pkod10, sum_insaldo, sum_charge, sum_money, sum_outsaldo ");
                sql2.Append("from " + pref + "_charge_" + (System.Convert.ToInt32(year_) - 2000).ToString("00") +
                            ":charge_" + System.Convert.ToInt32(month_).ToString("00") + " a,");
                sql2.Append(Points.Pref + "_data:kvar k, ");
                sql2.Append(Points.Pref + "_data:dom d, ");
                sql2.Append(Points.Pref + "_data:s_ulica su ");
                sql2.Append(" where a.nzp_kvar=k.nzp_kvar ");
                sql2.Append(" and d.nzp_dom=k.nzp_dom and d.nzp_ul=su.nzp_ul");
                sql2.Append(" and k.nzp_kvar=" + nzp_kvar);
                sql2.Append(" and nzp_serv=1");
#endif

                if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                if (reader2 != null)
                {
                    try
                    {

                        //заполнение DataTable
                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                        culture.NumberFormat.NumberDecimalSeparator = ".";
                        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                        System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                        Data_Table.Load(reader2, LoadOption.OverwriteChanges);
                    }

                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        conn_db.Close();
                        return null;
                    }
                }

                if (reader2 != null) reader2.Close();
                conn_db.Close();
                #endregion

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }


            return Data_Table;

        }

        public List<Charge> GetLicChetData(Ls finder, int y, int m, out Returns ret)
        {
            ret = Utils.InitReturns();
            Utils.setCulture();
            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return null;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            //IDataReader reader2;
            List<Charge> Spis = new List<Charge>();



            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            StringBuilder sql1 = new StringBuilder();

            sql1.Append(" Select s.ordering,kv.num_ls, kv.fio, ");
            sql1.Append("  s.service, c.num_ls, ");
            sql1.Append(" c.tarif, c.sum_tarif, c.c_sn, c_calc, ");
            sql1.Append(" m.measure, c.sum_charge ");
            sql1.Append(" , c.sum_insaldo - sum_money as Dolg, s.nzp_serv  ");
            sql1.Append(" From ");
#if PG
            sql1.Append("  " + cur_pref + "_data. kvar kv, ");
            sql1.Append("  " + cur_pref + "_charge_" + (y - 2000).ToString() + " . charge_" + m.ToString("00") + " c, ");
            sql1.Append("  " + cur_pref + "_kernel . services s, ");
            sql1.Append("  " + cur_pref + "_kernel . formuls f, ");
            sql1.Append("  " + cur_pref + "_kernel . s_measure m ");
#else
            sql1.Append("  " + cur_pref + "_data: kvar kv, ");
            sql1.Append("  " + cur_pref + "_charge_" + (y - 2000).ToString() + " : charge_" + m.ToString("00") + " c, ");
            sql1.Append("  " + cur_pref + "_kernel : services s, ");
            sql1.Append("  " + cur_pref + "_kernel : formuls f, ");
            sql1.Append("  " + cur_pref + "_kernel : s_measure m ");
#endif
            sql1.Append(" Where  ");
            sql1.Append("  kv.nzp_kvar = c.nzp_kvar ");
            sql1.Append(" and c.nzp_serv = s.nzp_serv ");
            sql1.Append(" and c.nzp_frm = f.nzp_frm ");
            sql1.Append(" and f.nzp_measure = m.nzp_measure ");
            sql1.Append(" and kv.nzp_kvar = " + finder.nzp_kvar.ToString());
            sql1.Append(" and (c.tarif>0.001 or c.sum_charge>0.001 or abs(sum_insaldo)>0.01)");
            sql1.Append(" and c.dat_charge is null ");
            sql1.Append(" and c.nzp_serv>1 ");
            sql1.Append(" order by 1 ");

            if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {

                while (reader.Read())
                {
                    Charge zap = new Charge();

                    if (reader["fio"] != DBNull.Value) zap.fio = Convert.ToString(reader["fio"]);
                    if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);
                    if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]);
                    if (reader["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader["tarif"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["c_sn"] != DBNull.Value) zap.c_sn = Convert.ToDecimal(reader["c_sn"]);
                    if (reader["c_calc"] != DBNull.Value) zap.c_calc = Convert.ToDecimal(reader["c_calc"]);
                    if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    if (reader["dolg"] != DBNull.Value) zap.sum_pere = Convert.ToDecimal(reader["dolg"]);

                    Spis.Add(zap);


                }
                reader.Close();



                #region Объединение по service_uniona


                sql1.Remove(0, sql1.Length);
                sql1.Append(" Select service, s.nzp_serv_uni, s.nzp_serv_base ");
#if PG
                sql1.Append(" From " + cur_pref + "_kernel.service_union s," + cur_pref + "_kernel.services a");
#else
   sql1.Append(" From " + cur_pref + "_kernel:service_union s," + cur_pref + "_kernel:services a");
#endif

                sql1.Append(" where s.nzp_serv_base=a.nzp_serv");
                sql1.Append(" order by service, s.nzp_serv_base, s.nzp_serv_uni ");

                if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return Spis;
                }

                int nzp_serv_uni = 0;
                int nzp_serv_base = 0;
                string service = "";

                while (reader.Read())
                {
                    if (reader["nzp_serv_uni"] != DBNull.Value) nzp_serv_uni = Convert.ToInt32(reader["nzp_serv_uni"]);
                    if (reader["nzp_serv_base"] != DBNull.Value) nzp_serv_base = Convert.ToInt32(reader["nzp_serv_base"]);
                    if (reader["service"] != DBNull.Value) service = Convert.ToString(reader["service"]);

                    for (int i = 0; i <= Spis.Count - 1; i++)
                    {
                        if (Spis[i].nzp_serv == nzp_serv_uni)
                        {
                            Spis[i].nzp_serv = nzp_serv_base;
                            Spis[i].service = service;
                        }
                    }
                }
                reader.Close();
                conn_db.Close();

                for (int i = 0; i <= Spis.Count - 1; i++)
                {
                    Charge zap = Spis[i];
                    for (int j = i + 1; j <= Spis.Count - 1; j++)
                    {
                        if (zap.nzp_serv == Spis[j].nzp_serv)
                        {
                            if (zap.nzp_serv != 25)
                                zap.tarif = zap.tarif + Spis[j].tarif;
                            else
                                zap.tarif = Math.Max(zap.tarif, Spis[j].tarif);

                            zap.sum_tarif = zap.sum_tarif + Spis[j].sum_tarif;
                            zap.sum_charge = zap.sum_charge + Spis[j].sum_charge;
                            zap.sum_pere = zap.sum_pere + Spis[j].sum_pere;
                            Spis[j].nzp_serv = -1;
                        }
                    }
                    Spis[i] = zap;
                }

                int k = 0;
                while (k < Spis.Count)
                {
                    if (Spis[k].nzp_serv < 0)
                        Spis.RemoveAt(k);
                    else
                        k++;
                }

                #endregion



                return Spis;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }
        }

       

        public void CalcEnergoStat(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();



            StringBuilder pref = new StringBuilder(); ;
            StringBuilder chargeXX = new StringBuilder(); ;

            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();

            #region Создание таблицы в базе
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" set search_path to " + Points.Pref + "_fin_");
#else
    sql2.Append(" database " + Points.Pref + "_fin_");
#endif
            sql2.Append((prm_.year_ - 2000).ToString("00"));
            if (ExecRead(conn_db, out reader2, sql2.ToString(), true).result != true)
            {
                MonitorLog.WriteLog("Ошибка подключения к БД  " +
                                   Points.Pref + "_fin_" +
                                   (prm_.year_ - 2000).ToString("00"), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return;
            }



            int is_table = 0;
            sql2.Remove(0, sql2.Length);
#if PG
            string tb = "table_name";
            sql2.Append(" select count(*) as co from information_schema.tables where table_schema = 'fgub_fin_13' and " + tb + " = 'fn_energo'");
#else
 sql2.Append(" select count(*) as co from systables where lower(tabname) = 'fn_energo'");
#endif
            if (ExecRead(conn_db, out reader2, sql2.ToString(), true).result != true)
            {
                MonitorLog.WriteLog("Ошибка поиска таблицы fn_energo в " +
                                   Points.Pref + "_fin_" +
                                   (prm_.year_ - 2000).ToString("00"), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return;
            }
            if (reader2.Read())
            {
                if (reader2["co"] != DBNull.Value) is_table = Convert.ToInt32(reader2["co"]);
            }
            reader2.Close();
            if (is_table == 0)
            {
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create  table fn_energo (     ");
                sql2.Append(" nzp_fne Serial(1),            ");
                sql2.Append(" nzp_area Integer,             ");
                sql2.Append(" month_ Integer,               ");
                sql2.Append(" typek Integer,                ");
                sql2.Append(" nzp_geu Integer,              ");
                sql2.Append(" nzp_dom Integer,              ");
                sql2.Append(" nzp_serv Integer,             ");
                sql2.Append(" nzp_supp Integer,             ");
                sql2.Append(" is_elplit Integer default 0,            ");
                sql2.Append(" sum_money numeric(14,2),      ");
                sql2.Append(" rsum_tarif numeric(14,2),     ");
                sql2.Append(" sum_nedop numeric(14,2),      ");
                sql2.Append(" reval numeric(14,2),          ");
                sql2.Append(" real_charge numeric(14,2),    ");
                sql2.Append(" sum_charge numeric(14,2),     ");
                sql2.Append(" sum_insaldo numeric(14,2),    ");
                sql2.Append(" sum_insaldo_d numeric(14,2),  ");
                sql2.Append(" sum_insaldo_k numeric(14,2),  ");
                sql2.Append(" sum_outsaldo numeric(14,2),   ");
                sql2.Append(" sum_outsaldo_d numeric(14,2), ");
                sql2.Append(" sum_outsaldo_k numeric(14,2), ");
                sql2.Append(" c_calc numeric(14,4),         ");
                sql2.Append(" c_reval numeric(14,4),        ");
                sql2.Append(" gkal_calc numeric(14,4),      ");
                sql2.Append(" gkal_reval numeric(14,4),     ");
                sql2.Append(" gkal numeric(14,2))           ");
                if (ExecRead(conn_db, out reader2, sql2.ToString(), true).result != true)
                {
                    MonitorLog.WriteLog("Ошибка создания таблицы fn_energo в " +
                                       Points.Pref + "_fin_" +
                                       (prm_.year_ - 2000).ToString("00"), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return;
                }

                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_00 on fn_energo(nzp_fne)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_01 on fn_energo(nzp_area)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_02 on fn_energo(nzp_geu)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_03 on fn_energo(nzp_supp)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_04 on fn_energo(nzp_serv)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_05 on fn_energo(nzp_dom)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_06 on fn_energo(month_)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_07 on fn_energo(typek)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);
                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index ix_fne_08 on fn_energo(is_elplit)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

            }




            #endregion

            #region Ограничения
            string where_wp = "";
            string where_supp = "";
            string where_serv = "";
            string where_adr = "";

            if (prm_.RolesVal != null)
            {
                if (prm_.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm_.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and nzp_supp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_wp)
                                where_wp += " and nzp_wp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_area)
                                where_adr += " and nzp_area in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_geu)
                                where_adr += " and nzp_geu in (" + role.val + ") ";


                        }
                    }
                }
            }
            if (prm_.nzp_geu > 0) where_adr += " and nzp_geu=" + prm_.nzp_geu.ToString();
            if (prm_.nzp_area > 0) where_adr += " and nzp_area=" + prm_.nzp_area.ToString();
            if (prm_.nzp_dom > 0) where_adr += " and nzp_dom=" + prm_.nzp_dom.ToString();
#if PG
            if (prm_.nzp_ul > 0) where_adr += " and nzp_dom in (select nzp_dom from " +
                Points.Pref + "_data"   +
                DBManager.getServer(conn_db) + ".dom where nzp_ul=" + prm_.nzp_ul.ToString() + ")";
#else
            if (prm_.nzp_ul > 0) where_adr += " and nzp_dom in (select nzp_dom from " +
                Points.Pref + "_data" + "@" +
                DBManager.getServer(conn_db) + ":dom where nzp_ul=" + prm_.nzp_ul.ToString() + ")";
#endif
            #endregion

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select * ");
            sql.Append(" from  " + Points.Pref + "_kernel" +  DBManager.getServer(conn_db) + ".s_point ");
            sql.Append(" where nzp_wp>1 " + where_wp);
#else
          sql.Append(" select * ");
            sql.Append(" from  " + Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":s_point ");
            sql.Append(" where nzp_wp>1 " + where_wp);
#endif

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return;
            }
            while (reader.Read())
            {
                if (reader["bd_kernel"] != null)
                {
                    pref.Remove(0, pref.Length);
                    pref.Append(Convert.ToString(reader["bd_kernel"]).Trim());
                    chargeXX.Remove(0, chargeXX.Length);
#if PG
                    chargeXX.Append(pref + "_charge_" + (prm_.year_ - 2000).ToString("00") +
                                       ".charge_" + prm_.month_.ToString("00"));
#else
     chargeXX.Append(pref + "_charge_" + (prm_.year_ - 2000).ToString("00") +
                        ":charge_" + prm_.month_.ToString("00"));
#endif

                    ExecRead(conn_db, out reader2, "drop table t_ensvod ", false);

                    #region создание временной таблицы
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create unlogged table t_ensvod (     ");
                    sql2.Append(" nzp_area integer,          ");
                    sql2.Append(" nzp_geu integer,           ");
                    sql2.Append(" typek integer,             ");
                    sql2.Append(" nzp_serv integer,          ");
                    sql2.Append(" nzp_supp integer,          ");
                    sql2.Append(" nzp_dom integer,           ");
                    sql2.Append(" tarif_gkal numeric(14,2),  ");
                    sql2.Append(" sum_insaldo numeric(14,2), ");
                    sql2.Append(" sum_insaldo_d numeric(14,2), ");
                    sql2.Append(" sum_insaldo_k numeric(14,2), ");
                    sql2.Append(" sum_outsaldo numeric(14,2),   ");
                    sql2.Append(" sum_outsaldo_d numeric(14,2),");
                    sql2.Append(" sum_outsaldo_k numeric(14,2),");
                    sql2.Append(" rsum_tarif numeric(14,2),   ");
                    sql2.Append(" sum_nedop numeric(14,2),    ");
                    sql2.Append(" reval numeric(14,2),        ");
                    sql2.Append(" real_charge numeric(14,2),  ");
                    sql2.Append(" sum_money numeric(14,2),    ");
                    sql2.Append(" c_calc  numeric(14,4),      ");
                    sql2.Append(" c_reval  numeric(14,4))  ");
#else
                    sql2.Append(" create temp table t_ensvod (     ");
                    sql2.Append(" nzp_area integer,          ");
                    sql2.Append(" nzp_geu integer,           ");
                    sql2.Append(" typek integer,             ");
                    sql2.Append(" nzp_serv integer,          ");
                    sql2.Append(" nzp_supp integer,          ");
                    sql2.Append(" nzp_dom integer,           ");
                    sql2.Append(" tarif_gkal Decimal(14,2),  ");
                    sql2.Append(" sum_insaldo Decimal(14,2), ");
                    sql2.Append(" sum_insaldo_d Decimal(14,2), ");
                    sql2.Append(" sum_insaldo_k Decimal(14,2), ");
                    sql2.Append(" sum_outsaldo Decimal(14,2),   ");
                    sql2.Append(" sum_outsaldo_d Decimal(14,2),");
                    sql2.Append(" sum_outsaldo_k Decimal(14,2),");
                    sql2.Append(" rsum_tarif Decimal(14,2),   ");
                    sql2.Append(" sum_nedop Decimal(14,2),    ");
                    sql2.Append(" reval Decimal(14,2),        ");
                    sql2.Append(" real_charge Decimal(14,2),  ");
                    sql2.Append(" sum_money Decimal(14,2),    ");
                    sql2.Append(" c_calc  Decimal(14,4),      ");
                    sql2.Append(" c_reval  Decimal(14,4)) with no log ");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    #endregion

                    #region Выборка данных
                    string dat_calc = "01." + prm_.month_.ToString() + "." + prm_.year_.ToString();
                    #region Подсчет цены гигакаллорий по поставщикам
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" Create unlogged table tmp_supp (nzp_supp integer, ");
                    sql2.Append(" tarif_gvs Decimal(14,2))           ");
#else
        sql2.Append(" Create temp table tmp_supp (nzp_supp integer, ");
                    sql2.Append(" tarif_gvs Decimal(14,2)) with no log          ");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка создания таблицы " +
                            sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into tmp_supp (nzp_supp)     ");
                    sql2.Append(" select nzp_supp                     ");
#if PG
                    sql2.Append(" from " + pref + "_kernel.supplier   where nzp_supp>0 " + where_supp);
#else
               sql2.Append(" from " + pref + "_kernel:supplier   where nzp_supp>0 " + where_supp);
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка создания таблицы " +
                            sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }

                    bool is_supp = false;
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select val_prm from " + pref + "_data.prm_5 ");
                    sql2.Append(" where nzp_prm=336 and is_actual<>100        ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              ");
#else
                  sql2.Append(" select val_prm from " + pref + "_data:prm_5 ");
                    sql2.Append(" where nzp_prm=336 and is_actual<>100        ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              ");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }
                    if (reader2.Read())
                    {
                        if (reader2["val_prm"] != DBNull.Value)
                            if (Convert.ToInt32(reader2["val_prm"]) == 1) is_supp = true;
                    }
                    reader2.Close();


                    sql2.Remove(0, sql2.Length);
                    if (is_supp == true)
                    {
#if PG
                        sql2.Append("  update tmp_supp set tarif_gvs=             ");
                        sql2.Append(" (select Max((val_prm::numeric) )   ");
                        sql2.Append(" from " + pref + "_data.prm_11 a             ");
                        sql2.Append(" where nzp_prm=339 and is_actual<>100        ");
                        sql2.Append("    and dat_s<='" + dat_calc + "'            ");
                        sql2.Append("    and dat_po>='" + dat_calc + "'           ");
                        sql2.Append("    and tmp_supp.nzp_supp=a.nzp )            ");
#else
           sql2.Append("  update tmp_supp set tarif_gvs=             ");
                        sql2.Append(" (select Max(Replace(val_prm,',','.') + 0)   ");
                        sql2.Append(" from " + pref + "_data:prm_11 a             ");
                        sql2.Append(" where nzp_prm=339 and is_actual<>100        ");
                        sql2.Append("    and dat_s<='" + dat_calc + "'            ");
                        sql2.Append("    and dat_po>='" + dat_calc + "'           ");
                        sql2.Append("    and tmp_supp.nzp_supp=a.nzp )            ");
#endif
                        if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            conn_db.Close();
                            ret.result = false;
                            return;
                        }

                    }
                    else
                    {
#if PG
                        sql2.Append(" update tmp_supp set tarif_gvs=              ");
                        sql2.Append(" (select Max(val_prm:numeric)   ");
                        sql2.Append("  from " + pref + "_data.prm_5 a              ");
                        sql2.Append("  where nzp_prm=252 and is_actual<>100       ");
                        sql2.Append("    and dat_s<='" + dat_calc + "'            ");
                        sql2.Append("    and dat_po>='" + dat_calc + "'           ");
                        sql2.Append("    and tmp_supp.nzp_supp=a.nzp )            ");
#else
         sql2.Append(" update tmp_supp set tarif_gvs=              ");
                        sql2.Append(" (select Max(Replace(val_prm,',','.') + 0)   ");
                        sql2.Append("  from " + pref + "_data:prm_5 a              ");
                        sql2.Append("  where nzp_prm=252 and is_actual<>100       ");
                        sql2.Append("    and dat_s<='" + dat_calc + "'            ");
                        sql2.Append("    and dat_po>='" + dat_calc + "'           ");
                        sql2.Append("    and tmp_supp.nzp_supp=a.nzp )            ");
#endif
                        if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            reader.Close();
                            conn_db.Close();
                            ret.result = false;
                            return;
                        }

                    }
                    #endregion

                    #region Подсчет цены гигакаллорий по ЛС
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" Create unlogged table tmp_kvar (                  ");
                    sql2.Append(" nzp_area integer,                             ");
                    sql2.Append(" nzp_geu integer,                             ");
                    sql2.Append(" nzp_dom integer,                             ");
                    sql2.Append(" nzp_kvar integer,                             ");
                    sql2.Append(" typek integer,                                ");
                    sql2.Append(" tarif_gvs Decimal(14,2),                      ");
                    sql2.Append(" tarif_otop Decimal(14,2) )          ");
#else
                    sql2.Append(" Create temp table tmp_kvar (                  ");
                    sql2.Append(" nzp_area integer,                             ");
                    sql2.Append(" nzp_geu integer,                             ");
                    sql2.Append(" nzp_dom integer,                             ");
                    sql2.Append(" nzp_kvar integer,                             ");
                    sql2.Append(" typek integer,                                ");
                    sql2.Append(" tarif_gvs Decimal(14,2),                      ");
                    sql2.Append(" tarif_otop Decimal(14,2) )with no log         ");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка создания таблицы " +
                            sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into tmp_kvar (typek, nzp_area, nzp_geu, nzp_dom, nzp_kvar, tarif_gvs)     ");
                    sql2.Append(" select typek, nzp_area, nzp_geu, nzp_dom, nzp, val_prm::numeric  ");
                    sql2.Append(" from " + pref + "_data.kvar k, " + pref + "_data.prm_1 p  ");
                    sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm = 340 and is_actual = 1 ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and k.nzp_dom=" + prm_.nzp_dom.ToString() + "           ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              " + where_adr);
#else
                    sql2.Append(" insert into tmp_kvar (typek, nzp_area, nzp_geu, nzp_dom, nzp_kvar, tarif_gvs)     ");
                    sql2.Append(" select typek, nzp_area, nzp_geu, nzp_dom, nzp, val_prm + 0                    ");
                    sql2.Append(" from " + pref + "_data:kvar k, " + pref + "_data:prm_1 p  ");
                    sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm = 340 and is_actual = 1 ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and k.nzp_dom=" + prm_.nzp_dom.ToString() + "           ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              " + where_adr);
#endif

                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы " +
                            sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into tmp_kvar (typek, nzp_area, nzp_geu, nzp_kvar, tarif_otop) ");
                    sql2.Append(" select typek, nzp_area, nzp_geu, nzp, val_prm::numeric ");
                    sql2.Append(" from " + pref + "_data.kvar k, " + pref + "_data.prm_1 p ");
                    sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm = 341 and is_actual = 1       ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and k.nzp_dom=" + prm_.nzp_dom.ToString() + "           ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              " + where_adr);
#else
                    sql2.Append(" insert into tmp_kvar (typek, nzp_area, nzp_geu, nzp_kvar, tarif_otop) ");
                    sql2.Append(" select typek, nzp_area, nzp_geu, nzp, val_prm                         ");
                    sql2.Append(" from " + pref + "_data:kvar k, " + pref + "_data:prm_1 p  ");
                    sql2.Append(" where k.nzp_kvar=p.nzp and nzp_prm = 341 and is_actual = 1       ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and k.nzp_dom=" + prm_.nzp_dom.ToString() + "           ");
                    sql2.Append(" and dat_s<='" + dat_calc + "'               ");
                    sql2.Append(" and dat_po>='" + dat_calc + "'              " + where_adr);
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы " +
                            sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                    }


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create index ixtmp_01 on tmp_kvar(nzp_kvar)    ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create index ixtmp_02 on tmp_supp(nzp_supp)    ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" analyze tmp_kvar    ");
#else
   sql2.Append(" update statistics for table tmp_kvar    ");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" analyze tmp_supp    ");
#else
      sql2.Append(" update statistics for table tmp_supp    ");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    #endregion

                    #region горячая вода
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_ensvod (nzp_area, nzp_geu, nzp_dom, typek, nzp_serv, nzp_supp, tarif_gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval ) ");
                    sql2.Append(" select nzp_area, nzp_geu, nzp_dom, ");
                    sql2.Append(" typek, nzp_serv, a.nzp_supp, tarif_gvs, ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then sum_insaldo else 0 end),   ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then 0 else sum_insaldo end),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end),  ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then 0 else sum_outsaldo end),  ");
                    sql2.Append(" sum(rsum_tarif),                  ");
                    sql2.Append(" sum(sum_nedop),                   ");
                    sql2.Append(" sum(reval) ,                      ");
                    sql2.Append(" sum(real_charge),                 ");
                    sql2.Append(" sum(sum_money),                    ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval           ");
                    sql2.Append(" from " + chargeXX + " a,          ");
#if PG
                    sql2.Append(pref + "_data.kvar t, tmp_supp  su        ");
#else
     sql2.Append(pref + "_data:kvar t, tmp_supp  su        ");
#endif
                    sql2.Append(" where a.num_ls = t.num_ls         ");
                    sql2.Append(" and a.nzp_serv = 9                ");
                    sql2.Append(" and a.nzp_supp = su.nzp_supp      ");
                    sql2.Append(" and dat_charge is null            ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and t.nzp_dom=" + prm_.nzp_dom.ToString());
                    sql2.Append(" and a.nzp_kvar not in (select nzp_kvar ");
                    sql2.Append(" from tmp_kvar where tarif_gvs is not null) " + where_serv);
                    sql2.Append(" group by 1,2,3,4,5,6,7            ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_ensvod (nzp_area, nzp_geu, nzp_dom, typek, nzp_serv, nzp_supp, tarif_gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval ) ");
                    sql2.Append(" select nzp_area, nzp_geu, ");
                    sql2.Append(" nzp_dom, typek, nzp_serv, ");
                    sql2.Append(" nzp_supp, tarif_gvs, ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then sum_insaldo else 0 end),   ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then 0 else sum_insaldo end),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end),  ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then 0 else sum_outsaldo end),  ");
                    sql2.Append(" sum(rsum_tarif),      ");
                    sql2.Append(" sum(sum_nedop),       ");
                    sql2.Append(" sum(reval) ,          ");
                    sql2.Append(" sum(real_charge),     ");
                    sql2.Append(" sum(sum_money),       ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval           ");
                    sql2.Append(" from " + chargeXX + " a,  tmp_kvar t  ");
                    sql2.Append(" where a.nzp_kvar = t.nzp_kvar         ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and t.nzp_dom=" + prm_.nzp_dom.ToString());
                    sql2.Append(" and a.nzp_serv = 9 and tarif_gvs is not null  ");
                    sql2.Append(" and dat_charge is null " + where_serv + where_supp);
                    sql2.Append(" group by 1,2,3,4,5,6,7            ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    #endregion

                    #region отопление
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_ensvod (nzp_area,  nzp_geu, nzp_dom, typek, nzp_serv, nzp_supp, tarif_gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval ) ");
                    sql2.Append(" select nzp_area,  ");
                    sql2.Append(" nzp_geu, nzp_dom, typek, nzp_serv, ");
                    sql2.Append(" a.nzp_supp, tarif_gvs, ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then sum_insaldo else 0 end),   ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then 0 else sum_insaldo end),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end),  ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then 0 else sum_outsaldo end),  ");
                    sql2.Append(" sum(rsum_tarif),      ");
                    sql2.Append(" sum(sum_nedop),       ");
                    sql2.Append(" sum(reval) ,          ");
                    sql2.Append(" sum(real_charge),     ");
                    sql2.Append(" sum(sum_money),       ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval           ");
                    sql2.Append(" from " + chargeXX + " a,          ");
#if PG
                    sql2.Append(pref + "_data.kvar t, tmp_supp  su  ");
#else
 sql2.Append(pref + "_data:kvar t, tmp_supp  su  ");
#endif
                    sql2.Append(" where a.num_ls = t.num_ls         ");
                    sql2.Append(" and a.nzp_serv = 8                ");
                    sql2.Append(" and a.nzp_supp = su.nzp_supp      ");
                    sql2.Append(" and dat_charge is null            ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and t.nzp_dom=" + prm_.nzp_dom.ToString());
                    sql2.Append(" and a.nzp_kvar not in (select nzp_kvar ");
                    sql2.Append(" from tmp_kvar where tarif_otop is not null) " + where_serv);
                    sql2.Append(" group by 1,2,3,4,5,6,7            ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_ensvod (nzp_area, nzp_geu, nzp_dom,  typek, nzp_serv, nzp_supp, tarif_gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval ) ");
                    sql2.Append(" select nzp_area, nzp_geu, nzp_dom, ");
                    sql2.Append(" typek, nzp_serv, nzp_supp, tarif_otop, ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then sum_insaldo else 0 end),   ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then 0 else sum_insaldo end),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end),  ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then 0 else sum_outsaldo end),  ");
                    sql2.Append(" sum(rsum_tarif),                  ");
                    sql2.Append(" sum(sum_nedop),                   ");
                    sql2.Append(" sum(reval) ,                      ");
                    sql2.Append(" sum(real_charge),                 ");
                    sql2.Append(" sum(sum_money),                   ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval           ");
                    sql2.Append(" from " + chargeXX + " a,  tmp_kvar t  ");
                    sql2.Append(" where a.nzp_kvar = t.nzp_kvar         ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and t.nzp_dom=" + prm_.nzp_dom.ToString());
                    sql2.Append(" and a.nzp_serv = 8 and tarif_otop is not null  ");
                    sql2.Append(" and dat_charge is null " + where_serv + where_supp);
                    sql2.Append(" group by 1,2,3,4,5,6,7            ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                    #endregion

                    #region Электроснабжение
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_ensvod (nzp_area,  nzp_geu, nzp_dom, typek, nzp_serv, nzp_supp, tarif_gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval ) ");
                    sql2.Append(" select nzp_area,  nzp_geu, ");
                    sql2.Append(" nzp_dom, typek, nzp_serv, a.nzp_supp,  0, ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then sum_insaldo else 0 end),   ");
                    sql2.Append(" sum(case when sum_insaldo > 0 then 0 else sum_insaldo end),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end),  ");
                    sql2.Append(" sum(case when sum_outsaldo > 0 then 0 else sum_outsaldo end),  ");
                    sql2.Append(" sum(rsum_tarif),      ");
                    sql2.Append(" sum(sum_nedop),       ");
                    sql2.Append(" sum(reval) ,          ");
                    sql2.Append(" sum(real_charge),     ");
                    sql2.Append(" sum(sum_money),       ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval           ");
                    sql2.Append(" from " + chargeXX + " a,          ");
#if PG
                    sql2.Append(pref + "_data.kvar t, tmp_supp su   ");
#else
  sql2.Append(pref + "_data:kvar t, tmp_supp su   ");
#endif
                    sql2.Append(" where a.num_ls = t.num_ls         ");
                    if (prm_.nzp_dom > 0)
                        sql2.Append(" and t.nzp_dom=" + prm_.nzp_dom.ToString());
                    sql2.Append(" and a.nzp_serv  in (11,25,210 )    ");
                    sql2.Append(" and a.nzp_supp = su.nzp_supp      ");
                    sql2.Append(" and dat_charge is null            " + where_serv);
                    sql2.Append(" group by 1,2,3,4,5,6,7            ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    #endregion

                    #region Удаление предыдущего значения
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" delete from " + Points.Pref + "_fin_");
#if PG
                    sql2.Append((prm_.year_ - 2000).ToString("00") );
                    sql2.Append(DBManager.getServer(conn_db) + ".fn_energo ");
#else
                 sql2.Append((prm_.year_ - 2000).ToString("00") + "@");
                    sql2.Append(DBManager.getServer(conn_db) + ":fn_energo ");
#endif
                    sql2.Append(" where month_ = " + prm_.month_.ToString());
                    sql2.Append(" and nzp_dom in (select nzp_dom from t_ensvod)  ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    #endregion

                    #region Выборка в таблицу
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into " + Points.Pref + "_fin_");
#if PG
                    sql2.Append((prm_.year_ - 2000).ToString("00") );
                    sql2.Append(DBManager.getServer(conn_db) + ".fn_energo(");
#else
                    sql2.Append((prm_.year_ - 2000).ToString("00") + "@");
                    sql2.Append(DBManager.getServer(conn_db) + ":fn_energo(");
#endif
                    sql2.Append(" month_, nzp_area,  nzp_geu, nzp_dom, typek, nzp_serv, nzp_supp, gkal, ");
                    sql2.Append(" sum_insaldo, sum_insaldo_d, sum_insaldo_k,");
                    sql2.Append(" sum_outsaldo, sum_outsaldo_d, sum_outsaldo_k,");
                    sql2.Append(" rsum_tarif, sum_nedop, reval, real_charge, ");
                    sql2.Append(" sum_money, c_calc, c_reval, gkal_calc, gkal_reval )  ");
                    sql2.Append(" select " + prm_.month_.ToString() + ",nzp_area,  nzp_geu, ");
                    sql2.Append(" nzp_dom, typek, nzp_serv, nzp_supp, tarif_gkal,  ");
                    sql2.Append(" sum(sum_insaldo),     ");
                    sql2.Append(" sum(sum_insaldo_d),   ");
                    sql2.Append(" sum(sum_insaldo_k),   ");
                    sql2.Append(" sum(sum_outsaldo),    ");
                    sql2.Append(" sum(sum_outsaldo_d),  ");
                    sql2.Append(" sum(sum_outsaldo_k),  ");
                    sql2.Append(" sum(rsum_tarif),      ");
                    sql2.Append(" sum(sum_nedop),       ");
                    sql2.Append(" sum(reval) ,          ");
                    sql2.Append(" sum(real_charge),     ");
                    sql2.Append(" sum(sum_money),       ");
                    sql2.Append(" sum(c_calc) as c_calc,            ");
                    sql2.Append(" sum(c_reval) as c_reval,          ");
                    sql2.Append(" sum(case when coalesce(tarif_gkal,0) = 0 ");
                    sql2.Append(" then 0 else (rsum_tarif-sum_nedop)/tarif_gkal end) as gkal_calc,");
                    sql2.Append(" sum(case when coalesce(tarif_gkal,0) = 0 ");
                    sql2.Append(" then 0 else reval/tarif_gkal end) as gkal_reval");
                    sql2.Append(" from t_ensvod a ");
                    sql2.Append(" group by 1,2,3,4,5,6,7,8        ");




                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return;
                    }

                    #endregion


                    #region Удаляем временные таблички
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" drop table tmp_kvar ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" drop table tmp_supp ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" drop table t_ensvod ");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    #endregion

                    #endregion
                }

            }
            reader.Close();

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" set search_path to " + Points.Pref + "_fin_");
#else
       sql2.Append(" database " + Points.Pref + "_fin_");
#endif
            sql2.Append((prm_.year_ - 2000).ToString("00"));
            if (ExecRead(conn_db, out reader2, sql2.ToString(), true).result == true)
            {
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" analyze fn_energo");
#else
                sql2.Append(" update statistics for table fn_energo");
#endif
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

            }

            conn_db.Close();

            #endregion


        }

        public DataTable GetEnergoActSverki(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();

            //  if (prm_.dopprm.Trim() != "")
            // {
            
            CalcEnergoStat(prm_, out ret, Nzp_user);
            //}

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();


            #region создание временной таблицы
            ExecRead(conn_db, out reader, " drop table t_ensvod ", false);
            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" create unlogged table t_ensvod (     ");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" typek integer default 0,   ");
            sql.Append(" nzp_serv integer default 0, ");
            sql.Append(" sum_insaldo numeric(14,2) default 0, ");
            sql.Append(" sum_tarif numeric(14,2) default 0,   ");
            sql.Append(" reval numeric(14,2) default 0,       ");
            sql.Append(" real_charge numeric(14,2) default 0, ");
            sql.Append(" sum_money numeric(14,2) default 0,   ");
            sql.Append(" c_calc  numeric(14,4) default 0,     ");
            sql.Append(" c_reval numeric(14,4) default 0,     ");
            sql.Append(" gkal_calc numeric(14,4) default 0,   ");
            sql.Append(" gkal_reval numeric(14,4) default 0,  ");
            sql.Append(" sum_in  numeric(14,2) default 0,     ");
            sql.Append(" sum_rasp numeric(14,2) default 0,    ");
            sql.Append(" sum_allrasp numeric(14,2)default 0)  ");
#else
            sql.Append(" create temp table t_ensvod (     ");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" typek integer default 0,   ");
            sql.Append(" nzp_serv integer default 0, ");
            sql.Append(" sum_insaldo Decimal(14,2) default 0, ");
            sql.Append(" sum_tarif Decimal(14,2) default 0,   ");
            sql.Append(" reval Decimal(14,2) default 0,       ");
            sql.Append(" real_charge Decimal(14,2) default 0, ");
            sql.Append(" sum_money Decimal(14,2) default 0,   ");
            sql.Append(" c_calc  Decimal(14,4) default 0,     ");
            sql.Append(" c_reval Decimal(14,4) default 0,     ");
            sql.Append(" gkal_calc Decimal(14,4) default 0,   ");
            sql.Append(" gkal_reval Decimal(14,4) default 0,  ");
            sql.Append(" sum_in  Decimal(14,2) default 0,     ");
            sql.Append(" sum_rasp Decimal(14,2) default 0,    ");
            sql.Append(" sum_allrasp Decimal(14,2)default 0) with no log ");
#endif
            if (ExecRead(conn_db, out reader, sql.ToString(), true).result != true)
            {
                MonitorLog.WriteLog("Ошибка создания таблицы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion


            #region Ограничения
            string where_wp = "";
            string where_supp = "";
            string where_serv = "";
            string where_area = "";
            string where_geu = "";
            string where_dom = "";

            if (prm_.RolesVal != null)
            {
                if (prm_.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in prm_.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and nzp_supp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_wp)
                                where_wp += " and nzp_wp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_area)
                                where_area += " and nzp_area in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_geu)
                                where_geu += " and nzp_geu in (" + role.val + ") ";


                        }
                    }
                }
            }
            if (prm_.nzp_geu > 0) where_geu += "and nzp_geu=" + prm_.nzp_geu.ToString();
            if (prm_.nzp_area > 0) where_area += "and nzp_area=" + prm_.nzp_area.ToString();
            if (prm_.nzp_dom > 0) where_dom += "and nzp_dom=" + prm_.nzp_dom.ToString();
#if PG
            if (prm_.nzp_ul > 0) where_dom += "and nzp_dom in (select nzp_dom from " +
                        Points.Pref + "_data" +  
                        DBManager.getServer(conn_db) + ".dom where nzp_ul=" + prm_.nzp_ul.ToString() + ")";
#else
    if (prm_.nzp_ul > 0) where_dom += "and nzp_dom in (select nzp_dom from " +
                Points.Pref + "_data" + "@" +
                DBManager.getServer(conn_db) + ":dom where nzp_ul=" + prm_.nzp_ul.ToString() + ")";
#endif

            #endregion


            #region финансовая информация
            if ((where_dom == "") & (where_geu == ""))
            {
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" insert into t_ensvod (nzp_area, typek, nzp_serv,  ");
                sql.Append(" sum_in, sum_rasp) ");
                sql.Append(" select nzp_area, ");
                sql.Append(" (case when nzp_area = 4000 then 3 else 1 end ) as ");
                sql.Append(" typek, nzp_serv,              ");
                sql.Append(" sum(sum_in) as sum_in,          ");
                sql.Append(" sum(sum_rasp) as sum_rasp      ");
                sql.Append(" from " + Points.Pref + "_fin_");
                sql.Append((prm_.year_ - 2000).ToString("00") );
                sql.Append(DBManager.getServer(conn_db) + ".fn_distrib_" + prm_.month_.ToString("00") + " a  ");
                sql.Append(" where nzp_serv in (8,9,11, 25, 210)  " + where_serv + where_area);
                sql.Append(" group by 1,2,3 ");
#else
                sql.Append(" insert into t_ensvod (nzp_area, typek, nzp_serv,  ");
                sql.Append(" sum_in, sum_rasp) ");
                sql.Append(" select nzp_area, ");
                sql.Append(" (case when nzp_area = 4000 then 3 else 1 end ) as ");
                sql.Append(" typek, nzp_serv,              ");
                sql.Append(" sum(sum_in) as sum_in,          ");
                sql.Append(" sum(sum_rasp) as sum_rasp      ");
                sql.Append(" from " + Points.Pref + "_fin_");
                sql.Append((prm_.year_ - 2000).ToString("00") + "@");
                sql.Append(DBManager.getServer(conn_db) + ":fn_distrib_" + prm_.month_.ToString("00") + " a  ");
                sql.Append(" where nzp_serv in (8,9,11, 25, 210)  " + where_serv + where_area);
                sql.Append(" group by 1,2,3 ");
#endif
                if (ExecRead(conn_db, out reader, sql.ToString(), true).result != true)
                {
                    MonitorLog.WriteLog("Ошибка SQL " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
            }
            #endregion

            #region Информация по начислениям
            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_ensvod (nzp_area,  typek, nzp_serv, ");
            sql.Append(" sum_insaldo, sum_tarif,                          ");
            sql.Append(" reval, real_charge, sum_money, c_calc, c_reval,  ");
            sql.Append(" gkal_calc, gkal_reval)            ");
            sql.Append(" select nzp_area,  typek, nzp_serv, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo,   ");
            sql.Append(" sum(rsum_tarif-sum_nedop) as sum_tarif,      ");
            sql.Append(" sum(reval) as reval,               ");
            sql.Append(" sum(real_charge) as real_charge,   ");
            sql.Append(" sum(sum_money) as sum_money,       ");
            sql.Append(" sum(c_calc) as c_calc,             ");
            sql.Append(" sum(c_reval) as c_reval,           ");
            sql.Append(" sum(gkal_calc) as gkal_calc,       ");
            sql.Append(" sum(gkal_reval) as gkal_reval      ");
            sql.Append(" from " + Points.Pref + "_fin_");
#if PG
            sql.Append((prm_.year_ - 2000).ToString("00") );
            sql.Append(DBManager.getServer(conn_db) + ".fn_energo a       ");
#else
        sql.Append((prm_.year_ - 2000).ToString("00") + "@");
            sql.Append(DBManager.getServer(conn_db) + ":fn_energo a       ");
#endif
            sql.Append(" where month_ = " + prm_.month_.ToString() + where_serv + where_area + where_geu + where_supp + where_dom);
            sql.Append(" group by 1,2,3                 ");
            if (ExecRead(conn_db, out reader, sql.ToString(), true).result != true)
            {
                MonitorLog.WriteLog("Ошибка SQL " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            #endregion

            #region Выборка на экран
            sql.Remove(0, sql.Length);
            sql.Append(" select 1 as ord1,3 as ord2, area, typek, service,       ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo,  ");
            sql.Append(" sum(sum_tarif) as sum_tarif,      ");
            sql.Append(" sum(reval) as reval,              ");
            sql.Append(" sum(real_charge) as real_charge,  ");
            sql.Append(" sum(sum_money) as sum_money,      ");
            sql.Append(" sum(sum_insaldo+sum_tarif+reval+real_charge-sum_money) as sum_outs,      ");
            sql.Append(" sum(c_calc) as c_calc,            ");
            sql.Append(" sum(c_reval) as c_reval,          ");
            sql.Append(" sum(sum_in) as sum_in,            ");
            sql.Append(" sum(sum_rasp) as sum_rasp,        ");
            sql.Append(" sum(sum_in + sum_money - sum_rasp) as sum_outf,        ");
            sql.Append(" sum(gkal_calc) as gkal_calc,       ");
            sql.Append(" sum(gkal_reval) as gkal_reval     ");
            sql.Append(" from t_ensvod a, ");
#if PG
            sql.Append(Points.Pref + "_data" +   DBManager.getServer(conn_db) + ".s_area sa, ");
            sql.Append(Points.Pref + "_kernel" +  DBManager.getServer(conn_db) + ".services s ");
#else
            sql.Append(Points.Pref + "_data" + "@" + DBManager.getServer(conn_db) + ":s_area sa, ");
            sql.Append(Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s ");
#endif
            sql.Append(" where a.nzp_area = sa.nzp_area    ");
            sql.Append(" and a.nzp_serv = s.nzp_serv      ");
            sql.Append(" group by 1,2,3,4,5                ");
            sql.Append(" union all                         ");
            sql.Append(" select 1 as ord1,1 as ord2, area, 4 as typek, '' as service, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo,  ");
            sql.Append(" sum(sum_tarif) as sum_tarif,      ");
            sql.Append(" sum(reval) as reval,              ");
            sql.Append(" sum(real_charge) as real_charge,  ");
            sql.Append(" sum(sum_money) as sum_money,      ");
            sql.Append(" sum(sum_insaldo+sum_tarif+reval+real_charge-sum_money) as sum_outs,      ");
            sql.Append(" sum(0) as c_calc,            ");
            sql.Append(" sum(0) as c_reval,          ");
            sql.Append(" sum(sum_in) as sum_in,            ");
            sql.Append(" sum(sum_rasp) as sum_rasp,        ");
            sql.Append(" sum(sum_in + sum_money - sum_rasp) as sum_outf,        ");
            sql.Append(" sum(0) as gkal_calc,       ");
            sql.Append(" sum(0) as gkal_reval     ");
            sql.Append(" from t_ensvod a, ");
#if PG
            sql.Append(Points.Pref + "_data" +   DBManager.getServer(conn_db) + ".s_area sa ");
#else
    sql.Append(Points.Pref + "_data" + "@" + DBManager.getServer(conn_db) + ":s_area sa ");
#endif
            sql.Append(" where a.nzp_area = sa.nzp_area    ");
            sql.Append(" group by 1,2,3,4,5                ");
            sql.Append(" union all                         ");
            sql.Append(" select 1 as ord1, 2 as ord2, area, typek,  '' as service, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo,  ");
            sql.Append(" sum(sum_tarif) as sum_tarif,      ");
            sql.Append(" sum(reval) as reval,              ");
            sql.Append(" sum(real_charge) as real_charge,  ");
            sql.Append(" sum(sum_money) as sum_money,      ");
            sql.Append(" sum(sum_insaldo+sum_tarif+reval+real_charge-sum_money) as sum_outs,      ");
            sql.Append(" sum(0) as c_calc,            ");
            sql.Append(" sum(0) as c_reval,          ");
            sql.Append(" sum(sum_in) as sum_in,            ");
            sql.Append(" sum(sum_rasp) as sum_rasp,        ");
            sql.Append(" sum(sum_in + sum_money - sum_rasp) as sum_outf,        ");
            sql.Append(" sum(0) as gkal_calc,       ");
            sql.Append(" sum(0) as gkal_reval     ");
            sql.Append(" from t_ensvod a, ");
#if PG
            sql.Append(Points.Pref + "_data" +  DBManager.getServer(conn_db) + ".s_area sa, ");
            sql.Append(Points.Pref + "_kernel" +  DBManager.getServer(conn_db) + ".services s ");
#else
            sql.Append(Points.Pref + "_data" + "@" + DBManager.getServer(conn_db) + ":s_area sa, ");
            sql.Append(Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s ");
#endif
            sql.Append(" where a.nzp_area = sa.nzp_area    ");
            sql.Append(" and a.nzp_serv = s.nzp_serv      ");
            sql.Append(" group by 1,2,3,4,5                ");
            sql.Append(" union all                         ");
            sql.Append(" select 2 as ord1, 4 as ord2, 'Всего', 4 as typek, '' as service, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo,  ");
            sql.Append(" sum(sum_tarif) as sum_tarif,      ");
            sql.Append(" sum(reval) as reval,              ");
            sql.Append(" sum(real_charge) as real_charge,  ");
            sql.Append(" sum(sum_money) as sum_money,      ");
            sql.Append(" sum(sum_insaldo+sum_tarif+reval+real_charge-sum_money) as sum_outs,      ");
            sql.Append(" sum(0) as c_calc,            ");
            sql.Append(" sum(0) as c_reval,          ");
            sql.Append(" sum(sum_in) as sum_in,            ");
            sql.Append(" sum(sum_rasp) as sum_rasp,        ");
            sql.Append(" sum(sum_in + sum_money - sum_rasp) as sum_outf,        ");
            sql.Append(" sum(0) as gkal_calc,       ");
            sql.Append(" sum(0) as gkal_reval     ");
            sql.Append(" from t_ensvod a, ");
#if PG
            sql.Append(Points.Pref + "_data" +   DBManager.getServer(conn_db) + ".s_area sa, ");
            sql.Append(Points.Pref + "_kernel" +  DBManager.getServer(conn_db) + ".services s ");
#else
            sql.Append(Points.Pref + "_data" + "@" + DBManager.getServer(conn_db) + ":s_area sa, ");
            sql.Append(Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s ");
#endif
            sql.Append(" where a.nzp_area = sa.nzp_area    ");
            sql.Append(" and a.nzp_serv = s.nzp_serv      ");
            sql.Append(" group by 1,2,3,4                  ");
            sql.Append(" order by ord1,area,ord2, typek, service        ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            if (reader != null)
            {

                try
                {
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);

                }

                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
                reader.Close();
            }

            conn_db.Close();
            return DT;
        }

        public DataTable GetSparavPuLs(Prm prm_, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader; //Основной цикл
            IDataReader reader2;//Для перерасчетов


            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с  БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                conn_db.Close();
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
            conn_web.Close();

            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();


            ExecSQL(conn_db, " drop table t_svod ", false);
            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" nzp_serv integer,");
            sql2.Append(" count_ls_pu integer,");
            sql2.Append(" count_pu integer,");
            sql2.Append(" count_npu integer,");
            sql2.Append(" count_gil integer,");
            sql2.Append(" c_calc numeric(14,4),");
            sql2.Append(" rsum_tarif numeric(14,2),");
            sql2.Append(" reval numeric(14,2),");
            sql2.Append(" sum_charge numeric(14,2),");
            sql2.Append(" norma numeric(14,2),");
            sql2.Append(" sverh_norma numeric(14,2))  ");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_dom integer,");
            sql2.Append(" nzp_serv integer,");
            sql2.Append(" count_ls_pu integer,");
            sql2.Append(" count_pu integer,");
            sql2.Append(" count_npu integer,");
            sql2.Append(" count_gil integer,");
            sql2.Append(" c_calc Decimal(14,4),");
            sql2.Append(" rsum_tarif Decimal(14,2),");
            sql2.Append(" reval Decimal(14,2),");
            sql2.Append(" sum_charge Decimal(14,2),");
            sql2.Append(" norma Decimal(14,2),");
            sql2.Append(" sverh_norma Decimal(14,2)) with no log");
#endif
            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            #region Подготовка списка лицевых счетов
            ExecSQL(conn_db, "drop table sel_kvar5", false);

            sql.Remove(0, sql.Length);
            sql.Append(" select * ");
#if PG
            sql.Append(" into unlogged sel_kvar5  from  " + tXX_spls);
#else
    sql.Append(" from  " + tXX_spls + " into temp sel_kvar5 with no log");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "create index ix_tmp01192 on sel_kvar5(nzp_kvar)", true);
#if PG
            ExecSQL(conn_db, "analyze sel_kvar5", true);
#else
            ExecSQL(conn_db, "update statistics for table sel_kvar5", true);
#endif
            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  sel_kvar5 group by 1");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }
            #endregion


            try
            {
                ret.result = false;

                while (reader.Read())
                {
                    if (reader["pref"] != null)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();
                        string dbCharge = pref + "_charge_" + (prm_.year_ - 2000).ToString("00");


                        ExecSQL(conn_db, "drop table t1", false);
                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" select t.nzp_dom, (case when nzp_serv=14 then 9 else nzp_serv end) as nzp_serv,");
                        sql2.Append(" t.nzp_kvar, sum(cast (0 as numeric(14,2))) as c_calc, ");
                        sql2.Append(" sum(0) as count_gil, sum(0) as norma, sum(0) as sverh_norma, ");
                        sql2.Append(" sum(sum_charge) as sum_charge, sum(rsum_tarif) as rsum_tarif,  ");
                        sql2.Append(" sum(reval+real_charge) as reval, sum(0) as count_pu  ");
                        sql2.Append(" into unlogged t1  from " + dbCharge + ".charge_" + prm_.month_.ToString("00") + " a,");
                        sql2.Append(" sel_kvar5 t, " + pref + "_kernel.formuls f");
                        sql2.Append(" where a.nzp_kvar=t.nzp_kvar and dat_charge is null ");
                        sql2.Append(" and a.nzp_frm=f.nzp_frm and f.is_device=1 and nzp_serv in (6,7,9,14,25,210)  ");
                        sql2.Append(" and a.tarif>0 and f.nzp_measure <>2 group by 1,2,3 ");
#else
       sql2.Append(" select t.nzp_dom, (case when nzp_serv=14 then 9 else nzp_serv end) as nzp_serv,");
                        sql2.Append(" t.nzp_kvar, sum(cast (0 as Decimal(14,2))) as c_calc, ");
                        sql2.Append(" sum(0) as count_gil, sum(0) as norma, sum(0) as sverh_norma, ");
                        sql2.Append(" sum(sum_charge) as sum_charge, sum(rsum_tarif) as rsum_tarif,  ");
                        sql2.Append(" sum(reval+real_charge) as reval, sum(0) as count_pu  ");
                        sql2.Append(" from " + dbCharge + ":charge_" + prm_.month_.ToString("00") + " a,");
                        sql2.Append(" sel_kvar5 t, " + pref + "_kernel:formuls f");
                        sql2.Append(" where a.nzp_kvar=t.nzp_kvar and dat_charge is null ");
                        sql2.Append(" and a.nzp_frm=f.nzp_frm and f.is_device=1 and nzp_serv in (6,7,9,14,25,210)  ");
                        sql2.Append(" and a.tarif>0 and f.nzp_measure <>2 group by 1,2,3 into temp t1 with no log");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;


                        //sql2.Remove(0, sql2.Length);
                        //sql2.Append(" update t1 set (c_calc, count_gil)=((select ");
                        //sql2.Append(" sum(rashod+dlt_reval), max(cnt1) as count_gil ");
                        //sql2.Append(" from " + dbCharge +":counters_" + prm_.month_.ToString("00") + " a ");
                        //sql2.Append(" where a.nzp_kvar=t1.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        //sql2.Append(" and stek=3 and nzp_type=3 ))");
                        //if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                        //    return null;

                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" update t1 set c_calc =((select ");
                        sql2.Append(" sum(case when nzp_serv = 9 then rsh1 else rashod end)  ");
                        sql2.Append(" from " + dbCharge + ".calc_gku_" + prm_.month_.ToString("00") + " a ");
                        sql2.Append(" where a.nzp_kvar=t1.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        sql2.Append(" )), ");

                        sql2.Append("  count_gil=((select ");
                        sql2.Append("  max(gil)  ");
                        sql2.Append(" from " + dbCharge + ".calc_gku_" + prm_.month_.ToString("00") + " a ");
                        sql2.Append(" where a.nzp_kvar=t1.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        sql2.Append(" ))");
#else
                        sql2.Append(" update t1 set (c_calc, count_gil)=((select ");
                        sql2.Append(" sum(case when nzp_serv = 9 then rsh1 else rashod end), max(gil) as count_gil ");
                        sql2.Append(" from " + dbCharge + ":calc_gku_" + prm_.month_.ToString("00") + " a ");
                        sql2.Append(" where a.nzp_kvar=t1.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        sql2.Append(" ))");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;


                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" update t1 set count_pu=coalesce((select ");
                        sql2.Append(" count(distinct nzp_counter) ");
                        sql2.Append(" from " + pref + "_data.counters a ");
                        sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        sql2.Append(" and is_actual=1 and 0 = (select count(*) ");
                        sql2.Append(" from " + pref + "_data.counters  b where a.nzp_counter=b.nzp_counter ");
                        sql2.Append(" and b.dat_close is not null and b.is_actual=1)),1) ");
#else
                  sql2.Append(" update t1 set count_pu=nvl((select ");
                        sql2.Append(" count(unique nzp_counter) ");
                        sql2.Append(" from " + pref + "_data:counters a ");
                        sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and a.nzp_serv=t1.nzp_serv ");
                        sql2.Append(" and is_actual=1 and 0 = (select count(*) ");
                        sql2.Append(" from " + pref + "_data:counters  b where a.nzp_counter=b.nzp_counter ");
                        sql2.Append(" and b.dat_close is not null and b.is_actual=1)),1) ");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;

                        sql2.Remove(0, sql2.Length);
                        sql2.Append(" update t1 set count_pu=1 where count_pu=0 ");
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;

                        // Добавляем канализацию
                        sql2.Remove(0, sql2.Length);
                        sql2.Append(" update t1 set count_pu=0 where nzp_serv=7 ");
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;



                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" insert into t_svod(nzp_dom, nzp_serv, count_ls_pu, count_gil, ");
                        sql2.Append(" c_calc, norma, sverh_norma,");
                        sql2.Append(" rsum_tarif, reval, sum_charge, count_pu, count_npu)");
                        sql2.Append(" select nzp_dom, nzp_serv, count(distinct nzp_kvar), ");
                        sql2.Append(" sum(count_gil), sum(c_calc), sum(norma), sum(sverh_norma), sum(rsum_tarif),  ");
                        sql2.Append(" sum(reval), sum(sum_charge), sum(coalesce(count_pu,0)), sum(0) ");
                        sql2.Append(" from t1 ");
                        sql2.Append(" group by 1,2                          ");
#else
                        sql2.Append(" insert into t_svod(nzp_dom, nzp_serv, count_ls_pu, count_gil, ");
                        sql2.Append(" c_calc, norma, sverh_norma,");
                        sql2.Append(" rsum_tarif, reval, sum_charge, count_pu, count_npu)");
                        sql2.Append(" select nzp_dom, nzp_serv, count(unique nzp_kvar), ");
                        sql2.Append(" sum(count_gil), sum(c_calc), sum(norma), sum(sverh_norma), sum(rsum_tarif),  ");
                        sql2.Append(" sum(reval), sum(sum_charge), sum(nvl(count_pu,0)), sum(0) ");
                        sql2.Append(" from t1 ");
                        sql2.Append(" group by 1,2                          ");
#endif
                        if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                            return null;



                        ExecSQL(conn_db, " drop table t1 ", true);

                    }

                }


#if PG
                string baseData = Points.Pref + "_data" + DBManager.getServer(conn_db);
                string baseKernel = Points.Pref + "_kernel"  + DBManager.getServer(conn_db);
#else
                string baseData = Points.Pref + "_data" + "@" + DBManager.getServer(conn_db);
                string baseKernel = Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db);
#endif

                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" select 0,ulica, ndom, nkor, idom, service,");
                sql2.Append(" sum(count_gil) as count_gil,            ");
                sql2.Append(" sum(count_ls_pu) as count_ls,           ");
                sql2.Append(" sum(count_pu) as count_pu,              ");
                sql2.Append(" sum(count_npu) as count_npu,            ");
                sql2.Append(" sum(rsum_tarif) as rsum_tarif,          ");
                sql2.Append(" sum(reval) as reval,                    ");
                sql2.Append(" sum(sum_charge) as sum_charge,          ");
                sql2.Append(" sum(norma) as c_sn,                     ");
                sql2.Append(" sum(sverh_norma) as c_sv,                ");
                sql2.Append(" sum(c_calc) as c_calc                   ");
                sql2.Append(" from t_svod a, " + baseData + ".dom d, ");
                sql2.Append(baseData + ".s_ulica su,                ");
                sql2.Append(baseKernel + ".services s               ");
                sql2.Append(" where a.nzp_dom=d.nzp_dom               ");
                sql2.Append("    and d.nzp_ul=su.nzp_ul               ");
                sql2.Append("    and a.nzp_serv=s.nzp_serv            ");
                sql2.Append(" group by 1,2,3,4,5,6                    ");
                sql2.Append(" union all                               ");
                sql2.Append(" select 1,'ВСЕГО', '', '', 0, service,   ");
                sql2.Append(" sum(count_gil) as count_gil,            ");
                sql2.Append(" sum(count_ls_pu) as count_ls,           ");
                sql2.Append(" sum(count_pu) as count_pu,              ");
                sql2.Append(" sum(count_npu) as count_npu,            ");
                sql2.Append(" sum(rsum_tarif) as rsum_tarif,          ");
                sql2.Append(" sum(reval) as reval,                    ");
                sql2.Append(" sum(sum_charge) as sum_charge,          ");
                sql2.Append(" sum(norma) as c_sn,                     ");
                sql2.Append(" sum(sverh_norma) as c_sv,               ");
                sql2.Append(" sum(c_calc) as c_calc                   ");
                sql2.Append(" from t_svod a,");
                sql2.Append(baseKernel + ".services s               ");
                sql2.Append(" where a.nzp_serv=s.nzp_serv            ");
                sql2.Append(" group by 1,2,3,4,5,6                    ");
                sql2.Append(" order by 1,2,5,3,4,6                   ");
#else
            sql2.Append(" select 0,ulica, ndom, nkor, idom, service,");
                sql2.Append(" sum(count_gil) as count_gil,            ");
                sql2.Append(" sum(count_ls_pu) as count_ls,           ");
                sql2.Append(" sum(count_pu) as count_pu,              ");
                sql2.Append(" sum(count_npu) as count_npu,            ");
                sql2.Append(" sum(rsum_tarif) as rsum_tarif,          ");
                sql2.Append(" sum(reval) as reval,                    ");
                sql2.Append(" sum(sum_charge) as sum_charge,          ");
                sql2.Append(" sum(norma) as c_sn,                     ");
                sql2.Append(" sum(sverh_norma) as c_sv,                ");
                sql2.Append(" sum(c_calc) as c_calc                   ");
                sql2.Append(" from t_svod a, " + baseData + ":dom d, ");
                sql2.Append(baseData + ":s_ulica su,                ");
                sql2.Append(baseKernel + ":services s               ");
                sql2.Append(" where a.nzp_dom=d.nzp_dom               ");
                sql2.Append("    and d.nzp_ul=su.nzp_ul               ");
                sql2.Append("    and a.nzp_serv=s.nzp_serv            ");
                sql2.Append(" group by 1,2,3,4,5,6                    ");
                sql2.Append(" union all                               ");
                sql2.Append(" select 1,'ВСЕГО', '', '', 0, service,   ");
                sql2.Append(" sum(count_gil) as count_gil,            ");
                sql2.Append(" sum(count_ls_pu) as count_ls,           ");
                sql2.Append(" sum(count_pu) as count_pu,              ");
                sql2.Append(" sum(count_npu) as count_npu,            ");
                sql2.Append(" sum(rsum_tarif) as rsum_tarif,          ");
                sql2.Append(" sum(reval) as reval,                    ");
                sql2.Append(" sum(sum_charge) as sum_charge,          ");
                sql2.Append(" sum(norma) as c_sn,                     ");
                sql2.Append(" sum(sverh_norma) as c_sv,               ");
                sql2.Append(" sum(c_calc) as c_calc                   ");
                sql2.Append(" from t_svod a,");
                sql2.Append(baseKernel + ":services s               ");
                sql2.Append(" where a.nzp_serv=s.nzp_serv            ");
                sql2.Append(" group by 1,2,3,4,5,6                    ");
                sql2.Append(" order by 1,2,5,3,4,6                   ");
#endif
                if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    return null;

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
                        MonitorLog.WriteLog("!!!GetSparavPuLs Ошибка записи в таблицу" +
                            ex.Message, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }
                reader2.Close();
                reader.Close();
                ExecSQL(conn_db, " drop table t_svod ", true);

                ret.result = true;


            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            // reader2.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }

        public DataTable GetFindTemplate(out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport получение шаблона поиска", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            DataTable Data_Table = new DataTable();
            StringBuilder sql = new StringBuilder();

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion
            try
            {


                #region Выборка

                sql.Remove(0, sql.Length);
                sql.Append(" select * ");
                sql.Append(" from  t" + Nzp_user + "_spfinder where nzp_page=41 order by nzp_finder");

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                #endregion


                #region Вывод списка в таблицу
                if (reader != null)
                {

                    //заполнение DataTable


                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                    Data_Table.Load(reader, LoadOption.OverwriteChanges);

                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! DomNach ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    return null;
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!! Ошибка сбора шаблона" + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                sql.Remove(0, sql.Length);
                conn_web.Close(); //закрыть соединение с основной базой     
            }

            return Data_Table;


        }

        public DataTable GetVedPere(Prm prm, out Returns ret, string Nzp_user)
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
            string tXX_spls = "public" + "." + "t" + Nzp_user + "_spls";
#else
     string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string pref = "";

            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();

            ExecRead(conn_db, out reader2, "drop table t_svod", false);
            ExecRead(conn_db, out reader2, "drop table t1", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append("create  table t_svod( ");
            sql2.Append("pkod10 integer,");
            sql2.Append("nzp_serv integer,");
            sql2.Append("nzp_kvar integer,");
            sql2.Append("fio char(40),");
            sql2.Append("reval_k numeric(14,2),");
            sql2.Append("reval_d numeric(14,2),");
            sql2.Append("nzp_reason integer,");
            sql2.Append("nzp_prm integer,");
            sql2.Append("dat_s Date,");
            sql2.Append("dat_po Date) ");
#else
            sql2.Append("create temp table t_svod( ");
            sql2.Append("pkod10 integer,");
            sql2.Append("nzp_serv integer,");
            sql2.Append("nzp_kvar integer,");
            sql2.Append("fio char(40),");
            sql2.Append("reval_k Decimal(14,2),");
            sql2.Append("reval_d Decimal(14,2),");
            sql2.Append("nzp_reason integer,");
            sql2.Append("nzp_prm integer,");
            sql2.Append("dat_s Date,");
            sql2.Append("dat_po Date) with no log");
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
            sql.Append(" from  t" + Nzp_user + "_spls group by 1");

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
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



                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append("select nzp_serv, a.nzp_kvar, ");
                    sql2.Append("sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append("sum(case when reval>0 then reval else 0 end) as reval_d,");
                    sql2.Append("0 as nzp_reason, 0 as nzp_prm, '01.01.1900'::date as dat_s, '01.01.3000'::date as dat_po ");
                    sql2.Append(" into unlogged t1  from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + "  k ");
                    sql2.Append("where a.nzp_serv>1 and dat_charge is null and abs(reval)>0.001 ");
                    sql2.Append("and a.num_ls=k.num_ls ");
                    sql2.Append("group by 1,2  ");
#else
         sql2.Append("select nzp_serv, a.nzp_kvar, ");
                    sql2.Append("sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append("sum(case when reval>0 then reval else 0 end) as reval_d,");
                    sql2.Append("0 as nzp_reason, 0 as nzp_prm, '01.01.1900' as dat_s, '01.01.3000' as dat_po ");
                    sql2.Append("from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + "  k ");
                    sql2.Append("where a.nzp_serv>1 and dat_charge is null and abs(reval)>0.001 ");
                    sql2.Append("and a.num_ls=k.num_ls ");
                    sql2.Append("group by 1,2 into temp t1 with no log");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append("create index ix_tmps_01 on t1(nzp_serv, nzp_kvar)");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append("analyze t1");
#else
sql2.Append("update statistics for table t1");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


#if PG
                    sql2.Remove(0, sql2.Length);
                    sql2.Append("  UPDATE t1 SET ");
                    sql2.Append(" nzp_reason = ( SELECT	MAX (kod1)	FROM  " + pref + "_data.must_calc a WHERE ");
                    sql2.Append(" t1.nzp_kvar = A .nzp_kvar	AND t1.nzp_serv = A .nzp_serv	AND A .month_ = 09	AND A .year_ = 09),");
                    sql2.Append("nzp_prm = ( SELECT		MAX (kod2)	FROM  " + pref + "_data.must_calc a WHERE ");
                    sql2.Append(" t1.nzp_kvar = A .nzp_kvar AND t1.nzp_serv = A .nzp_serv	AND A .month_ = 09	AND A .year_ = 09),");
                    sql2.Append(" dat_s = (	SELECT MIN (dat_s)	FROM  " + pref + "_data.must_calc a WHERE ");
                    sql2.Append("t1.nzp_kvar = A .nzp_kvar	AND t1.nzp_serv = A .nzp_serv	AND A .month_ = 09	AND A .year_ = 09),");
                    sql2.Append(" dat_po = (	SELECT	MAX (dat_po) from " + pref + "_data.must_calc a WHERE ");
                    sql2.Append("	t1.nzp_kvar = A .nzp_kvar	AND t1.nzp_serv = A .nzp_serv	AND A .month_ = 09	AND A .year_ = 09) ");


                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("update t1 set  ");
                    sql2.Append("nzp_reason= (select max(kod1) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=1 ),");

                    sql2.Append("nzp_prm= (select  max(kod2) from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=1 ),");

                    sql2.Append("dat_s=(select min(dat_s) from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=1 ),");

                    sql2.Append("dat_po=(select  max(dat_po) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=1 ) ");
                    sql2.Append(" where nzp_reason is null");

                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);


                    sql2.Append(" update t1 set ");
                    sql2.Append("nzp_reason= (select max(kod1) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=9 ), ");

                    sql2.Append("nzp_prm= (select max(kod2) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=9 ), ");

                    sql2.Append("dat_s=(select min(dat_s) from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=9 ), ");

                    sql2.Append("  dat_po=(select max(dat_po)  from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=9 ) ");
                    sql2.Append(" where nzp_reason is null and nzp_serv = 14");

                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                    sql2.Append("update t1 set ");
                    sql2.Append("nzp_reason= (select max(kod1) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv in (6,9) ),");

                    sql2.Append("nzp_prm= (select max(kod2) from  " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv in (6,9) ),");

                    sql2.Append("dat_s=(select min(dat_s) from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv in (6,9) ),");

                    sql2.Append("  dat_po=(select max(dat_po)  from " + pref + "_data.must_calc a ");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv in (6,9) ) ");
                    sql2.Append(" where nzp_reason is null and nzp_serv = 7");


                    sql2.Append("");
                    sql2.Append("");
                    sql2.Append("");
                    sql2.Remove(0, sql2.Length);

                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(pkod10, fio, nzp_serv, nzp_kvar, reval_k, reval_d, nzp_reason, nzp_prm, dat_s, dat_po)");
                    sql2.Append(" select pkod10, fio, nzp_serv, a.nzp_kvar, ");
                    sql2.Append(" reval_k, reval_d, nzp_reason, nzp_prm, dat_s, dat_po");
                    sql2.Append(" from t1 a, " + pref + "_data.kvar k");
                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar");
#else
 sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set (nzp_reason, nzp_prm, dat_s, dat_po)=((");
                    sql2.Append(" select max(kod1) as kod1, max(kod2) as kod2, ");
                    sql2.Append(" min(dat_s) as dat_s, max(dat_po) as dat_po");
                    sql2.Append(" from " + pref + "_data:must_calc a");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + "))");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set (nzp_reason, nzp_prm, dat_s, dat_po)=((");
                    sql2.Append(" select max(kod1) as kod1, max(kod2) as kod2, ");
                    sql2.Append(" min(dat_s) as dat_s, max(dat_po) as dat_po");
                    sql2.Append(" from " + pref + "_data:must_calc a");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=1 ))");
                    sql2.Append(" where nzp_reason is null");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set (nzp_reason, nzp_prm, dat_s, dat_po)=((");
                    sql2.Append(" select max(kod1) as kod1, max(kod2) as kod2, ");
                    sql2.Append(" min(dat_s) as dat_s, max(dat_po) as dat_po");
                    sql2.Append(" from " + pref + "_data:must_calc a");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv=9 ))");
                    sql2.Append(" where nzp_reason is null and nzp_serv = 14");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t1 set (nzp_reason, nzp_prm, dat_s, dat_po)=((");
                    sql2.Append(" select max(kod1) as kod1, max(kod2) as kod2, ");
                    sql2.Append(" min(dat_s) as dat_s, max(dat_po) as dat_po");
                    sql2.Append(" from " + pref + "_data:must_calc a");
                    sql2.Append(" where t1.nzp_kvar=a.nzp_kvar and t1.nzp_serv=a.nzp_serv");
                    sql2.Append(" and a.month_=" + prm.month_.ToString("00"));
                    sql2.Append(" and a.year_=" + prm.month_.ToString("00") + " and nzp_serv in (6,9) ))");
                    sql2.Append(" where nzp_reason is null and nzp_serv = 7");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(pkod10, fio, nzp_serv, nzp_kvar, reval_k, reval_d, nzp_reason, nzp_prm, dat_s, dat_po)");
                    sql2.Append(" select pkod10, fio, nzp_serv, a.nzp_kvar, ");
                    sql2.Append(" reval_k, reval_d, nzp_reason, nzp_prm, dat_s, dat_po");
                    sql2.Append(" from t1 a, " + pref + "_data:kvar k");
                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);
                    sql2.Append("drop table t1");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                }
            }

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select a.*, reason, service");
            sql2.Append(" from t_svod a, " + Points.Pref + "_kernel" + DBManager.getServer(conn_db) + ".s_reason b,");
            sql2.Append(Points.Pref + "_kernel" + DBManager.getServer(conn_db) + ".services s");
            sql2.Append(" where a.nzp_reason=b.nzp_reason");
            sql2.Append(" and a.nzp_serv=s.nzp_serv");
#else
           sql2.Append(" select a.*, reason, service");
            sql2.Append(" from t_svod a, " + Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":s_reason b,");
            sql2.Append(Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s");
            sql2.Append(" where a.nzp_reason=b.nzp_reason");
            sql2.Append(" and a.nzp_serv=s.nzp_serv");
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

        public DataTable GetDolgSved(Prm prm, out Returns ret, string Nzp_user)
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


            sql2.Remove(0, sql2.Length);
            sql2.Append("drop table t_svod");
            ExecRead(conn_db, out reader2, sql2.ToString(), false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged  table t_svod( ");
            sql2.Append(" mes_dolg integer,");
            sql2.Append(" nzp_geu integer,");
            sql2.Append(" count_ls integer,");
            sql2.Append(" proc_dolg numeric(14,2),");
            sql2.Append(" sum_outsaldo numeric(14,2),");
            sql2.Append(" count_all integer) ");
#else
          sql2.Append(" create temp table t_svod( ");
            sql2.Append(" mes_dolg integer,");
            sql2.Append(" nzp_geu integer,");
            sql2.Append(" count_ls integer,");
            sql2.Append(" proc_dolg Decimal(14,2),");
            sql2.Append(" sum_outsaldo Decimal(14,2),");
            sql2.Append(" count_all integer) with no log");
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
            sql.Append(" select count(*)  as co from t" + Nzp_user + "_spls ");
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }
            int count_all = 0;
            if (reader.Read())
            {
                count_all = System.Convert.ToInt32(reader["co"]);
            }
            reader.Close();


            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  t" + Nzp_user + "_spls group by 1");

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
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

                    ExecRead(conn_db, out reader2, "drop table t1", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_geu, round((sum_insaldo-sum_money)/rsum_tarif,0) as mes_dolg,");
                    sql2.Append(" count(a.num_ls) as count_ls, ");
                    sql2.Append(" sum(sum_insaldo-sum_money) as sum_outsaldo");
                    sql2.Append(" into unlogged t1  from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where rsum_tarif>0.001 and sum_insaldo-sum_money>0.001 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1 ");
                    sql2.Append(" group by 1,2");
                    sql2.Append(" union all");
                    sql2.Append(" select nzp_geu,-1,");
                    sql2.Append(" count(a.num_ls) as count_ls, ");
                    sql2.Append(" sum(sum_insaldo-sum_money) as sum_outsaldo");
                    sql2.Append("   from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where  rsum_tarif<0.001 and sum_insaldo-sum_money>0.001 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1 ");
                    sql2.Append(" group by 1,2");
                   
#else
                    sql2.Append(" select nzp_geu, round((sum_insaldo-sum_money)/rsum_tarif,0) as mes_dolg,");
                    sql2.Append(" count(a.num_ls) as count_ls, ");
                    sql2.Append(" sum(sum_insaldo-sum_money) as sum_outsaldo");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where rsum_tarif>0.001 and sum_insaldo-sum_money>0.001 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1 ");
                    sql2.Append(" group by 1,2");
                    sql2.Append(" union all");
                    sql2.Append(" select nzp_geu,-1,");
                    sql2.Append(" count(a.num_ls) as count_ls, ");
                    sql2.Append(" sum(sum_insaldo-sum_money) as sum_outsaldo");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where  rsum_tarif<0.001 and sum_insaldo-sum_money>0.001 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1 ");
                    sql2.Append(" group by 1,2");
                    sql2.Append(" into temp t1 with no log;");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_web.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }





                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(mes_dolg, nzp_geu, count_ls, proc_dolg, sum_outsaldo, count_all)");
                    sql2.Append(" select (case when mes_dolg>12 then 12 else mes_dolg end), ");
                    sql2.Append(" nzp_geu, sum(count_ls), sum(0), sum(sum_outsaldo), sum(0) ");
                    sql2.Append(" from t1");
                    sql2.Append(" group by 1,2");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_web.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }



                    sql2.Remove(0, sql2.Length);
                    sql2.Append("drop table t1");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_web.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }



                }
            }

            sql2.Remove(0, sql2.Length);
            sql2.Append(" update t_svod set count_all = " + count_all.ToString());
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            sql2.Remove(0, sql2.Length);
            sql2.Append(" update t_svod set proc_dolg = count_ls/count_all*100 where count_all>0 ");
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select geu, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data");
            sql2.Append( DBManager.getServer(conn_db) + ".s_geu g ");
            sql2.Append(" where a.nzp_geu=g.nzp_geu ");
            sql2.Append(" order by mes_dolg, geu ");
#else
         sql2.Append(" select geu, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data");
            sql2.Append("@" + DBManager.getServer(conn_db) + ":s_geu g ");
            sql2.Append(" where a.nzp_geu=g.nzp_geu ");
            sql2.Append(" order by mes_dolg, geu ");
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

        public DataTable GetDolgSpis(Prm prm, out Returns ret, string Nzp_user)
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

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_kvar, fio, ulica, ndom, idom, nkvar, ikvar, nzp_geu into unlogged t_adr from " + tXX_spls);
           
#else
            sql2.Append(" select nzp_kvar, fio, ulica, ndom, idom, nkvar, ikvar, nzp_geu from " + tXX_spls);
            sql2.Append(" into temp t_adr with no log");
#endif
            ExecRead(conn_db, out reader2, sql2.ToString(), true);


            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" mes_dolg integer,");
            sql2.Append(" nzp_kvar integer,");
            sql2.Append(" sum_insaldo numeric(14,2),");
            sql2.Append(" sum_charge numeric(14,2),");
            sql2.Append(" sum_outsaldo numeric(14,2),");
            sql2.Append(" sum_money numeric(14,2), ");
            sql2.Append(" privat integer)  ");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" mes_dolg integer,");
            sql2.Append(" nzp_kvar integer,");
            sql2.Append(" sum_insaldo Decimal(14,2),");
            sql2.Append(" sum_charge Decimal(14,2),");
            sql2.Append(" sum_outsaldo Decimal(14,2),");
            sql2.Append(" sum_money Decimal(14,2), ");
            sql2.Append(" privat integer) with no log");
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
                    ExecRead(conn_db, out reader2, "drop table t1", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, round((sum_insaldo-sum_money)/rsum_tarif,0) as mes_dolg,");
                    sql2.Append(" sum_insaldo, sum_charge, sum_money, sum_outsaldo, 0 as privat ");
                    sql2.Append(" into unlogged t1 from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where  rsum_tarif>0.001 and sum_outsaldo>0.001 ");
                    sql2.Append(" and sum_insaldo-sum_money>=rsum_tarif*3 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1");
                
#else
             sql2.Append(" select a.nzp_kvar, round((sum_insaldo-sum_money)/rsum_tarif,0) as mes_dolg,");
                    sql2.Append(" sum_insaldo, sum_charge, sum_money, sum_outsaldo, 0 as privat ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
                    sql2.Append(prm.month_.ToString("00") + " a, " + tXX_spls + " k ");
                    sql2.Append(" where  rsum_tarif>0.001 and sum_outsaldo>0.001 ");
                    sql2.Append(" and sum_insaldo-sum_money>=rsum_tarif*3 ");
                    sql2.Append(" and a.nzp_kvar=k.nzp_kvar and nzp_serv=1");
                    sql2.Append(" into temp t1 with no log");
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
                    sql2.Append(" update t1 set privat = (select max(1) from " + pref + "_data.prm_1 a where nzp_prm=8");
                    sql2.Append(" and is_actual=1 and t1.nzp_kvar=a.nzp ");
                    //sql2.Append(" and val_prm='1' and dat_s<=today and dat_po>=today)");
                    sql2.Append(" and dat_s<=current_date and dat_po>=current_date)");
#else
           sql2.Append(" update t1 set privat = (select max(1) from " + pref + "_data:prm_1 a where nzp_prm=8");
                    sql2.Append(" and is_actual=1 and t1.nzp_kvar=a.nzp ");
                    //sql2.Append(" and val_prm='1' and dat_s<=today and dat_po>=today)");
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
                    sql2.Append(" update t1 set privat = 0 where privat is null");
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
                    sql2.Append(" insert into t_svod(nzp_kvar, mes_dolg, sum_insaldo,sum_charge ,sum_money, sum_outsaldo, privat)");
                    sql2.Append(" select nzp_kvar, mes_dolg, sum_insaldo,sum_charge ,sum_money, sum_outsaldo, privat");
                    sql2.Append(" from t1 ");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("drop table t1");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }


                }
            }


            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select geu, ulica, idom, ndom, ikvar, nkvar, fio, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data");
            sql2.Append( DBManager.getServer(conn_db) + ".s_geu g, t_adr ta ");
            sql2.Append(" where g.nzp_geu=ta.nzp_geu and a.nzp_kvar=ta.nzp_kvar ");
            sql2.Append(" order by geu,ulica, idom, ndom, ikvar, nkvar, fio  ");
#else
            sql2.Append(" select geu, ulica, idom, ndom, ikvar, nkvar, fio, a.* ");
            sql2.Append(" from t_svod a, " + Points.Pref + "_data");
            sql2.Append("@" + DBManager.getServer(conn_db) + ":s_geu g, t_adr ta ");
            sql2.Append(" where g.nzp_geu=ta.nzp_geu and a.nzp_kvar=ta.nzp_kvar ");
            sql2.Append(" order by geu,ulica, idom, ndom, ikvar, nkvar, fio  ");
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
            sql2.Remove(0, sql2.Length);
            sql2.Append("drop table t_adr");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);
            conn_db.Close();




            reader.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }

        public DataTable GetPaspRas(Prm prm, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("отчет для самары.", MonitorLog.typelog.Info, 20, 201, true);
            IDataReader reader = null;
            IDataReader reader2 = null;
            StringBuilder sql;
            StringBuilder sql2;
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            try
            {
                ret = Utils.InitReturns();

                string ds;
                string ds_1;
                string dpo;
                //string dpo_1;
                int mm_1;
                int yy_1;

                if (prm.month_ != 1)
                {
                    mm_1 = prm.month_ - 1;
                    yy_1 = prm.year_;
                }
                else
                {
                    mm_1 = 12;
                    yy_1 = prm.year_ - 1;
                }


                ds = "01." + prm.month_.ToString("00") + "." + prm.year_.ToString();
                ds_1 = "01." + mm_1.ToString("00") + "." + yy_1.ToString();
                dpo = DateTime.DaysInMonth(prm.year_, prm.month_).ToString("00") + "." + prm.month_.ToString("00") + "." + prm.year_.ToString();
                //dpo_1 = DateTime.DaysInMonth(yy_1, mm_1).ToString("00") + "." + prm.month_.ToString("00") + "." + prm.year_.ToString();


                #region Подключение к БД
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                    conn_web.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                sql = new StringBuilder();
                sql2 = new StringBuilder();
#if PG
                string tXX_spls = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
                string pref = "";

                #region Выборка по локальным банкам
                DataTable LocalTable = new DataTable();

                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" select nzp_kvar, pref into unlogged t_adr  from " + tXX_spls);
            
#else
                sql2.Append(" select nzp_kvar, pref from " + tXX_spls);
                sql2.Append(" into temp t_adr with no log");
#endif
                ExecRead(conn_db, out reader2, sql2.ToString(), true);


                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index idx_pasp_ras1 on t_adr(nzp_kvar)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index idx_pasp_ras2 on t_adr(pref)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" analyze t_adr");
#else
      sql2.Append(" Update statistics for table t_adr");
#endif
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" create unlogged  table t_svod( ");
                sql2.Append(" nzp_kvar integer,");
                sql2.Append(" num_ls   integer,");
                sql2.Append(" ulica    char(100),");
                sql2.Append(" idom integer,");
                sql2.Append(" ndom char(10),");
                sql2.Append(" nkor char(10),");
                sql2.Append(" ikvar integer,");
                sql2.Append(" nkvar char(10),");
                sql2.Append(" nkvar_n char(10),");
                sql2.Append(" fio char(50),");
                sql2.Append(" sost char(10),");
                sql2.Append(" kol_gil integer,");
                sql2.Append(" kol_prm_pasp char(10),");
                sql2.Append(" base char(10),");
                sql2.Append(" dat_s1 date,");
                sql2.Append(" dat_po1 date,");
                sql2.Append(" dat_s2 date,");
                sql2.Append(" dat_po2 date,");
                sql2.Append(" dat_s3 date,");
                sql2.Append(" dat_po3 date,");
                sql2.Append(" kol_prm char(10)  ) ");
#else
                sql2.Append(" create temp table t_svod( ");
                sql2.Append(" nzp_kvar integer,");
                sql2.Append(" num_ls   integer,");
                sql2.Append(" ulica    char(100),");
                sql2.Append(" idom integer,");
                sql2.Append(" ndom char(10),");
                sql2.Append(" nkor char(10),");
                sql2.Append(" ikvar integer,");
                sql2.Append(" nkvar char(10),");
                sql2.Append(" nkvar_n char(10),");
                sql2.Append(" fio char(50),");
                sql2.Append(" sost char(10),");
                sql2.Append(" kol_gil integer,");
                sql2.Append(" kol_prm_pasp char(10),");
                sql2.Append(" base char(10),");
                sql2.Append(" kol_prm char(10)  ) with no log");
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
                sql.Append(" from  " + tXX_spls + " group by 1");

                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                string my_num_ls;
                if (Points.IsSmr)
                {
                    my_num_ls = "pkod10";
                }
                else
                {
                    my_num_ls = "num_ls";
                }

                while (reader.Read())
                {
                    if (reader["pref"] != null)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();
                        string dom = pref + DBManager.sDataAliasRest + "dom ";
                        string ul = pref + DBManager.sDataAliasRest + "s_ulica ";
                        string kvar = pref + DBManager.sDataAliasRest + "kvar ";
                        string prm_1 = pref + DBManager.sDataAliasRest + "prm_1 ";
                        string prm_3 = pref + DBManager.sDataAliasRest + "prm_3 ";
                        string gil = pref + "_charge_"+(prm.year_-2000).ToString("00") + DBManager.tableDelimiter+ "gil_"+prm.month_.ToString("00");

                        sql2.Remove(0, sql2.Length);

                        sql2.Append(" insert into t_svod");
                        sql2.Append(" select k.nzp_kvar, k." + my_num_ls + ", u.ulica, d.idom, d.ndom,d.nkor,k.ikvar, k.nkvar, k.nkvar_n, k.fio,");
                        sql2.Append(" (case when p3.val_prm::INT =1 then 'открыт' when p3.val_prm::INT =2 then 'закрыт' else  'неопределено' end ),"); 
                        sql2.Append(" round(g.val1 + g.val3 - g.val5) as kol_gil,");
                        sql2.Append(" p1.val_prm  as kol_prm_pasp ,");//2005
                        sql2.Append(" t.pref as base ,");
                        sql2.Append(" p1.dat_s, p1.dat_po, p2.dat_s, p2.dat_po, p3.dat_s, p3.dat_po,");
                        sql2.Append(" p2.val_prm  as kol_prm");  //5
                        //from
                        sql2.Append(" from " + kvar + " k, " + dom + " d, " + ul + " u, t_adr t," + gil + " g, " + prm_1 + " p1, " 
                            + prm_1 + " p2, " + prm_3 + " p3 ");
                       
                        sql2.Append(" where t.pref='" + pref + "'");
                        sql2.Append(" and k.nzp_kvar=t.nzp_kvar");
                        sql2.Append(" and k.nzp_dom=d.nzp_dom");
                        sql2.Append(" and d.nzp_ul=u.nzp_ul");
                        sql2.Append(" and g.nzp_kvar=k.nzp_kvar");
                        sql2.Append(" and g.stek=3");

                        sql2.Append(" and p1.nzp= k.nzp_kvar");
                        sql2.Append(" and p1.nzp_prm=2005");
                        sql2.Append(" and p1.is_actual<>100");
                        sql2.Append(" and p1.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p1.dat_po >= date('" + ds + "')");

                        sql2.Append(" and p2.nzp= k.nzp_kvar");
                        sql2.Append(" and p2.nzp_prm=5");
                        sql2.Append(" and p2.is_actual<>100");
                        sql2.Append(" and p2.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p2.dat_po >= date('" + ds + "')");
                        
                        sql2.Append(" and p3.nzp= k.nzp_kvar");
                        sql2.Append(" and p3.nzp_prm=51");
                        sql2.Append(" and p3.is_actual<>100");
                        sql2.Append(" and p3.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p3.dat_po >= date('" + ds + "') ");
                       
                        //если не считается по паспортистке
                        //sql2.Append(" and 0=(select count(*)");
                        //sql2.Append(" from " + pref + "_data.prm_1 p11");
                        //sql2.Append(" where p11.nzp=k.nzp_kvar");
                        //sql2.Append(" and p11.nzp_prm=130");
                        //sql2.Append(" and p11.val_prm='1'");
                        //sql2.Append(" and p11.is_actual<>100");
                        //sql2.Append(" and p11.dat_s<=date('" + dpo + "') ");
                        //sql2.Append(" and p11.dat_po >= date('" + ds + "')) ");
                        
                        if (Points.IsSmr)
                        {
                            sql2.Append(" and round(g.val1 + g.val3 - g.val5)||''<>p1.val_prm ");
                        }
                        else
                        {    
                            sql2.Append(" and round(g.val1 + g.val3 - g.val5)||''<>p2.val_prm ");
                        }
                        
                        if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            conn_web.Close();
                            sql.Remove(0, sql.Length);
                            ret.result = false;
                            return null;
                        }
                    }
                }

                sql2.Remove(0, sql2.Length);
                sql2.Append(" select * ");
                sql2.Append(" from t_svod a ");
                sql2.Append(" order by  a.ulica, a.idom, a.ndom, a.nkor, a.ikvar, a.nkvar, a.nkvar_n, a.num_ls ");
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
                #endregion

                return LocalTable;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Рассогласование с паспортисткой " +
    ex.Message, MonitorLog.typelog.Error, true);
                ret = Utils.InitReturns();
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                OpenDb(conn_db, true);
                ExecSQL(conn_db, " Drop table t_svod; ", true);
                ExecSQL(conn_db, " Drop table t_adr; ", true);
                conn_db.Close();
                conn_web.Close();

                if (conn_db != null)
                    conn_db.Close();

                if (conn_web != null)
                    conn_web.Close();

                if (reader != null)
                    reader.Close();

                if (reader2 != null)
                    reader2.Close();
                #endregion
            }
        }
    }
}
