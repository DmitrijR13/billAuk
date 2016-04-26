using System;
using System.Data;
using System.Windows.Forms.VisualStyles;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {

        /// <summary>
        /// Отчет по поставщикам
        /// </summary>
        /// <param name="prm_">Параметры поиска</param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user">Потльзователь</param>
        /// <returns>DataTable</returns>
        public DataTable GetSpravSuppNachHar(Prm prm_, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);
            // StreamWriter sw = new StreamWriter(@"C:/temp/qazxsw.txt", false);

            ret = Utils.InitReturns();

            string dat = "01." + prm_.month_.ToString("00") + "." + prm_.year_.ToString();
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
            DataTable LocalTable = new DataTable();
#if PG
            string tXX_spls = defaultPgSchema + "." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string pref = "";
            string where_supp = "";
            string where_serv = "";
            //sw.WriteLine(tXX_spls);
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
            #endregion


            //sw.WriteLine("25");
            #region Выборка по локальным банкам



            ExecSQL(conn_db, "drop table t_svod", false);
            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" create temp table t_svod( ");
            sql.Append("  tip integer, ");
            sql.Append("  geu char(40), ");
            sql.Append("  sort integer, ");
            sql.Append("  name_supp char(100), ");
            sql.Append("  service char(50), ");
            sql.Append("  ord_serv integer, ");
            sql.Append("  has_serv integer default 0, ");
            sql.Append("  ordering integer, ");
            sql.Append("  isol_pl numeric(14,2), ");
            sql.Append("  komm_pl numeric(14,2), ");
            sql.Append("  count_gil integer, ");
            sql.Append("  rsum_tarif numeric(14,2), ");
            sql.Append("  sum_nedop numeric(14,2), ");
            sql.Append("  reval_k numeric(14,2), ");
            sql.Append("  reval_d numeric(14,2), ");
            sql.Append("  sum_otopl numeric(14,2), ");
            sql.Append("  real_charge_k numeric(14,2), ");
            sql.Append("  real_charge_d numeric(14,2), ");
            sql.Append("  sum_charge numeric(14,2), ");
            sql.Append("  sum_money numeric(14,2)) ");
#else
            sql.Append(" create temp table t_svod( ");
            sql.Append("  tip integer, ");
            sql.Append("  geu char(40), ");
            sql.Append("  sort integer, ");
            sql.Append("  name_supp char(100), ");
            sql.Append("  service char(50), ");
            sql.Append("  ord_serv integer, ");
            sql.Append("  has_serv integer default 0, ");
            sql.Append("  ordering integer, ");
            sql.Append("  isol_pl Decimal(14,2), ");
            sql.Append("  komm_pl Decimal(14,2), ");
            sql.Append("  count_gil integer, ");
            sql.Append("  rsum_tarif Decimal(14,2), ");
            sql.Append("  sum_nedop Decimal(14,2), ");
            sql.Append("  reval_k Decimal(14,2), ");
            sql.Append("  reval_d Decimal(14,2), ");
            sql.Append("  sum_otopl Decimal(14,2), ");
            sql.Append("  real_charge_k Decimal(14,2), ");
            sql.Append("  real_charge_d Decimal(14,2), ");
            sql.Append("  sum_charge Decimal(14,2), ");
            sql.Append("  sum_money Decimal(14,2)) with no log");
#endif
            ExecSQL(conn_db, sql.ToString(), true);
            //sw.WriteLine("26");

            #region Подготовка списка лицевых счетов
            ExecSQL(conn_db, "drop table sel_kvar5", false);

            sql.Remove(0, sql.Length);
            sql.Append(" select * ");
