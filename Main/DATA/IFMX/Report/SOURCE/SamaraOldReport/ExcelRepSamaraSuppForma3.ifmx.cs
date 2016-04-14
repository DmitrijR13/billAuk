using System;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {
        /// <summary>
        /// Отчет по поставщикам для Самры Форма 3
        /// </summary>
        /// <param name="prm_">Параметры поиска</param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user">Потльзователь</param>
        /// <returns>DataTable</returns>
        public DataTable GetSpravSuppNachHar2(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();

            string dat = "01." + prm_.month_.ToString("00") + "." + prm_.year_.ToString();
            DateTime dat1 = Convert.ToDateTime(dat);
            string dat_po = dat1.AddMonths(1).ToString("dd.MM.yyyy");

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            MyDataReader reader = null;
            MyDataReader reader2 = null;


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

            DataTable LocalTable = new DataTable();
            string tXX_spls = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "t" + Nzp_user + "_spls";
            string pref = "";
            string where_supp = "";
            string where_serv = "";
            conn_web.Close();

            #region Ограничения на выборку
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
                        }
                    }
                }
            }

            where_serv += (prm_.nzp_serv == -206 ? " and nzp_serv <> 206 " :
                       prm_.nzp_serv == 206 ? " and nzp_serv=206 " : "");
            #endregion


            try
            {
                #region Выборка по локальным банкам


                ExecSQL(conn_db, "drop table t_svod", false);
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_svod( ");
                sql.Append("  tip integer, ");
                sql.Append("  geu char(40), ");
                sql.Append("  sort integer, ");
                sql.Append("  has_serv integer default 0, ");
                sql.Append("  name_supp char(100), ");
                sql.Append("  service char(50), ");
                sql.Append("  ordering integer, ");
                sql.Append("  ord_serv integer, ");
                sql.Append("  isol_pl " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  komm_pl " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  count_gil integer, ");
                sql.Append("  rashod " + DBManager.sDecimalType + "(14,4), ");
                sql.Append("  measure char(10), ");
                sql.Append("  rashod_gkal " + DBManager.sDecimalType + "(14,4), ");
                sql.Append("  count_ls integer, ");
                sql.Append("  rsum_tarif " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  sum_nedop " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  reval_k " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  reval_d " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  real_charge_k " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  real_charge_d " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  sum_charge " + DBManager.sDecimalType + "(14,2), ");
                sql.Append("  sum_money " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);




                #region Подготовка списка лицевых счетов
                ExecSQL(conn_db, "drop table sel_kvar5", false);

                sql.Remove(0, sql.Length);
                sql.Append("CREATE TEMP TABLE sel_kvar5(" +
                           " nzp_kvar integer, " +
                           " nzp_geu integer, " +
                           " pref char(10)) " + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" insert into sel_kvar5(nzp_kvar, nzp_geu, pref)");
                sql.Append(" select nzp_kvar, nzp_geu, pref ");
                sql.Append(" from  " + tXX_spls + "");
                ExecSQL(conn_db, sql.ToString(), true);


                ExecSQL(conn_db, "create index ix_tmp01192 on sel_kvar5(nzp_kvar)", true);
                ExecSQL(conn_db, DBManager.sUpdStat + " sel_kvar5", true);

                sql.Remove(0, sql.Length);
                sql.Append(" select pref ");
                sql.Append(" from  sel_kvar5 group by 1");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    throw new Exception("Ошибка формирования списка квартир для отчета");

                }
                #endregion


                while (reader.Read())
                {
                    if (reader["pref"] != null)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();

                        #region Создаем временные таблички

                        ExecSQL(conn_db, " drop table t_nachdom1 ", false);
                        ExecSQL(conn_db, " drop table t_nachdom2 ", false);
                        ExecSQL(conn_db, " drop table t_servs ", false);
                        ExecSQL(conn_db, " drop table t_nachdom ", false);
                        ExecSQL(conn_db, " drop table t_tf1 ", false);
                        ExecSQL(conn_db, " drop table selected_kvars ", false);


                        sql.Remove(0, sql.Length);
                        sql.Append(" create temp table selected_kvars( ");
                        sql.Append(" nzp_geu integer,  ");
                        sql.Append(" nzp_kvar integer,  ");
                        sql.Append(" pl_kvar_n " + DBManager.sDecimalType + "(14,2)  default 0,  ");
                        sql.Append(" pl_kvar " + DBManager.sDecimalType + "(14,2)  default 0,  ");
                        sql.Append(" isol integer default 1)" + DBManager.sUnlogTempTable);

                        ExecSQL(conn_db, sql.ToString(), true);

                        #endregion

                        #region Заносим список квартир
                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into selected_kvars (nzp_kvar,nzp_geu) ");
                        sql.Append(" select nzp_kvar,nzp_geu from sel_kvar5 where pref='" + pref + "'");
                        ExecSQL(conn_db, sql.ToString(), true);



                        sql.Remove(0, sql.Length);
                        sql.Append(" create index ix_tmp_ts99 on selected_kvars(nzp_kvar)");
                        ExecSQL(conn_db, sql.ToString(), true);

                        #endregion

                        #region Вычисляем площадь по коммунальным квартирам
                        sql.Remove(0, sql.Length);
                        sql.Append(" update  selected_kvars  set pl_kvar_n= ");
                        sql.Append(" (select sum(p.val_prm" + DBManager.sConvToNum + ") as pl_kvar ");
                        sql.Append(" from  " + pref + DBManager.sDataAliasRest + "prm_1 p ");
                        sql.Append(" where nzp_prm=6 and p.nzp=selected_kvars.nzp_kvar ");
                        sql.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
                        ExecSQL(conn_db, sql.ToString(), true);

                        #endregion

                        #region Вычисляем площадь по квартирам
                        sql.Remove(0, sql.Length);
                        sql.Append(" update  selected_kvars  set pl_kvar= ");
                        sql.Append(" (select sum(p.val_prm" + DBManager.sConvToNum + ") as pl_kvar ");
                        sql.Append(" from  " + pref + DBManager.sDataAliasRest + "prm_1 p ");
                        sql.Append(" where nzp_prm=4 and p.nzp=selected_kvars.nzp_kvar ");
                        sql.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
                        ExecSQL(conn_db, sql.ToString(), true);
                        #endregion

                        #region Вычисляем кофортность
                        sql.Remove(0, sql.Length);
                        sql.Append(" update selected_kvars set isol =2 ");
                        sql.Append(" where nzp_kvar in (select nzp ");
                        sql.Append(" from " + pref + DBManager.sDataAliasRest + "prm_1 p ");
                        sql.Append(" where nzp_prm=3 and p.nzp=selected_kvars.nzp_kvar ");
                        sql.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "' and val_prm='2')");
                        ExecSQL(conn_db, sql.ToString(), true);
                        #endregion

                        #region Подсчет начислений
                        sql.Remove(0, sql.Length);
                        sql.Append(" create temp table t_nachdom( ");
                        sql.Append(" nzp_kvar integer,  ");
                        sql.Append(" nzp_supp integer,  ");
                        sql.Append(" nzp_serv integer,  ");
                        sql.Append(" count_gil integer,  ");
                        sql.Append(" has_serv integer default 0,  ");
                        sql.Append(" pl_kvar " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" tarif " + DBManager.sDecimalType + "(14,3), rashod ");
                        sql.Append(DBManager.sDecimalType + "(14,4) default 0,");
                        sql.Append(" rashod_gkal " + DBManager.sDecimalType + "(14,4) default 0, ");
                        sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" reval_k " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" reval_d " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" real_charge_k " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" real_charge_d " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2) default 0, ");
                        sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2) default 0,");
                        sql.Append(" measure char(20))" + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);




                        #region Выборка начислений
                        string db_charge = pref + "_charge_" + (prm_.year_ - 2000).ToString("00");
                        if (DBManager.getServer(conn_db).Trim() != "")
                            db_charge = db_charge + "@" + DBManager.getServer(conn_db);
                        db_charge += DBManager.tableDelimiter;

                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_nachdom(nzp_kvar, nzp_supp,  nzp_serv, rsum_tarif,tarif, rashod, ");
                        sql.Append(" rashod_gkal, sum_nedop, reval_k, reval_d, real_charge_k, real_charge_d, ");
                        sql.Append(" sum_charge, sum_money, measure)");
                        sql.Append(" select d.nzp_kvar, nzp_supp, d.nzp_serv, sum(rsum_tarif),  sum(d.tarif), ");
                        sql.Append(" sum(cast (0 as " + DBManager.sDecimalType + "(14,4))), ");
                        sql.Append(" sum(cast (0 as " + DBManager.sDecimalType + "(14,4))), ");

                        sql.Append(" sum(sum_nedop), ");

                        sql.Append(" sum(case when reval<0 then reval else 0 end) as reval_k, ");
                        sql.Append(" sum(case when reval>0 then reval else 0 end) as reval_d, ");
                        sql.Append(" sum(case when real_charge<0 then real_charge else 0 end) as real_charge_k,");
                        sql.Append(" sum(case when real_charge<0 then 0 else real_charge end) as real_charge_d, ");
                        sql.Append(" sum(sum_charge), sum(sum_money), max(s.ed_izmer) ");
                        sql.Append(" from  " + db_charge + "charge_" + prm_.month_.ToString("00") + " d, selected_kvars sp, ");
                        sql.Append(" outer " + Points.Pref + DBManager.sKernelAliasRest + "formuls f, ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s ");
                        sql.Append(" where d.nzp_kvar=sp.nzp_kvar and d.nzp_serv=s.nzp_serv ");
                        sql.Append(" and d.nzp_frm=f.nzp_frm  ");
                        sql.Append(" and d.nzp_serv>1 and dat_charge is null");
                        sql.Append(where_supp + where_serv);
                        sql.Append(" group by 1,2,3 ");
                        ExecSQL(conn_db, sql.ToString(), true);


                        //rsh2
                        sql.Remove(0, sql.Length);

                        sql.Append(" update t_nachdom set pl_kvar= (select max(squ) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp ),");
                        sql.Append(" count_gil  = (select max(gil) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //Основную услугу
                        sql.Remove(0, sql.Length);

                        sql.Append(" update t_nachdom set rashod = (select sum(valm+dlt_reval) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv not in (8,9) and (nzp_serv <510 or nzp_serv>518) ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod_gkal = (select sum(case when nzp_frm_type=1 valm+dlt_reval) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv in (8,9)");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod = (select sum(squ) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv = 8 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //Все кроме подогрева так как другая единица измерения
                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod = (select  ");
                        sql.Append(" sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
                        sql.Append("          else dop87 end) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and 6=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv = 510 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod = (select  ");
                        sql.Append(" sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
                        sql.Append("          else dop87 end) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and 14=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv = 514 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod = (select  ");
                        sql.Append(" sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
                        sql.Append("          else dop87 end) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar   ");
                        sql.Append(" and 25=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv = 515 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //ОДН подогрев
                        sql.Remove(0, sql.Length);
                        sql.Append(" update t_nachdom set rashod_gkal = (select  ");
                        sql.Append(" sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
                        sql.Append("          else dop87 end) ");
                        sql.Append(" from  " + db_charge + "calc_gku_" + prm_.month_.ToString("00") + " d ");
                        sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0  ");
                        sql.Append(" and 9 =d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )");
                        sql.Append(" where nzp_serv=513 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //04.06.2014 по решению Татьяны
                        //sql.Remove(0, sql.Length);
                        //sql.Append(" update t_nachdom set pl_kvar = (select max(pl_kvar) ");
                        //sql.Append(" from  selected_kvars d");
                        //sql.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar ");
                        //sql.Append(" and isol=2 )");
                        //sql.Append(" where (nzp_serv in (select nzp_serv from " + pref + DBManager.sKernelAliasRest +
                        //    "s_counts where nzp_serv<>8) or ");
                        //sql.Append(" nzp_serv in (select nzp_serv from " + pref + DBManager.sKernelAliasRest +
                        //    "serv_odn where nzp_serv<>512)) and ");
                        //sql.Append(" nzp_kvar in (select nzp_kvar from selected_kvars where isol=2) ");
                        //ExecSQL(conn_db, sql.ToString(), true);





                        #region Выборка перерасчетов прошлого периода

                        sql.Remove(0, sql.Length);
                        sql.Append(" CREATE TEMP TABLE t_nedop(");
                        sql.Append("       nzp_kvar integer, ");
                        sql.Append("       month_s integer,");
                        sql.Append("       month_po integer)" + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO  t_nedop(nzp_kvar, month_s, month_po) ");
                        sql.Append(" SELECT a.nzp_kvar, min(EXTRACT (year FROM dat_s)*12+EXTRACT (month FROM dat_s)) as month_s,  max(EXTRACT (year FROM dat_po)*12+EXTRACT (month FROM dat_po)) as month_po");
                        sql.Append(" FROM " + pref + DBManager.sDataAliasRest + "nedop_kvar a, ");
                        sql.Append("       selected_kvars b ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar ");
                        sql.Append("       AND month_calc='01." + prm_.month_.ToString("00") + "." + prm_.year_.ToString("0000") + "'  ");
                        sql.Append(" GROUP BY 1 ");
                        ExecSQL(conn_db, sql.ToString(), true);


                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT month_, year_ ");
                        sql.Append(" FROM " + db_charge + "lnk_charge_" + prm_.month_.ToString("00") + " b, t_nedop d ");
                        sql.Append(" WHERE  b.nzp_kvar=d.nzp_kvar ");
                        sql.Append("       AND year_*12+month_>=month_s ");
                        sql.Append("       AND  year_*12+month_<=month_po");
                        sql.Append(" GROUP BY 1,2");
                        if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                        {
                            throw new Exception("Ошибка выборки перерасчетов");
                        }
                        while (reader2.Read())
                        {
                            string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_nachdom (nzp_kvar, nzp_supp, nzp_serv, sum_nedop, reval_k, reval_d, ");
                            sql.Append("               real_charge_k, real_charge_d) ");
                            sql.Append(" SELECT d.nzp_kvar, nzp_supp, nzp_serv, ");
                            sql.Append("       sum(sum_nedop-sum_nedop_p),  ");
                            sql.Append("       sum(case when (sum_nedop-sum_nedop_p)>0 ");
                            sql.Append("       then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                            sql.Append("       sum(case when (sum_nedop-sum_nedop_p)>0 ");
                            sql.Append("       then 0 else sum_nedop-sum_nedop_p end ) as reval_d, sum(0), sum(0)");
                            sql.Append(" FROM " + sTmpAlias + DBManager.tableDelimiter + "charge_" +
                                Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                            sql.Append(" b, selected_kvars d ");
                            sql.Append(" WHERE  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                            sql.Append(prm_.month_.ToString("00") + "." + prm_.year_.ToString() + "')");
                            sql.Append(where_serv + where_supp);
                            sql.Append(" GROUP BY 1,2,3");
                            ExecSQL(conn_db, sql.ToString(), true);


                        }
                        reader2.Close();
                        ExecSQL(conn_db, "drop table t_nedop", true);

                        #endregion


                        #endregion

                        ExecSQL(conn_db, "create index ix_tmp_tnch_01 on t_nachdom(nzp_kvar,nzp_supp,nzp_serv)", true);
                        ExecSQL(conn_db, DBManager.sUpdStat + " t_nachdom", true);

                        ExecSQL(conn_db, "drop table t12", false);


                        sql.Remove(0, sql.Length);
                        sql.Append(" CREATE TEMP TABLE t12 (  ");
                        sql.Append("   nzp_kvar integer, ");
                        sql.Append("   nzp_supp integer) " + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);


                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t12(nzp_kvar, nzp_supp) ");
                        sql.Append(" SELECT nzp_kvar, nzp_supp  ");
                        sql.Append(" FROM t_nachdom ");
                        sql.Append(" WHERE nzp_serv = 9 ");
                        sql.Append(" GROUP BY 1,2  ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        ExecSQL(conn_db, "create index ix_tmp_862 on t12(nzp_kvar, nzp_supp)", true);

                        ExecSQL(conn_db, DBManager.sUpdStat + " t12", true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" UPDATE t_nachdom SET nzp_serv=9 ");
                        sql.Append(" WHERE nzp_serv = 14 AND 0<(select count(*) ");
                        sql.Append(" FROM t12 ");
                        sql.Append(" WHERE t12.nzp_supp=t_nachdom.nzp_supp ");
                        sql.Append("       AND t12.nzp_kvar=t_nachdom.nzp_kvar) ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" UPDATE t_nachdom SET nzp_serv=513 ");
                        sql.Append(" WHERE nzp_serv = 514 AND 0<(select count(*) ");
                        sql.Append(" FROM t12 ");
                        sql.Append(" WHERE t12.nzp_supp=t_nachdom.nzp_supp ");
                        sql.Append("       AND t12.nzp_kvar=t_nachdom.nzp_kvar) ");
                        ExecSQL(conn_db, sql.ToString(), true);


                        ExecSQL(conn_db, "drop table t12", true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" CREATE TEMP TABLE t12 (  ");
                        sql.Append("   nzp_kvar integer, ");
                        sql.Append("   nzp_supp integer) " + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" SELECT nzp_kvar, nzp_supp ");
                        sql.Append(" FROM t_nachdom a ");
                        sql.Append(" WHERE nzp_serv = 515 and 0=(select count(*) from t_nachdom b");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar ");
                        sql.Append("       AND a.nzp_supp=b.nzp_supp ");
                        sql.Append("       AND b.nzp_serv=25 and b.rsum_tarif>0)");
                        sql.Append("       AND rsum_tarif>0 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_nachdom(nzp_kvar, nzp_supp,  nzp_serv, rsum_tarif,tarif, rashod, ");
                        sql.Append("             sum_nedop, reval_k, reval_d, real_charge_k, real_charge_d, ");
                        sql.Append("             sum_charge, sum_money, measure, has_serv)");
                        sql.Append(" SELECT nzp_kvar, nzp_supp, 25,0,0,0,0,0,0,0,0,0,0,'Квт*час',1 ");
                        sql.Append(" FROM t12 a ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        ExecSQL(conn_db, "drop table t12", true);


                        sql.Remove(0, sql.Length);
                        sql.Append(" CREATE TEMP TABLE t_nachdom1( ");
                        sql.Append("       nzp_kvar integer,");
                        sql.Append("       nzp_supp integer,");
                        sql.Append("       nzp_serv integer,");
                        sql.Append("       rsum_tarif " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       rashod " + DBManager.sDecimalType + "(14,4),");
                        sql.Append("       rashod_gkal " + DBManager.sDecimalType + "(14,4),");
                        sql.Append("       measure CHAR(10), ");
                        sql.Append("       sum_nedop " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       reval_k " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       reval_d " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       real_charge_k " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       real_charge_d " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       sum_charge " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       sum_money " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       pl_kvar " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       count_gil integer, ");
                        sql.Append("       has_serv integer)" + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_nachdom1 (nzp_kvar, nzp_supp, nzp_serv, rsum_tarif, ");
                        sql.Append("       rashod, rashod_gkal, measure, sum_nedop, ");
                        sql.Append("       reval_k, reval_d, real_charge_k, real_charge_d, ");
                        sql.Append("       sum_charge, sum_money, pl_kvar, count_gil, has_serv) ");
                        sql.Append(" SELECT nzp_kvar, nzp_supp, nzp_serv, sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("       sum(rashod) as rashod, sum(rashod_gkal) as rashod_gkal, ");
                        sql.Append("       max(measure) as measure, sum(sum_nedop) as sum_nedop, ");
                        sql.Append("       sum(reval_k) as reval_k, sum(reval_d) as reval_d, ");
                        sql.Append("       sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                        sql.Append("       sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, ");
                        sql.Append("       max(pl_kvar) as pl_kvar, ");
                        sql.Append("       max(count_gil) as count_gil, max(has_serv) as has_serv ");
                        sql.Append(" FROM  t_nachdom ");
                        sql.Append(" GROUP BY 1,2,3 ");
                        ExecSQL(conn_db, sql.ToString(), true);



                        sql.Remove(0, sql.Length);
                        sql.Append(" CREATE TEMP TABLE t_nachdom2( ");
                        sql.Append("       nzp_kvar integer,");
                        sql.Append("       nzp_serv integer,");
                        sql.Append("       rsum_tarif " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       rashod " + DBManager.sDecimalType + "(14,4),");
                        sql.Append("       rashod_gkal " + DBManager.sDecimalType + "(14,4),");
                        sql.Append("       measure CHAR(10), ");
                        sql.Append("       sum_nedop " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       reval_k " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       reval_d " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       real_charge_k " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       real_charge_d " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       sum_charge " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       sum_money " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       pl_kvar " + DBManager.sDecimalType + "(14,2),");
                        sql.Append("       count_gil integer, ");
                        sql.Append("       has_serv integer)" + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_nachdom2 (nzp_kvar, nzp_serv, rsum_tarif, ");
                        sql.Append("       rashod, rashod_gkal, measure, sum_nedop, ");
                        sql.Append("       reval_k, reval_d, real_charge_k, real_charge_d, ");
                        sql.Append("       sum_charge, sum_money, pl_kvar, count_gil, has_serv) ");
                        sql.Append(" SELECT  nzp_kvar, nzp_serv, sum(rsum_tarif) as rsum_tarif, sum(rashod) as rashod, ");
                        sql.Append("       sum(rashod_gkal) as rashod_gkal, max(measure) as measure, sum(sum_nedop) as sum_nedop, ");
                        sql.Append("       sum(coalesce(reval_k,0)) as reval_k, ");
                        sql.Append("       sum(coalesce(reval_d,0)) as reval_d, ");
                        sql.Append("       sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                        sql.Append("       sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, ");
                        sql.Append("       max(pl_kvar) as pl_kvar, ");
                        sql.Append("       max(count_gil) as count_gil, max(has_serv) as has_serv ");
                        sql.Append(" FROM  t_nachdom ");
                        sql.Append(" GROUP BY 1,2 ");
                        ExecSQL(conn_db, sql.ToString(), true);


                        #region Удаление временных таблиц
                        ExecSQL(conn_db, "drop table t_nachdom ", true);


                        #endregion

                        #endregion

                        #region Вывод списка в таблицу
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod(tip, geu, sort, name_supp, service, ");
                        sql.Append("                    ordering, ord_serv, isol_pl, komm_pl, ");
                        sql.Append("                    count_gil, rashod, rashod_gkal, measure, count_ls, ");
                        sql.Append("                    rsum_tarif, sum_nedop, reval_k, reval_d, ");
                        sql.Append("                    real_charge_k, real_charge_d, ");
                        sql.Append("                    sum_charge, sum_money, has_serv) ");
                        sql.Append(" SELECT 2 as tip, geu, 1, name_supp, service, s.ordering, ");
                        sql.Append("        (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                        sql.Append("        sum(case when isol=2 then 0 else coalesce(a.pl_kvar,0) end) as isol_pl, ");
                        sql.Append("        sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                        sql.Append("        sum(count_gil) as count_gil, ");
                        sql.Append("        sum(rashod) as rashod, ");
                        sql.Append("        sum(rashod_gkal) as rashod_gkal, ");
                        sql.Append("        max(measure) as measure, ");
                        sql.Append("        count(b.nzp_kvar) as count_ls,");
                        sql.Append("        sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("        sum(sum_nedop) as sum_nedop, ");
                        sql.Append("        sum(-1*reval_k) as reval_k, ");
                        sql.Append("        sum(reval_d) as reval_d, ");
                        sql.Append("        sum(real_charge_k) as real_charge_k, ");
                        sql.Append("        sum(real_charge_d) as real_charge_d, ");
                        sql.Append("        sum(sum_charge) as sum_charge, ");
                        sql.Append("        sum(sum_money) as sum_money, ");
                        sql.Append("        max(has_serv) ");
                        sql.Append(" FROM  selected_kvars b, t_nachdom1 a,  ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su, ");
                        sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_geu sg ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar ");
                        sql.Append("        AND a.nzp_serv=s.nzp_serv ");
                        sql.Append("        AND a.nzp_supp=su.nzp_supp ");
                        sql.Append("        AND b.nzp_geu=sg.nzp_geu ");
                        sql.Append(" GROUP BY 1,2,3,4,5,6,7");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod(tip, geu, sort, name_supp, service, ");
                        sql.Append("                    ordering, ord_serv, isol_pl, komm_pl, ");
                        sql.Append("                    count_gil, rashod, rashod_gkal, measure, count_ls, ");
                        sql.Append("                    rsum_tarif, sum_nedop, reval_k, reval_d, ");
                        sql.Append("                    real_charge_k, real_charge_d, ");
                        sql.Append("                    sum_charge, sum_money, has_serv) ");
                        sql.Append(" SELECT 2 as tip, geu,2, 'Итого ' as name_supp, service, s.ordering, ");
                        sql.Append("        (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                        sql.Append("        sum(case when isol=2 then 0 else coalesce(a.pl_kvar,0) end) as isol_pl, ");
                        sql.Append("        sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                        sql.Append("        sum(count_gil) as count_gil,  sum(rashod) as rashod, ");
                        sql.Append("        sum(rashod_gkal) as rashod_gkal,  max(measure) as measure,  ");
                        sql.Append("        count(b.nzp_kvar) as count_ls, ");
                        sql.Append("        sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("        sum(sum_nedop) as sum_nedop, ");
                        sql.Append("        sum(-1*(reval_k+real_charge_k)) as reval_k, ");
                        sql.Append("        sum(reval_d+real_charge_d) as reval_d, ");
                        sql.Append("        sum(real_charge_k) as real_charge_k, ");
                        sql.Append("        sum(real_charge_d) as real_charge_d, ");
                        sql.Append("        sum(sum_charge) as sum_charge, ");
                        sql.Append("        sum(sum_money) as sum_money, ");
                        sql.Append("        max(has_serv) ");
                        sql.Append(" from  selected_kvars b, ");
                        sql.Append("       t_nachdom2 a,  ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
                        sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_geu sg ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar AND a.nzp_serv=s.nzp_serv ");
                        sql.Append("        AND b.nzp_geu=sg.nzp_geu ");
                        sql.Append(" GROUP BY 1,2,3,4,5,6,7");
                        ExecSQL(conn_db, sql.ToString(), true);


                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod(tip, geu, sort, name_supp, service, ");
                        sql.Append("                    ordering, ord_serv, isol_pl, komm_pl, ");
                        sql.Append("                    count_gil, rashod, rashod_gkal, measure, count_ls, ");
                        sql.Append("                    rsum_tarif, sum_nedop, reval_k, reval_d, ");
                        sql.Append("                    real_charge_k, real_charge_d, ");
                        sql.Append("                    sum_charge, sum_money, has_serv) ");
                        sql.Append(" SELECT 1 as tip, '-1', 1, name_supp, service, s.ordering, ");
                        sql.Append("        (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                        sql.Append("        sum(case when isol=2 then 0 else coalesce(a.pl_kvar,0) end) as isol_pl, ");
                        sql.Append("        sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                        sql.Append("        sum(count_gil) as count_gil,  sum(rashod) as rashod, ");
                        sql.Append("        sum(rashod_gkal) as rashod_gkal,");
                        sql.Append("        max(measure) as measure,  ");
                        sql.Append("        count(b.nzp_kvar) as count_ls, ");
                        sql.Append("        sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("        sum(sum_nedop) as sum_nedop, ");
                        sql.Append("        sum(-1*(reval_k+real_charge_k)) as reval_k, ");
                        sql.Append("        sum(reval_d+real_charge_d) as reval_d, ");
                        sql.Append("        sum(real_charge_k) as real_charge_k, ");
                        sql.Append("        sum(real_charge_d) as real_charge_d, ");
                        sql.Append("        sum(sum_charge) as sum_charge, ");
                        sql.Append("        sum(sum_money) as sum_money, ");
                        sql.Append("        max(has_serv) ");
                        sql.Append(" FROM  selected_kvars b, t_nachdom1 a,  ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su, ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
                        sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_geu sg ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar AND a.nzp_serv=s.nzp_serv ");
                        sql.Append("        AND a.nzp_supp=su.nzp_supp AND b.nzp_geu=sg.nzp_geu ");
                        sql.Append(" GROUP BY 1,2,3,4,5,6,7");
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod(tip, geu, sort, name_supp, service, ");
                        sql.Append("                    ordering, ord_serv, isol_pl, komm_pl, ");
                        sql.Append("                    count_gil, rashod, rashod_gkal, measure, count_ls, ");
                        sql.Append("                    rsum_tarif, sum_nedop, reval_k, reval_d, ");
                        sql.Append("                    real_charge_k, real_charge_d, ");
                        sql.Append("                    sum_charge, sum_money, has_serv) ");
                        sql.Append(" select 1 as tip, '-1',2, 'Итого ' as name_supp, service, s.ordering, ");
                        sql.Append("        (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                        sql.Append("        sum(case when isol=2 then 0 else coalesce(a.pl_kvar,0) end) as isol_pl, ");
                        sql.Append("        sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                        sql.Append("        sum(count_gil) as count_gil,  ");
                        sql.Append("        sum(rashod) as rashod, ");
                        sql.Append("        sum(rashod_gkal) as rashod_gkal, ");
                        sql.Append("        max(measure) as measure, ");
                        sql.Append("        count(b.nzp_kvar) as count_ls, ");
                        sql.Append("        sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("        sum(sum_nedop) as sum_nedop, ");
                        sql.Append("        sum(-1*(reval_k+real_charge_k)) as reval_k, ");
                        sql.Append("        sum(reval_d+real_charge_d) as reval_d, ");
                        sql.Append("        sum(real_charge_k) as real_charge_k, ");
                        sql.Append("        sum(real_charge_d) as real_charge_d, ");
                        sql.Append("        sum(sum_charge) as sum_charge, ");
                        sql.Append("        sum(sum_money) as sum_money, ");
                        sql.Append("        max(has_serv) ");
                        sql.Append(" FROM  selected_kvars b, t_nachdom2 a,  ");
                        sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s, ");
                        sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_geu sg ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv ");
                        sql.Append("       AND b.nzp_geu=sg.nzp_geu ");
                        sql.Append(" GROUP BY 1,2,3,4,5,6,7");
                        ExecSQL(conn_db, sql.ToString(), true);
                        #endregion

                    }

                }
                reader.Close();
                #endregion
                #region Выборка данных на экран
                sql.Remove(0, sql.Length);
                sql.Append(" select tip, geu, sort, name_supp, service, ");
                sql.Append("        ordering, ord_serv, sum(coalesce(isol_pl,0)) as isol_pl, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(komm_pl,0)) as komm_pl,");
                sql.Append("        sum(" + DBManager.sNvlWord + "(count_gil,0)) as count_gil, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(rashod,0)) as rashod, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(rashod_gkal,0)) as rashod_gkal,");
                sql.Append("        max(" + DBManager.sNvlWord + "(measure,'')) as measure, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(count_ls,0)) as count_ls, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(rsum_tarif,0)) as rsum_tarif, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(sum_nedop,0)) as sum_nedop, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(reval_d,0)) as reval_d, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(real_charge_k,0)) as real_charge_k, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(real_charge_d,0)) as real_charge_d, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, ");
                sql.Append("        sum(" + DBManager.sNvlWord + "(sum_money,0)) as sum_money ");
                sql.Append(" FROM t_svod ");
                sql.Append(" WHERE abs(" + DBManager.sNvlWord + "(rsum_tarif,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(sum_nedop,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(reval_k,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(reval_d,0))+ ");
                sql.Append("       abs(" + DBManager.sNvlWord + "(sum_charge,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(sum_money,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(real_charge_k,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(real_charge_d,0))+ ");
                sql.Append("       abs(" + DBManager.sNvlWord + "(sum_money,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(has_serv,0))+");
                sql.Append("       abs(" + DBManager.sNvlWord + "(rashod,0)) >0.001 ");
                sql.Append(" GROUP BY 1,2,3,4,5,6,7 ");
                sql.Append(" ORDER BY 1,2,3,4,6,7,5 ");
                LocalTable = DBManager.ExecSQLToTable(conn_db, sql.ToString());



                #region Удаление временных таблиц
                ExecSQL(conn_db, "drop table selected_kvars", true);
                ExecSQL(conn_db, "drop table t_nachdom1 ", true);
                ExecSQL(conn_db, "drop table t_nachdom2 ", true);
                #endregion

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                ExecSQL(conn_db, "drop table sel_kvar5", false);
                conn_db.Close();
            }



            return LocalTable;


        }


    }
}