#if PG
            sql.Append(" into temp sel_kvar5 from  " + tXX_spls);
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
            //sw.WriteLine("27");
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

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create temp table selected_kvars( ");
                    sql2.Append(" nzp_geu integer,  ");
                    sql2.Append(" nzp_kvar integer,  ");
                    sql2.Append(" pl_kvar_n numeric(14,2)  default 0,  ");
                    //sql2.Append(" count_gil integer,  ");
                    sql2.Append(" pl_kvar numeric(14,2)  default 0,  ");
                    sql2.Append(" isol integer default 1)   ");
#else
                    sql2.Append(" create temp table selected_kvars( ");
                    sql2.Append(" nzp_geu integer,  ");
                    sql2.Append(" nzp_kvar integer,  ");
                    sql2.Append(" pl_kvar_n Decimal(14,2)  default 0,  ");
                    //sql2.Append(" count_gil integer,  ");
                    sql2.Append(" pl_kvar Decimal(14,2)  default 0,  ");
                    sql2.Append(" isol integer default 1) with no log  ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), false);

                    #endregion

                    #region Заносим список квартир
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into selected_kvars (nzp_kvar,nzp_geu) ");
                    sql2.Append(" select nzp_kvar,nzp_geu from sel_kvar5 where pref='" + pref + "'");
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }


                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" drop index ix_tmp_ts99 ");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" create index ix_tmp_ts99 on selected_kvars(nzp_kvar)");
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    #endregion

                    #region Вычисляем площадь по коммунальным квартирам
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update  selected_kvars  set pl_kvar= ");
                    sql2.Append(" (select sum(p.val_prm::numeric::int) as pl_kvar ");
                    sql2.Append(" from  " + pref + "_data.prm_1 p ");
                    sql2.Append(" where nzp_prm=4 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
#else
                    sql2.Append(" update  selected_kvars  set pl_kvar= ");
                    sql2.Append(" (select sum(Replace(p.val_prm, ',','.')+0) as pl_kvar ");
                    sql2.Append(" from  " + pref + "_data:prm_1 p ");
                    sql2.Append(" where nzp_prm=4 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
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
                    sql2.Append(" update  selected_kvars  set pl_kvar_n= ");
                    sql2.Append(" (select sum(p.val_prm::numeric) as pl_kvar ");
                    sql2.Append(" from  " + pref + "_data.prm_1 p ");
                    sql2.Append(" where nzp_prm=6 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
#else
                    sql2.Append(" update  selected_kvars  set pl_kvar_n= ");
                    sql2.Append(" (select sum(Replace(p.val_prm, ',','.')+0) as pl_kvar ");
                    sql2.Append(" from  " + pref + "_data:prm_1 p ");
                    sql2.Append(" where nzp_prm=6 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");
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



                    //#region Вычисляем количество жильцов по дому
                    //sql2.Remove(0, sql2.Length);
                    //sql2.Append(" update selected_kvars set count_gil = ");
                    //sql2.Append(" (select sum(val_prm+0) as pl_kvar ");
                    //sql2.Append(" from " + pref + "_data:prm_1 p ");
                    //sql2.Append(" where nzp_prm=5 and p.nzp=selected_kvars.nzp_kvar ");
                    //sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "')");

                    //if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //    reader.Close();
                    //    conn_db.Close();
                    //    ret.result = false;
                    //    return null;
                    //}
                    //#endregion

                    #region Вычисляем кофортность
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update selected_kvars set isol =2 ");
                    sql2.Append(" where nzp_kvar in (select nzp ");
                    sql2.Append(" from " + pref + "_data.prm_1 p ");
                    sql2.Append(" where nzp_prm=3 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "' and val_prm='2')");
#else
                    sql2.Append(" update selected_kvars set isol =2 ");
                    sql2.Append(" where nzp_kvar in (select nzp ");
                    sql2.Append(" from " + pref + "_data:prm_1 p ");
                    sql2.Append(" where nzp_prm=3 and p.nzp=selected_kvars.nzp_kvar ");
                    sql2.Append(" and p.is_actual=1 and p.dat_s<='" + dat + "' and p.dat_po>='" + dat + "' and val_prm='2')");
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
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create temp table t_nachdom( ");
                    sql2.Append(" nzp_kvar integer,  ");
                    sql2.Append(" nzp_supp integer,  ");
                    sql2.Append(" name_supp CHARACTER(100), ");
                    sql2.Append(" nzp_serv integer,  ");
                    sql2.Append(" count_gil integer,  ");
                    sql2.Append(" has_serv integer default 0,  ");
                    sql2.Append(" pl_kvar numeric(14,2) default 0, ");
                    sql2.Append(" rsum_tarif numeric(14,2) default 0, ");
                    sql2.Append(" sum_nedop numeric(14,2) default 0, ");
                    sql2.Append(" sum_nedop_cor numeric(14,2) default 0, ");
                    sql2.Append(" reval_k numeric(14,2) default 0, ");
                    sql2.Append(" reval_d numeric(14,2) default 0, ");
                    sql2.Append(" real_charge Decimal(14,2) default 0, ");
                    sql2.Append(" sum_otopl numeric(14,2) default 0, ");
                    sql2.Append(" real_charge_k numeric(14,2) default 0, ");
                    sql2.Append(" real_charge_d numeric(14,2) default 0, ");
                    sql2.Append(" sum_charge numeric(14,2) default 0, ");
                    sql2.Append(" sum_money numeric(14,2) default 0)   ");
#else
                    sql2.Append(" create temp table t_nachdom( ");
                    sql2.Append(" nzp_kvar integer,  ");
                    sql2.Append(" nzp_supp integer,  ");
                    sql2.Append(" name_supp CHARACTER(100), ");     
                    sql2.Append(" nzp_serv integer,  ");
                    sql2.Append(" count_gil integer,  ");
                    sql2.Append(" has_serv integer default 0,  ");
                    sql2.Append(" pl_kvar Decimal(14,2) default 0, ");
                    sql2.Append(" rsum_tarif Decimal(14,2) default 0, ");
                    sql2.Append(" sum_nedop Decimal(14,2) default 0, ");
                    sql2.Append(" sum_nedop_cor Decimal(14,2) default 0, ");
                    sql2.Append(" reval_k Decimal(14,2) default 0, ");
                    sql2.Append(" reval_d Decimal(14,2) default 0, ");
                    sql2.Append(" real_charge Decimal(14,2) default 0, ");
                    sql2.Append(" sum_otopl Decimal(14,2) default 0, ");
                    sql2.Append(" real_charge_k Decimal(14,2) default 0, ");
                    sql2.Append(" real_charge_d Decimal(14,2) default 0, ");
                    sql2.Append(" sum_charge Decimal(14,2) default 0, ");
                    sql2.Append(" sum_money Decimal(14,2) default 0) with no log   ");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }


                    #region Выборка статистики из начислений
                    string db_charge = pref + "_charge_" + (prm_.year_ - 2000).ToString("00");

                    string conn_db_name = "";
                    try
                    {
                        conn_db_name = DBManager.getServer(conn_db).Trim();
                    }
                    catch
                    {
                        conn_db_name = "";
                    }
#if PG
                    if (conn_db_name != "")
                        db_charge = db_charge + DBManager.getServer(conn_db);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_nachdom(nzp_kvar, nzp_supp,  nzp_serv, rsum_tarif, ");
                    sql2.Append(" sum_nedop, reval_k, reval_d, real_charge, real_charge_k, real_charge_d, ");
                    sql2.Append(" sum_charge, sum_money)");
                    sql2.Append(" select d.nzp_kvar, nzp_supp, nzp_serv, sum(rsum_tarif), sum(sum_nedop), ");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d, ");
                    sql2.Append(" sum(real_charge) as real_charge, ");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) as real_charge_k,");
                    sql2.Append(" sum(case when real_charge<0 then 0 else real_charge end) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge), sum(sum_money) ");
                    sql2.Append(" from  " + db_charge + ".charge_" + prm_.month_.ToString("00") + " d, selected_kvars sp ");
                    sql2.Append(" where d.nzp_kvar=sp.nzp_kvar ");
                    sql2.Append(" and nzp_serv>1 and dat_charge is null");
                    sql2.Append(where_supp + where_serv);
                    sql2.Append(" group by 1,2,3 ");
#else
                    if (DBManager.getServer(conn_db).Trim() != "") db_charge = db_charge + "@" + DBManager.getServer(conn_db);
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_nachdom(nzp_kvar, nzp_supp,  nzp_serv, rsum_tarif, ");
                    sql2.Append(" sum_nedop,reval_k, reval_d, real_charge,  real_charge_k, real_charge_d, ");  //  
                    sql2.Append(" sum_charge, sum_money)");
                    sql2.Append(" select d.nzp_kvar, nzp_supp, nzp_serv, sum(rsum_tarif), sum(sum_nedop), ");
                    sql2.Append(" sum(case when reval<0 then reval else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when reval>0 then reval else 0 end) as reval_d, ");
                    sql2.Append(" sum(real_charge) as real_charge, ");
                    sql2.Append(" sum(case when real_charge<0 then real_charge else 0 end) as real_charge_k,");
                    sql2.Append(" sum(case when real_charge<0 then 0 else real_charge end) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge), sum(sum_money) ");
                    sql2.Append(" from  " + db_charge + ":charge_" + prm_.month_.ToString("00") + " d, selected_kvars sp ");
                    sql2.Append(" where d.nzp_kvar=sp.nzp_kvar ");
                    sql2.Append(" and nzp_serv>1 and dat_charge is null");
                    sql2.Append(where_supp);
                    sql2.Append(" group by 1,2,3 ");
#endif

                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }


                    #region Корректировка по отоплению

                    string sql3 = "update t_nachdom set sum_otopl = " + DBManager.sNvlWord + "((select  " +
                                  " sum(sum_rcl) " +
                                  " from " + db_charge + DBManager.tableDelimiter + "perekidka a " +
                                  " where a.nzp_kvar=t_nachdom.nzp_kvar " +
                                  "         and type_rcl = 110  " +
                                  "         and month_=" + prm_.month_.ToString("00") +
                                  "         and abs(sum_rcl)>0.001 " + where_supp +
                                  "         and t_nachdom.nzp_serv=a.nzp_serv),0) " +
                                  " where nzp_serv = 8  ";
                    ExecSQL(conn_db, sql3, true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" UPDATE t_nachdom SET real_charge_k =real_charge_k - sum_otopl ");
                    sql2.Append(" where sum_otopl<0 ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" UPDATE t_nachdom SET real_charge_d =real_charge_d - sum_otopl ");
                    sql2.Append(" where sum_otopl>0 ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    #endregion


                    #region Жигулевск недопоставка как перекидка



                    sql3 = "update t_nachdom set sum_nedop_cor = (select  " +
                          " -sum(sum_rcl) " +
                          " from " + db_charge + DBManager.tableDelimiter + "perekidka a INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base " +
                          " where a.nzp_kvar=t_nachdom.nzp_kvar and type_rcl in (101, 102) and d.comment != 'Выравнивание сальдо' and month_=" + prm_.month_.ToString("00") +
                          " and abs(sum_rcl)>0.001 " + where_supp + " and t_nachdom.nzp_serv=a.nzp_serv)" +
                           " where real_charge_k<0 ";
                    ExecSQL(conn_db, sql3, true);

                    sql3 = " UPDATE t_nachdom set reval_d = reval_d + coalesce((SELECT reval  from(SELECT a.nzp_kvar, a.nzp_supp, a.nzp_serv, sum(sum_rcl) as reval from " +
                        db_charge + ".perekidka a " +
                  " INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base where month_ = " +
                  prm_.month_.ToString() + "  AND d.comment != 'Выравнивание сальдо' group by 1,2,3) t " +
                  " where t_nachdom.nzp_supp = t.nzp_supp and t_nachdom.nzp_kvar = t.nzp_kvar and t_nachdom.nzp_serv = t.nzp_serv and reval > 0), 0) where rsum_tarif > 0";

                    if (!ExecSQL(conn_db, sql3.ToString(), true).result)
                        return null;


                    sql3 = "update t_nachdom set sum_nedop = sum_nedop + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(conn_db, sql3, true);

                    sql3 = "update t_nachdom set real_charge_k = real_charge_k + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(conn_db, sql3, true);

                    #endregion

                    //rsh2
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update t_nachdom set pl_kvar = ((select max(squ)  ");
                    sql2.Append(" from  " + db_charge + ".calc_gku_" + prm_.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp )), ");

                    sql2.Append(" count_gil = ((select  max(round(gil)) ");
                    sql2.Append(" from  " + db_charge + ".calc_gku_" + prm_.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp ))");

#else
                    sql2.Append(" update t_nachdom set (pl_kvar, count_gil) = ((select max(squ), max(round(gil)) ");
                    sql2.Append(" from  " + db_charge + ":calc_gku_" + prm_.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar and d.tarif>0 ");
                    sql2.Append(" and t_nachdom.nzp_serv=d.nzp_serv and t_nachdom.nzp_supp=d.nzp_supp ))");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //04.06.2014 по решению Татьяны
                    //                    sql2.Remove(0, sql2.Length);
                    //#if PG
                    //                    sql2.Append(" update t_nachdom set pl_kvar = (select max(pl_kvar) ");
                    //                    sql2.Append(" from  selected_kvars d");
                    //                    sql2.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar ");
                    //                    sql2.Append(" and isol=2 )");
                    //                    sql2.Append(" where (nzp_serv in (select nzp_serv from " + pref + "_kernel.s_counts where nzp_serv<>8) or ");
                    //                    sql2.Append(" nzp_serv in (select nzp_serv from " + pref + "_kernel.serv_odn where nzp_serv<>512)) and ");
                    //                    sql2.Append(" nzp_kvar in (select nzp_kvar from selected_kvars where isol=2) ");
                    //#else
                    //                    sql2.Append(" update t_nachdom set pl_kvar = (select max(pl_kvar) ");
                    //                    sql2.Append(" from  selected_kvars d");
                    //                    sql2.Append(" where d.nzp_kvar=t_nachdom.nzp_kvar ");
                    //                    sql2.Append(" and isol=2 )");
                    //                    sql2.Append(" where (nzp_serv in (select nzp_serv from " + pref + "_kernel:s_counts where nzp_serv<>8) or ");
                    //                    sql2.Append(" nzp_serv in (select nzp_serv from " + pref + "_kernel:serv_odn where nzp_serv<>512)) and ");
                    //                    sql2.Append(" nzp_kvar in (select nzp_kvar from selected_kvars where isol=2) ");
                    //#endif
                    //                    ExecSQL(conn_db, sql2.ToString(), true);





                    #region Выборка перерасчетов прошлого периода

                    //ExecSQL(conn_db, "drop table t_nedop", true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, min(extract (year from(dat_s))*12+extract (month from(dat_s))) as month_s,  max(extract (year from(dat_po))*12+extract( month from(dat_po))) as month_po");
                    sql2.Append(" into temp t_nedop  from " + pref + "_data.nedop_kvar a, selected_kvars b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm_.month_.ToString("00") + "." +
                        prm_.year_.ToString("0000") + "'  ");
                    sql2.Append(" group by 1 ");
#else
                    sql2.Append(" select a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql2.Append(" from " + pref + "_data:nedop_kvar a, selected_kvars b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm_.month_.ToString("00") + "." +
                        prm_.year_.ToString("0000") + "'  ");
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
                    sql2.Append(" from " + db_charge + ".lnk_charge_" + prm_.month_.ToString("00") + " b, t_nedop d ");
                    sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                    sql2.Append(" group by 1,2");
#else
                    sql2.Append(" select month_, year_ ");
                    sql2.Append(" from " + db_charge + ":lnk_charge_" + prm_.month_.ToString("00") + " b, t_nedop d ");
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
                        sql2.Append(" insert into t_nachdom (nzp_kvar, nzp_supp, nzp_serv, sum_nedop, reval_k, reval_d) ");
                        sql2.Append(" select d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, selected_kvars d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm_.month_.ToString("00") + "." + prm_.year_.ToString() + "')");
                        sql2.Append(where_serv + where_supp);
                        sql2.Append(" group by 1,2,3");
#else
                        sql2.Append(" insert into t_nachdom (nzp_kvar, nzp_supp, nzp_serv, sum_nedop, reval_k, reval_d) ");
                        sql2.Append(" select d.nzp_kvar, nzp_supp, nzp_serv, ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, selected_kvars d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm_.month_.ToString("00") + "." + prm_.year_.ToString() + "')");
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


                    #endregion


                    //Объединение услуг
                    if (prm_.service == "union")
                    {
                        sql2.Remove(0, sql2.Length);
#if PG
                        sql2.Append(" update t_nachdom set nzp_serv = (select max(nzp_serv_base) from " + pref + "_kernel.service_union ");
                        sql2.Append(" where nzp_serv_uni = nzp_serv)");
                        sql2.Append(" where nzp_serv in (select nzp_serv_uni from  " + pref + "_kernel.service_union )");
#else
                        sql2.Append(" update t_nachdom set nzp_serv = (select max(nzp_serv_base) from " + pref + "_kernel:service_union ");
                        sql2.Append(" where nzp_serv_uni = nzp_serv)");
                        sql2.Append(" where nzp_serv in (select nzp_serv_uni from  " + pref + "_kernel:service_union )");
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

                    ExecSQL(conn_db, "drop table t12", false);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" UPDATE t_nachdom SET name_supp = " + DBManager.sNvlWord +
                            "((SELECT name_supp FROM " + pref + DBManager.sKernelAliasRest + "supplier s WHERE s.nzp_supp = t_nachdom.nzp_supp),'Неопределенный поставщик')");
                    ExecSQL(conn_db, sql.ToString(), true);


                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, nzp_supp  ");
                    sql2.Append(" into temp t12 from t_nachdom ");
                    sql2.Append(" where nzp_serv = 9 group by 1,2    ");
#else
                    sql2.Append(" select nzp_kvar, nzp_supp, TRIM(name_supp) AS name_supp  ");
                    sql2.Append(" from t_nachdom t ");
                    sql2.Append(" where nzp_serv = 9 group by 1,2 into temp t12 with no log  ");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t_nachdom set nzp_serv=9 ");
                    sql2.Append(" where nzp_serv = 14 and 0<(select count(*) ");
                    sql2.Append(" from t12 where t12.nxp_supp = t_nachdom.nzp_supp and  t12.nzp_kvar=t_nachdom.nzp_kvar) ");    // t12.nzp_supp=t_nachdom.nzp_supp and
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t_nachdom set nzp_serv=513 ");
                    sql2.Append(" where nzp_serv = 514 and 0<(select count(*) ");
                    sql2.Append(" from t12 where t12.nzp_supp = t_nachdom.nzp_supp and  t12.nzp_kvar=t_nachdom.nzp_kvar) ");  // t12.nzp_supp=t_nachdom.nzp_supp and
                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table t12", true);



                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select nzp_kvar, nzp_supp ");
                    sql2.Append(" into temp t12  from t_nachdom a ");
                    sql2.Append(" where nzp_serv = 515 and 0=(select count(*) from t_nachdom b");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_supp=b.nzp_supp and b.nzp_serv=25 and b.rsum_tarif>0)");
                    sql2.Append(" and rsum_tarif>0 ");
#else
                    sql2.Append(" select nzp_kvar, nzp_supp ");
                    sql2.Append(" from t_nachdom a ");
                    sql2.Append(" where nzp_serv = 515 and 0=(select count(*) from t_nachdom b");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_supp=b.nzp_supp and b.nzp_serv=25 and b.rsum_tarif>0)");
                    sql2.Append(" and rsum_tarif>0 into temp t12 with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_nachdom(nzp_kvar, nzp_supp,  nzp_serv, pl_kvar, rsum_tarif,  ");
                    sql2.Append(" sum_nedop, reval_k, reval_d, real_charge_k, real_charge_d, ");
                    sql2.Append(" sum_charge, sum_money, count_gil, has_serv)");
                    sql2.Append(" select nzp_kvar, nzp_supp, 25,0,0,0,0,0,0,0,0,0,0,1 ");
                    sql2.Append(" from t12 a ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    ExecSQL(conn_db, "drop table t12", true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select  nzp_kvar, nzp_supp, nzp_serv, max(pl_kvar) as pl_kvar, sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, SUM(sum_otopl) AS sum_otopl, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(count_gil) as count_gil, max(has_serv) as has_serv ");
                    sql2.Append("  into temp t_nachdom1 from  t_nachdom ");
                    sql2.Append(" group by 1,2,3 ");

#else
                    sql2.Append(" select  nzp_kvar, nzp_supp, nzp_serv, max(pl_kvar) as pl_kvar, sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k,SUM(sum_otopl) AS sum_otopl, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(count_gil) as count_gil, max(has_serv) as has_serv ");
                    sql2.Append(" from  t_nachdom ");
                    sql2.Append(" group by 1,2,3 ");
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

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select  nzp_kvar, nzp_serv, max(pl_kvar) as pl_kvar, sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, SUM(sum_otopl) AS sum_otopl, sum(real_charge_d) as real_charge_d,max(has_serv) as has_serv, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(count_gil) as count_gil ");
                    sql2.Append(" into temp t_nachdom2 from  t_nachdom ");
                    sql2.Append(" group by 1,2 ");

#else
                    sql2.Append(" select  nzp_kvar, nzp_serv, max(pl_kvar) as pl_kvar, sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(reval_k) as reval_k, sum(reval_d) as reval_d, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k,SUM(sum_otopl) AS sum_otopl, sum(real_charge_d) as real_charge_d,max(has_serv) as has_serv, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(count_gil) as count_gil ");
                    sql2.Append(" from  t_nachdom ");
                    sql2.Append(" group by 1,2 ");
                    sql2.Append(" into temp t_nachdom2 with no log");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    #region Удаление временных таблиц


                    ExecSQL(conn_db, "drop table t_nachdom ", true);

                    #endregion

                    #endregion

                    #region Вывод списка в таблицу

                    string kapr = (prm_.nzp_serv == -206 ? " and s.nzp_serv <> 206 " :
                        prm_.nzp_serv == 206 ? " and s.nzp_serv=206 " : "");

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 2 as tip, geu, 1, name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=2 then 0 else coalesce(a.pl_kvar,0) end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom1 a,  " + pref + "_kernel.services s, ");
                    sql2.Append(pref + "_kernel.supplier su, " + Points.Pref + "_data.s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv ");
                    sql2.Append(" and a.nzp_supp=su.nzp_supp and b.nzp_geu=sg.nzp_geu " + kapr);
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#else
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 2 as tip, geu, 1, name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=2 then 0 else nvl(a.pl_kvar,0) end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then nvl(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(-1*real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom1 a,  " + pref + "_kernel:services s, ");
                    sql2.Append(pref + "_kernel:supplier su, " + Points.Pref + "_data:s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv ");
                    sql2.Append(" and a.nzp_supp=su.nzp_supp and b.nzp_geu=sg.nzp_geu " + kapr);
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 2 as tip, geu,2, 'Итого ' as name_supp, service, s.ordering,  ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then coalesce(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom2 a,  " + pref + "_kernel.services s, ");
                    sql2.Append(Points.Pref + "_data.s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv " + kapr);
                    sql2.Append(" and b.nzp_geu=sg.nzp_geu ");
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#else
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 2 as tip, geu,2, 'Итого ' as name_supp, service, s.ordering,  ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then nvl(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then nvl(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(-1*real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom2 a,  " + pref + "_kernel:services s, ");
                    sql2.Append(Points.Pref + "_data:s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv " + kapr);
                    sql2.Append(" and b.nzp_geu=sg.nzp_geu ");
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 1 as tip, '-1', 1, name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then coalesce(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom1 a,  " + pref + "_kernel.services s, ");
                    sql2.Append(pref + "_kernel.supplier su, " + Points.Pref + "_data.s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv ");
                    sql2.Append(" and a.nzp_supp=su.nzp_supp and b.nzp_geu=sg.nzp_geu " + kapr);
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#else
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 1 as tip, '-1', 1, name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then nvl(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then nvl(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(-1*real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom1 a,  " + pref + "_kernel:services s, ");
                    sql2.Append(pref + "_kernel:supplier su, " + Points.Pref + "_data:s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv ");
                    sql2.Append(" and a.nzp_supp=su.nzp_supp and b.nzp_geu=sg.nzp_geu " + kapr);
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 1 as tip, '-1',2, 'Итого ' as name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then coalesce(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then coalesce(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl,  ");
                    sql2.Append(" sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom2 a,  " + pref + "_kernel.services s, ");
                    sql2.Append(Points.Pref + "_data.s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv " + kapr);
                    sql2.Append(" and b.nzp_geu=sg.nzp_geu ");
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#else
                    sql2.Append(" insert into t_svod(tip , geu , sort , name_supp , service , ");
                    sql2.Append(" ordering, ord_serv , isol_pl , komm_pl , count_gil , ");
                    sql2.Append(" rsum_tarif , sum_nedop , reval_k , reval_d , sum_otopl, ");
                    sql2.Append(" real_charge_k , real_charge_d , sum_charge , sum_money, has_serv)");
                    sql2.Append(" select 1 as tip, '-1',2, 'Итого ' as name_supp, service, s.ordering, ");
                    sql2.Append(" (case when s.nzp_serv in (515,25) then  s.nzp_serv else 1000 end) as ord_serv , ");
                    sql2.Append(" sum(case when isol=1 then nvl(a.pl_kvar,0) else 0 end) as isol_pl, ");
                    sql2.Append(" sum(case when isol=2 then nvl(a.pl_kvar,0) else 0 end) as komm_pl, ");
                    sql2.Append(" sum(count_gil) as count_gil, ");
                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, ");
                    sql2.Append(" sum(-1*reval_k) as reval_k, sum(reval_d) as reval_d, SUM(sum_otopl) AS sum_otopl, ");
                    sql2.Append(" sum(-1*real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, max(has_serv) ");
                    sql2.Append(" from  selected_kvars b, t_nachdom2 a,  " + pref + "_kernel:services s, ");
                    sql2.Append(Points.Pref + "_data:s_geu sg ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=s.nzp_serv " + kapr);

                    sql2.Append(" and b.nzp_geu=sg.nzp_geu ");
                    sql2.Append(" group by 1,2,3,4,5,6,7");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update t_svod set reval_d = reval_d + (SELECT sum(sum_rcl) as sum_rcl from bill01_charge_" + (prm_.year_ - 2000).ToString("00")
                        + " a inner join + " + pref + "_kernel.supplier su on su.nzp_supp = a.nzp_supp " +
                        " inner join " + pref + "_kernel.services s on s.nzp_serv = a.nzp_serv where su.name_supp = t_svod.name_supp and s.service = t_svod.service and a.sum_rcl > 0 and month_ = " +
                         prm_.month_ + ")");


                    #region Удаление временных таблиц
                    ExecSQL(conn_db, "drop table selected_kvars", true);
                    ExecSQL(conn_db, "drop table t_nachdom1 ", true);
                    ExecSQL(conn_db, "drop table t_nachdom2 ", true);
                    #endregion


                    #endregion

                }

            }
            //sw.WriteLine("28");

            //Переименовано горячая вода в горячее водоснабжение и т.д. 
            if (!ExecSQL(conn_db, "update t_svod set service = replace(service,'чая вода','чее водоснабжение')", true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            if (!ExecSQL(conn_db, "update t_svod set service = replace(service,'ная вода','ное водоснабжение')", true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #region Выгрузка на экран
            sql2.Remove(0, sql2.Length);
#if PG
            //sql2.Append(" select tip , geu , sort , name_supp , service , ordering, ");
            //sql2.Append(" ord_serv , sum(coalesce(isol_pl,0)) as isol_pl , sum(coalesce(komm_pl,0)) as komm_pl ,");
            //sql2.Append(" sum(coalesce(count_gil,0)) as count_gil , ");
            //sql2.Append(" sum(rsum_tarif) as rsum_tarif , ");
            //sql2.Append(" sum(sum_nedop) as sum_nedop , ");
            //sql2.Append(" abs(-sum(coalesce(reval_k,0)+coalesce(real_charge_k,0))) as reval_k , ");
            //sql2.Append(" abs(sum(coalesce(reval_d,0)+coalesce(real_charge_d,0))) as reval_d , ");
            //sql2.Append(" sum(sum_otopl) as sum_otopl , ");
            //sql2.Append(" sum(real_charge_k) as real_charge_k ,");
            //sql2.Append(" sum(real_charge_d) as  real_charge_d, ");
            //sql2.Append(" sum(rsum_tarif) - abs(-sum(coalesce(reval_k,0)+coalesce(real_charge_k,0))) + abs(sum(coalesce(reval_d,0)+coalesce(real_charge_d,0))) as sum_charge , sum(sum_money) as sum_money");
            //sql2.Append(" from t_svod ");
            //sql2.Append(" where abs(coalesce(rsum_tarif,0))+abs(coalesce(sum_nedop,0))+abs(coalesce(reval_k,0))+abs(coalesce(real_charge_k,0))+");
            //sql2.Append(" abs(coalesce(reval_d,0))+abs(coalesce(real_charge_d,0))+abs(coalesce(sum_charge,0))+abs(coalesce(sum_money,0))+abs(coalesce(has_serv,0))>0.001");
            //sql2.Append("  group by 1,2,3,4,5,6,7 ");
            //sql2.Append(" order by 1,2,3,4,6,7,5 ");t_nedop  
            sql2.Append(" select tip , geu , sort , name_supp , service , ordering, ");
            sql2.Append(" ord_serv , sum(coalesce(isol_pl,0)) as isol_pl , sum(coalesce(komm_pl,0)) as komm_pl ,");
            sql2.Append(" sum(coalesce(count_gil,0)) as count_gil , ");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif , ");
            sql2.Append(" sum(sum_nedop) as sum_nedop , ");
            sql2.Append(" abs(-sum(coalesce(reval_k,0))) as reval_k , ");
            sql2.Append(" abs(sum(coalesce(reval_d,0))) as reval_d , ");
            sql2.Append(" sum(sum_otopl) as sum_otopl , ");
            sql2.Append(" sum(real_charge_k) as real_charge_k ,");
            sql2.Append(" sum(real_charge_d) as  real_charge_d, ");
            sql2.Append(" sum(rsum_tarif) - abs(sum(coalesce(sum_nedop,0))) - abs(-sum(coalesce(reval_k,0))) + abs(sum(coalesce(reval_d,0))) as sum_charge , sum(sum_money) as sum_money");
            sql2.Append(" from t_svod ");
            sql2.Append(" where abs(coalesce(rsum_tarif,0))+abs(coalesce(sum_nedop,0))+abs(coalesce(reval_k,0))+abs(coalesce(real_charge_k,0))+");
            sql2.Append(" abs(coalesce(reval_d,0))+abs(coalesce(real_charge_d,0))+abs(coalesce(sum_charge,0))+abs(coalesce(sum_money,0))+abs(coalesce(has_serv,0))>0.001");
            sql2.Append("  group by 1,2,3,4,5,6,7 ");
            sql2.Append(" order by 1,2,3,4,6,7,5 ");
#else
            sql2.Append(" select tip , geu , sort , name_supp , service , ordering, ");
            sql2.Append(" ord_serv , sum(nvl(isol_pl,0)) as isol_pl , sum(nvl(komm_pl,0)) as komm_pl ,");
            sql2.Append(" sum(nvl(count_gil,0)) as count_gil , ");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif , ");
            sql2.Append(" sum(sum_nedop) as sum_nedop , ");
            sql2.Append(" sum(nvl(reval_k,0)+nvl(real_charge_k,0)) as reval_k , ");
            sql2.Append(" sum(nvl(reval_d,0)+nvl(real_charge_d,0)) as reval_d , ");
            sql2.Append(" sum(sum_otopl) as sum_otopl , ");
            sql2.Append(" sum(real_charge_k) as real_charge_k ,");
            sql2.Append(" sum(real_charge_d) as  real_charge_d, ");
            sql2.Append(" sum(sum_charge) as sum_charge , sum(sum_money) as sum_money");
            sql2.Append(" from t_svod ");
            sql2.Append(" where abs(nvl(rsum_tarif,0))+abs(nvl(sum_nedop,0))+abs(nvl(reval_k,0))+abs(nvl(real_charge_k,0))+");
            sql2.Append(" abs(nvl(reval_d,0))+abs(nvl(real_charge_d,0))+abs(nvl(sum_charge,0))+abs(nvl(sum_money,0))+abs(nvl(has_serv,0))>0.001");
            sql2.Append(" group by 1,2,3,4,5,6,7 ");
            sql2.Append(" order by 1,2,3,4,6,7,5 ");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
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
                        //    DataTable Data_Table = new DataTable();

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
                        conn_web.Close();
                        return null;
                    }
                    if (reader2 != null) reader2.Close();





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

                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            #endregion
            reader.Close();

            ExecSQL(conn_db, "drop table sel_kvar5", false);
            ExecSQL(conn_db, "drop table t_svod", false);

            conn_db.Close();
            return LocalTable;
            #endregion

        }

    }
}
