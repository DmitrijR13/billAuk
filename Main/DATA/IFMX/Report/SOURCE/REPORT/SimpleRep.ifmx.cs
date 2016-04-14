using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    
    public class SimpleRep : DataBaseHead
    {


        /// <summary>
        /// Отчет счетчикам для Самары
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetCountersSprav(ReportPrm prm)
        {
            

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_counterss ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_counterss (     ");
            sql.Append(" month_ integer default 0,");
            sql.Append(" year_ integer default 0,");
            sql.Append(" nzp_counter integer default 0,");
            sql.Append(" cnt_stage integer default 0,");
            sql.Append(" nzp_cnttype integer default 0,");
            sql.Append(" num_cnt char(20),");
            sql.Append(" mmnog integer default 0,");
            sql.Append(" pu_type char(40),");
            sql.Append(" dat_open char(10),");
            sql.Append(" dat_close char(10),");
            sql.Append(" dat_begin Date,");
            sql.Append(" dat_uchet Date,");
            sql.Append(" dat_uchet_pred Date,");
            sql.Append(" val_begin " + sDecimalType + "(14,2) default 0.00, "); //Первое показание счетчика            
            sql.Append(" val_cnt " + sDecimalType + "(14,2) default 0.00, "); //Показания счетчика
            sql.Append(" val_cnt_pred " + sDecimalType + "(14,2) default 0.00, "); //Предыдущие показания счетчика
            sql.Append(" rashod " + sDecimalType + "(14,2) default 0.00, "); //Расход по ПУ
            sql.Append(" rashod_sr " + sDecimalType + "(14,2) default 0.00, "); //Расход по среднему
            sql.Append(" rashod_nr " + sDecimalType + "(14,2) default 0.00, "); //Расход по нормативу
            sql.Append(" Vnach " + sDecimalType + "(14,2) default 0.00, "); //Объем начислено
            sql.Append(" Vodn " + sDecimalType + "(14,2) default 0.00, "); //Объем ОДН
            sql.Append(" VItogo " + sDecimalType + "(14,2) default 0.00 "); //Итого к начислению
            sql.Append(" ) " + sUnlogTempTable);
            if (ExecSQL(conn_db, sql.ToString(), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "drop table t1_counters", false);

            //Создание временной таблицы для поиска первых значений
            if (ExecSQL(conn_db, sql.ToString().Replace("t_counterss","t1_counters"), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            #endregion

            string pref = prm.pref;
            try
            {
                #region Выборка по локальным банкам

                //Добавляем показания приборов учета
                sql.Remove(0, sql.Length);
                sql.Append("INSERT into t_counterss(month_, year_,nzp_cnttype,nzp_counter, " +
                           "       num_cnt, dat_uchet, val_cnt) " +
                           " SELECT month(dat_uchet - 1 units day ), year(dat_uchet - 1 units day), " +
                           "        nzp_cnttype,nzp_counter, num_cnt, dat_uchet, val_cnt " +
                           " FROM " + pref + sDataAliasRest + "counters  " +
                           " WHERE nzp_kvar=" + prm.nzp_kvar +
                           "       AND nzp_serv= " + prm.reportServList.First().Key +
                           "       AND is_actual=1 ");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, " INSERT INTO t1_counters SELECT * FROM t_counterss ", true);



                sql.Remove(0, sql.Length);
                sql.Append(" DELETE FROM t_counterss " +
                           " WHERE DAT_UCHET<'" + prm.reportDatBegin + "'");
                ExecSQL(conn_db, sql.ToString(), true);

                //Проставляем масштабный множитель
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_counterss set (mmnog, cnt_stage, pu_type)=((" +
                           " SELECT mmnog, cnt_stage, name_type " +
                           " FROM " + pref + sKernelAliasRest + "s_counttypes a " +
                           " WHERE t_counterss.nzp_cnttype=a.nzp_cnttype))");
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_begin=( " +
                           "         SELECT MIN(dat_uchet) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter)");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET val_begin=( " +
                           "         SELECT val_cnt " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter " +
                           "               AND t_counterss.dat_begin = t1_counters.dat_uchet )");
                ExecSQL(conn_db, sql.ToString(), true);


                //Проставляем дату предыдущего показания
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_uchet_pred=( " +
                           "         SELECT MAX(dat_uchet) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter" +
                           "               AND t_counterss.dat_uchet > t1_counters.dat_uchet)");
                ExecSQL(conn_db, sql.ToString(), true);

                //Проставляем предыдущее показание
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET val_cnt_pred=( " +
                           "         SELECT MAX(val_cnt) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter" +
                           "               AND t_counterss.dat_uchet_pred = t1_counters.dat_uchet)");
                ExecSQL(conn_db, sql.ToString(), true);

                //Расчитываем расход по счетчикам
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET rashod=(CASE WHEN val_cnt>=nvl(val_cnt_pred,0) THEN val_cnt-nvl(val_cnt_pred,0) " +
                           " ELSE val_cnt-nvl(val_cnt_pred,0) + POW(10,cnt_stage) END) ");
                ExecSQL(conn_db, sql.ToString(), true);

                //Проставляем дату закрытия счетчика
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss set dat_close=( " +
                           "        SELECT a.dat_close " +
                           "        FROM " + pref + sDataAliasRest + "counters_spis a" +
                           "        WHERE t_counterss.nzp_counter = a.nzp_counter) ");
                ExecSQL(conn_db, sql.ToString(), true);

                //Проставляем дату открытия счетчика
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_open = ( " +
                           "        SELECT val_prm " +
                           "        FROM " + pref + sDataAliasRest + "prm_17 " +
                           "        WHERE t_counterss.nzp_counter=nzp " +
                           "               AND nzp_prm=2025 " +
                           "               AND is_actual=1 " +
                           "               AND dat_s<= " + sCurDate +
                           "               AND dat_po>=" + sCurDate + " )");
                ExecSQL(conn_db, sql.ToString(), true);



                for (int i = DateTime.Parse(prm.reportDatBegin).Year * 12 + DateTime.Parse(prm.reportDatBegin).Month; i <
                   DateTime.Parse(prm.reportDatEnd).Year * 12 + DateTime.Parse(prm.reportDatEnd).Month + 1; i++)
                {
                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 1;
                    }

                    if (year_ >= 2013)
                    {

                        //Добавляем месяца по которым не было показаний счетчиков
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT into t_counterss(month_, year_) " +
                                   " VALUES(" + month_ + "," + year_ + ")");
                        ExecSQL(conn_db, sql.ToString(), true);

                        string sChargeTable = pref + "_charge_" + (year_ - 2000).ToString("00") +
                                tableDelimiter + "calc_gku_" + month_.ToString("00");

                        sql.Remove(0, sql.Length);
                        //Если до начала действия нашей системы, то по данным сервера
                        if (month_ + year_ * 12 < 10 + 2013 * 12)
                        {
                            sql.Append(" UPDATE  t_counterss SET (rashod_sr, rashod_nr, Vnach, Vodn, Vitogo )=(( " +
                                       "         SELECT (CASE WHEN type_rashod =3  THEN rashod ELSE 0 END), " +
                                       "                (CASE WHEN type_rashod IN (0,1) THEN rashod ELSE 0 END), " +
                                       "                (rashod), (rashod_odn), (rashod + rashod_odn) " +
                                       "         FROM " + Points.Pref + sDataAliasRest + "a_serverlsserv" + month_ +
                                       "         WHERE nzp_serv=" + prm.reportServList.First().Key +
                                       " AND num_ls=" + prm.nzp_kvar + "  )) " +
                                       " WHERE month_=" + month_ + " AND year_=" + year_);
                        }
                        else
                        {

                            //С начала действия нашей системы по нашим данным
                            if (prm.reportServList.First().Key == "9")
                            {
                                sql.Append(" UPDATE  t_counterss SET (rashod_sr, rashod_nr, Vnach,Vodn,Vitogo )=((" +
                                           "         SELECT (CASE WHEN is_device = 9 THEN Round(valm/rsh2,2) ELSE 0 END), " +
                                           "                (CASE WHEN is_device = 0 THEN Round(valm/rsh2,2) ELSE 0 END), " +
                                           "                Round((valm + dlt_reval)/rsh2,2), " +
                                           "                (CASE WHEN dop87<0 AND dop87<-valm-dlt_reval " +
                                           "                      AND valm+dlt_reval>=0 then Round((-valm-dlt_reval)/rsh2,2) " +
                                           "                      WHEN valm+dlt_reval<0 THEN 0 ELSE Round(dop87/rsh2,2) END), " +
                                           "                Round(rashod/rsh2,2) " +
                                           "         FROM  " + sChargeTable +
                                           "         WHERE nzp_kvar=" + prm.nzp_kvar + " AND nzp_serv=9 AND rsh2>0)) " +
                                           " WHERE month_=" + month_ + " AND year_=" + year_);
                            }
                            else
                            {
                                sql.Append(" UPDATE  t_counterss SET (rashod_sr, rashod_nr, Vnach,Vodn,Vitogo )=((" +
                                           "         SELECT (CASE WHEN is_device = 9 THEN valm ELSE 0 END), " +
                                           "                (CASE WHEN is_device = 0 THEN valm ELSE 0 END), " +
                                           "                (valm + dlt_reval), " +
                                           "                (CASE WHEN dop87<0 AND dop87<-valm-dlt_reval AND valm+dlt_reval>=0 then -valm-dlt_reval " +
                                           "                      WHEN valm+dlt_reval<0 THEN 0 ELSE dop87 END), (rashod) " +
                                           "         FROM  " + sChargeTable +
                                           "         WHERE nzp_kvar=" + prm.nzp_kvar + " AND nzp_serv=" +
                                           prm.reportServList.First().Key + ")) " +
                                           " WHERE month_=" + month_ + " AND year_=" + year_);
                            }
                        }
                        ExecSQL(conn_db, sql.ToString(), false);

                    }

                }
                //Убираем дублирующие записи
                sql.Remove(0, sql.Length);
                sql.Append(" DELETE  t_counterss  " +
                           " WHERE nzp_counter = 0 AND 0<(" +
                           "        SELECT count(*) " +
                           "        FROM t1_counters " +
                           "        WHERE t_counterss.year_=t1_counters.year_ " +
                           "                AND t_counterss.month_=t1_counters.month_) ");
                ExecSQL(conn_db, sql.ToString(), true);


                ExecSQL(conn_db, "DROP TABLE t1_counters", true);
                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT year_, month_, num_cnt, ");
                sql.Append("        " + sNvlWord + "(dat_open" + sConvToChar + ",'') as dat_open,");
                sql.Append("        " + sNvlWord + "(dat_close" + sConvToChar + ",'') as dat_close, ");
                sql.Append("        " + sNvlWord + "(val_begin,0) as val_begin, ");
                sql.Append("        " + sNvlWord + "(pu_type" + sConvToChar + ",'') as pu_type,");
                sql.Append("        " + sNvlWord + "(mmnog,1) as mmnog, ");
                sql.Append("        " + sNvlWord + "(val_cnt,0) as val_cnt, ");
                sql.Append("        " + sNvlWord + "(val_cnt_pred,0) as val_cnt_pred,  ");
                sql.Append("        " + sNvlWord + "(rashod,0) as rashod, ");
                sql.Append("        " + sNvlWord + "(rashod_nr,0) + ");
                sql.Append("        " + sNvlWord + "(rashod_sr,0) as rashod_nrsr, ");
                sql.Append("        " + sNvlWord + "(Vnach,0) as Vnach, ");
                sql.Append("        " + sNvlWord + "(Vodn,0) as Vodn, ");
                sql.Append("        " + sNvlWord + "(VItogo,0) as VItogo ");
                sql.Append(" FROM t_counterss t ");
                sql.Append(" ORDER BY 1,2,3 ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                Utils.setCulture();

                if (reader != null)
                {
                    DT.Load(reader, LoadOption.OverwriteChanges);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_counterss ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }


        /// <summary>
        /// Отчет по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetListDolgTula(ReportPrm prm)
        {


            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";
            try
            {
                #region создание временной таблицы
                ExecSQL(conn_db, " drop table t_dolg ", false);
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_dolg (     ");
                sql.Append(" num_ls integer default 0,");
                sql.Append(" sum_dolg " + sDecimalType + "(14,2) default 0.00 "); //Сумма долга
                sql.Append(" ) " + sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);
                #endregion

                #region Выборка по локальным банкам

                #region Ограничения
                string where_wp = "";
                string where_supp = "";
                string where_serv = "";
                string where_adr = "";

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

                                if (role.kod == Constants.role_sql_area)
                                    where_adr += " and nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }


                //Множественный выбор УК или поставщиков услуг
                if (prm.reportGeuList.Count == 1)
                {
                    where_adr += " and nzp_geu=" + prm.reportGeuList.First().Key;
                }
                else if (prm.reportGeuList.Count > 0)
                {
                    where_adr += " and nzp_geu in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportGeuList)
                        where_adr += kp.Key + ",";
                    where_adr += "-1)";
                }

                if (prm.reportAreaList.Count == 1)
                {
                    where_adr += " and nzp_area=" + prm.reportAreaList.First().Key;
                }
                else if (prm.reportAreaList.Count > 0)
                {
                    where_adr += " and nzp_area in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportAreaList)
                        where_adr += kp.Key + ",";
                    where_adr += "-1)";
                }

                if (prm.reportSuppList.Count == 1)
                {
                    where_supp += " and nzp_supp=" + prm.reportSuppList.First().Key;
                }
                else if (prm.reportSuppList.Count > 0)
                {
                    where_supp += " and nzp_supp in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportSuppList)
                        where_supp += kp.Key + ",";
                    where_supp += "-1)";
                }

                if (prm.reportServList.Count == 1)
                {
                    where_serv += " and nzp_serv=" + prm.reportServList.First().Key;
                }
                else if (prm.reportServList.Count > 0)
                {
                    where_serv += " and nzp_serv in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportServList)
                        where_serv += kp.Key + ",";
                    where_serv += "-1)";
                }

                if (prm.nzp_dom > 0) where_adr += " and nzp_dom=" + prm.nzp_dom.ToString();







                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT * ");
                sql.Append(" FROM  " + Points.Pref + sKernelAliasRest + "s_point ");
                sql.Append(" WHERE nzp_wp>1 " + where_wp);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    while (reader.Read())
                    {
                        string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                        string chargeXX = pref + "_charge_" + (prm.year - 2000).ToString("00") +
                            DBManager.tableDelimiter + "charge_" + prm.month.ToString("00");

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_nach (num_ls, sum_dolg)");
                        sql.Append(" SELECT num_ls, sum(sum_insaldo)  ");
                        sql.Append(" FROM " + chargeXX + " a, " + pref + sDataAliasRest + "kvar k");
                        sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar ");
                        sql.Append("        AND dat_charge is null and sum_insaldo<0");
                        sql.Append("        AND a.nzp_serv>1 ");
                        sql.Append(where_adr + where_supp + where_serv);
                        sql.Append(" GROUP BY  1            ");
                        if (!ExecSQL(conn_db, sql.ToString(), true).result)
                            return null;

                    }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT town, rajon, ulica, idom, ndom, nkor, k.nzp_dom, ikvar, nkvar, nkvar_n, k.num_ls, ");
                sql.Append("        fio, sum_dolg ");
                sql.Append(" FROM t_dolg a, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "kvar k, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "dom d, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_ulica su, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_rajon sr, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_town st ");
                sql.Append(" WHERE a.num_ls=k.num_ls ");
                sql.Append("        AND k.nzp_dom=d.nzp_dom ");
                sql.Append("        AND d.nzp_ul=su.nzp_ul ");
                sql.Append("        AND su.nzp_raj=sr.nzp_raj ");
                sql.Append("        and sr.nzp_town=st.nzp_town ");
                sql.Append("        ORDER BY 1,2,3,4,5,6,7,8,9,10 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_dolg ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }


        /// <summary>
        /// Справка по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetSpravDolgTula(ReportPrm prm)
        {


            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";
            try
            {
                #region создание временной таблицы
                ExecSQL(conn_db, " drop table t_svod ", false);
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_svod (     "+
                           " month_ integer, "+
                           " year_ integer, "+
                           " nzp_serv integer, "+
                           " nzp_supp integer, "+
                           " sum_insaldo "+DBManager.sDecimalType+"(14,2), "+
                           " sum_tarif "+DBManager.sDecimalType+"(14,2), "+
                           " reval "+DBManager.sDecimalType+"(14,2), "+
                           " real_pere "+DBManager.sDecimalType+"(14,2), "+
                           " sum_money "+DBManager.sDecimalType+"(14,2), "+
                           " sum_outsaldo "+DBManager.sDecimalType+"(14,2)) ) " + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);
                #endregion

                #region Выборка по локальным банкам

                #region Ограничения
                string where_wp = "";
                string where_supp = "";
                string where_serv = "";
                string where_adr = "";

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

                                if (role.kod == Constants.role_sql_area)
                                    where_adr += " and nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }


                //Множественный выбор УК или поставщиков услуг
                if (prm.reportGeuList.Count == 1)
                {
                    where_adr += " and nzp_geu=" + prm.reportGeuList.First().Key;
                }
                else if (prm.reportGeuList.Count > 0)
                {
                    where_adr += " and nzp_geu in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportGeuList)
                        where_adr += kp.Key + ",";
                    where_adr += "-1)";
                }

                if (prm.reportAreaList.Count == 1)
                {
                    where_adr += " and nzp_area=" + prm.reportAreaList.First().Key;
                }
                else if (prm.reportAreaList.Count > 0)
                {
                    where_adr += " and nzp_area in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportAreaList)
                        where_adr += kp.Key + ",";
                    where_adr += "-1)";
                }

                if (prm.reportSuppList.Count == 1)
                {
                    where_supp += " and nzp_supp=" + prm.reportSuppList.First().Key;
                }
                else if (prm.reportSuppList.Count > 0)
                {
                    where_supp += " and nzp_supp in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportSuppList)
                        where_supp += kp.Key + ",";
                    where_supp += "-1)";
                }

                if (prm.reportServList.Count == 1)
                {
                    where_serv += " and nzp_serv=" + prm.reportServList.First().Key;
                }
                else if (prm.reportServList.Count > 0)
                {
                    where_serv += " and nzp_serv in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportServList)
                        where_serv += kp.Key + ",";
                    where_serv += "-1)";
                }

                if (prm.nzp_dom > 0) where_adr += " and nzp_dom=" + prm.nzp_dom.ToString();
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT * ");
                sql.Append(" FROM  " + Points.Pref + sKernelAliasRest + "s_point ");
                sql.Append(" WHERE nzp_wp>1 " + where_wp);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    while (reader.Read())
                    {
                        string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                        string chargeXX = pref + "_charge_" + (prm.year - 2000).ToString("00") +
                            DBManager.tableDelimiter + "charge_" + prm.month.ToString("00");

                        for (int i = DateTime.Parse(prm.reportDatBegin).Year * 12 + DateTime.Parse(prm.reportDatBegin).Month; i <
                 DateTime.Parse(prm.reportDatEnd).Year * 12 + DateTime.Parse(prm.reportDatEnd).Month + 1; i++)
                        {
                            int year_ = i / 12;
                            int month_ = i % 12;
                            if (month_ == 0)
                            {
                                year_--;
                                month_ = 1;
                            }

                            sql.Remove(0, sql.Length);
                            sql.Append(" insert into t_svod(month_, year_, nzp_serv, nzp_supp, sum_insaldo, " +
                                       " sum_tarif, reval, real_pere, sum_money, sum_outsaldo)  " +
                                       " select " + year_ + "," + month_ + ",nzp_serv, nzp_supp, sum(sum_insaldo) as sum_insaldo, " +
                                       "       sum(sum_tarif) as sum_tarif, " +
                                       "       sum(reval) as reval, " +
                                       "       sum(real_pere) as real_pere, " +
                                       "       sum(sum_money) as sum_money, " +
                                       "       sum(sum_outsaldo) as sum_outsaldo " +
                                       "       from " + chargeXX + ", " + pref + DBManager.sDataAliasRest + "kvar k" +
                                       " where nzp_serv>1 and dat_charge is null " +
                                       where_adr + where_supp + where_serv +
                                       " GROUP BY  1,2,3,4           ");
                            ExecSQL(conn_db, sql.ToString(), true);

                        }

                    }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT year_, month_, service, name_supp,  sum(sum_insaldo) as sum_insaldo, "+
                           "        sum(sum_tarif) as sum_tarif, "+
                           "        sum(reval) as reval, "+
                           "        sum(real_pere) as real_pere, "+
                           "        sum(sum_money) as sum_money, "+
                           "        sum(sum_outsaldo) as sum_outsaldo");
                sql.Append(" FROM t_svod a, ");
                sql.Append(Points.Pref + DBManager.sKernelAliasRest + "services s,");
                sql.Append(Points.Pref + DBManager.sKernelAliasRest + "supplier su");
                sql.Append(" WHERE a.nzp_supp=su.nzp_supp ");
                sql.Append("        AND a.nzp_serv=s.nzp_serv ");
                sql.Append(" GROUP BY 1,2,3,4 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_svod ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }

        /// <summary>
        /// Отчет справка по поставщикам Тула
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetSpravSuppTula(ReportPrm prm)
        {


            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";
            try
            {
                #region создание временной таблицы
                ExecSQL(conn_db, " drop table t_svod ", false);
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_svod (     " +
                           " nzp_serv integer, " +
                           " nzp_supp integer) " + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);
                #endregion

                #region Выборка по локальным банкам

      

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT * ");
                sql.Append(" FROM  " + Points.Pref + sKernelAliasRest + "s_point ");
                sql.Append(" WHERE nzp_wp>1 ");


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    while (reader.Read())
                    {
                        string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                        string tarifTable = pref + "_data"+DBManager.tableDelimiter+"tarif ";

              

                            sql.Remove(0, sql.Length);
                            sql.Append(" insert into t_svod(nzp_serv, nzp_supp) " +
                                       " select nzp_serv, nzp_supp " +
                                       "       from " + tarifTable +
                                       " where is_actual=1 " +
                                       " GROUP BY  1,2           ");
                            ExecSQL(conn_db, sql.ToString(), true);


                    }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append("SELECT payer, npayer, service, inn, kpp, rcount , " +
                            "       (case when b.nzp_prm=505 then val_prm end) as ur_adr, " +
                            "       (case when b.nzp_prm=1269 then val_prm end) as fact_adr  " +
                            " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_payer a " +
                            " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "prm_9 b " +
                            "       ON a.nzp_payer=b.nzp and b.nzp_prm in (505,1269) " +
                            "           AND b.is_actual=1 " +
                            "           AND b.dat_s<= " + DBManager.sCurDate +
                            "           AND b.dat_po>=" + DBManager.sCurDate +
                            " LEFT OUTER JOIN  " + Points.Pref + DBManager.sDataAliasRest + "fn_bank f " +
                            " ON a.nzp_payer=f.nzp_payer, " +
                            " t_svod lf,  " + Points.Pref + DBManager.sKernelAliasRest + "services s " +
                            " WHERE a.nzp_supp=lf.nzp_supp " +
                            " AND lf.nzp_serv=s.nzp_serv "+
                            " GROUP BY 1,2,3,4,5,6,7,8 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_svod ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }


        /// <summary>
        /// Отчет по поступлению платежей для Тулы2
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetServSuppMoney2(ReportPrm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_distrib ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_distrib (     ");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" nzp_serv integer default 0,");
            sql.Append(" nzp_supp integer default 0,");
            sql.Append(" sum_in " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Входящий остаток
            sql.Append(" sum_send " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Перечислено
            sql.Append(" sum_out " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Исходящий остаток
            sql.Append(" sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Рапределено
            sql.Append(" sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Удержано
            sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00 "); //К перечислению
            sql.Append(" ) " + DBManager.sUnlogTempTable);
            ExecSQL(conn_db, sql.ToString(), true);
            #endregion

            try
            {


                #region Ограничения
                string where_supp = String.Empty;
                string where_serv = String.Empty;
                string where_adr = String.Empty;

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

                                if (role.kod == Constants.role_sql_area)
                                    where_adr += " and nzp_area in (" + role.val + ") ";
                            }
                        }
                    }
                }


                //Множественный выбор УК или поставщиков услуг
                if (prm.reportAreaList.Count == 1)
                {
                    where_adr += " and nzp_area=" + prm.reportAreaList.First().Key;
                }
                else if (prm.reportAreaList.Count > 0)
                {
                    where_adr += " and nzp_area in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportAreaList)
                        where_adr += kp.Key + ",";
                    where_adr += "-1)";
                }

                if (prm.reportSuppList.Count == 1)
                {
                    where_supp += " and nzp_supp=" + prm.reportSuppList.First().Key;
                }
                else if (prm.reportSuppList.Count > 0)
                {
                    where_supp += " and nzp_supp in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportSuppList)
                        where_supp += kp.Key + ",";
                    where_supp += "-1)";
                }

                if (prm.reportServList.Count == 1)
                {
                    where_serv += " and nzp_serv=" + prm.reportServList.First().Key;
                }
                else if (prm.reportServList.Count > 0)
                {
                    where_serv += " and nzp_serv in (";
                    foreach (KeyValuePair<string, string> kp in prm.reportServList)
                        where_serv += kp.Key + ",";
                    where_serv += "-1)";
                }


                #endregion

                string pref = Points.Pref;
                for (int i = DateTime.Parse(prm.reportDatBegin).Year * 12 + DateTime.Parse(prm.reportDatBegin).Month; i <
                    DateTime.Parse(prm.reportDatEnd).Year * 12 + DateTime.Parse(prm.reportDatEnd).Month + 1; i++)
                {

                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 1;
                    }
                    string distribXX = pref + "_fin_" + (year_ - 2000).ToString("00") +
                            DBManager.tableDelimiter + "fn_distrib_" + month_.ToString("00");


                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_distrib (nzp_area,nzp_serv, nzp_supp, sum_rasp, ");
                    sql.Append(" sum_ud, sum_charge, sum_send, sum_in, sum_out )");
                    sql.Append(" SELECT a.nzp_area,a.nzp_serv, sp.nzp_supp, sum(a.sum_rasp), ");
                    sql.Append(" sum(a.sum_ud), sum(a.sum_charge),  ");
                    sql.Append(" sum(a.sum_send), ");
                    sql.Append(" sum(case when dat_oper='" + prm.reportDatBegin + "' then a.sum_in else 0 end), ");
                    sql.Append(" sum(case when dat_oper='" + prm.reportDatEnd + "' then a.sum_out else 0 end) ");
                    sql.Append(" FROM " + distribXX + " a,  " + pref + DBManager.sKernelAliasRest + "s_payer sp");
                    sql.Append(" WHERE  dat_oper>='" + prm.reportDatBegin + "' AND dat_oper<='" + prm.reportDatEnd + "'");
                    sql.Append(" and a.nzp_payer=sp.nzp_payer ");
                    sql.Append(where_adr + where_supp + where_serv);
                    sql.Append(" GROUP BY  1,2,3          ");
                    ExecSQL(conn_db, sql.ToString(), true);


                }



                sql.Remove(0, sql.Length);
                sql.Append(" SELECT sa.area, s.service, su.name_supp, ");
                sql.Append("        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, ");
                sql.Append("        sum(t.sum_ud) as sum_ud, sum(sum_send) as sum_send, ");
                sql.Append("        sum(t.sum_in) as sum_in, sum(sum_out) as sum_out ");
                sql.Append(" FROM t_distrib t, ");
                sql.Append(pref + DBManager.sKernelAliasRest + "services s, ");
                sql.Append(pref + DBManager.sKernelAliasRest + "supplier su, ");
                sql.Append(pref + DBManager.sDataAliasRest + "s_area sa ");
                sql.Append(" WHERE t.nzp_supp = su.nzp_supp ");
                sql.Append("        AND t.nzp_serv = s.nzp_serv ");
                sql.Append("        AND t.nzp_area = sa.nzp_area ");
                sql.Append(" GROUP BY 1,2,3 ORDER BY 1,2,3 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_distrib ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }

        public DataTable GetSpravVipis(ReportPrm prm)
        {
            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            try
            {
                sql.Remove(0, sql.Length);
                sql.Append(" CREATE TEMP TABLE t_pers_account (     ");
                sql.Append(" ob_s char(20),");
                sql.Append(" gil_s char(20),");
                sql.Append(" count_room char(20),");
                sql.Append(" is_priv integer,");
                sql.Append(" is_komm integer,");
                sql.Append(" has_naem integer,");
                sql.Append(" pasp_gil_count integer default 0,");
                sql.Append(" sobstv_gil_count integer default 0, ");
                sql.Append(" sobstv_reg_count integer default 0 ");
                sql.Append(" ) " + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);


                //Квартирные параметры
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_pers_account (ob_s, gil_s, count_room, is_priv, is_komm) " +
                           " SELECT CASE WHEN nzp_prm = 4 then val_prm end as ob_s, " +
                           "        CASE WHEN nzp_prm = 6 then val_prm end as gil_s, " +
                           "        CASE WHEN nzp_prm = 107 then val_prm end as count_room, " +
                           "        CASE WHEN nzp_prm = 8 then 1 end as is_priv, " +
                           "        CASE WHEN nzp_prm = 3 and val_prm = '2' then 1 end as is_komm " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "prm_1 " +
                           " WHERE nzp=" + prm.nzp_kvar +
                           "        AND is_actual=1 " +
                           "        AND dat_s<=" + DBManager.sCurDate +
                           "        AND dat_po>=" + DBManager.sCurDate +
                           "        AND nzp_prm in (3,4,6,8,107)");
                ExecSQL(conn_db, sql.ToString(), true);


                //КОличество зарегистрированных
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_pers_account (pasp_gil_count) " +
                           " SELECT count(unique nzp_gil) " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "kart " +
                           " WHERE nzp_kvar=" + prm.nzp_kvar +
                           "        AND isactual='1' " +
                           "        AND nzp_tkrt = 1");
                ExecSQL(conn_db, sql.ToString(), true);



                //Количество собственников
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_pers_account (sobstv_gil_count) " +
                           " SELECT count(*) " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "sobstw " +
                           " WHERE nzp_kvar=" + prm.nzp_kvar +
                           "        AND is_actual=1");
                ExecSQL(conn_db, sql.ToString(), true);


                //Количество зарегистрированных собственников
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_pers_account (sobstv_reg_count) " +
                           " SELECT count(*) " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "sobstw a," +
                                      prm.pref + DBManager.sDataAliasRest + "kart b" +
                           " WHERE a.nzp_kvar=" + prm.nzp_kvar +
                           "        AND a.is_actual=1 " +
                           "        AND b.isactual='1' " +
                           "        AND a.nzp_gil=b.nzp_gil " +
                           "        AND a.nzp_kvar=b.nzp_kvar ");
                ExecSQL(conn_db, sql.ToString(), true);



                //Проверяем есть ли наем
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_pers_account (has_naem) " +
                           " SELECT 1 " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "tarif " +
                           " WHERE nzp_kvar=" + prm.nzp_kvar +
                           "        AND is_actual=1 " +
                           "        AND  nzp_serv=15 " +
                           "        AND  dat_s<=" + DBManager.sCurDate +
                           "        AND  dat_po>=" + DBManager.sCurDate);
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" SELECT ulica, ndom, nkor, nkvar, " + 
                            DBManager.sNvlWord + "(nkvar_n,'') as nkvar_n,"+
                           " k.nzp_kvar, " + DBManager.sNvlWord + "(fio,'') as fio, " +
                           DBManager.sNvlWord +"(pkod,0) as pkod," +
                           " max("+DBManager.sNvlWord +"(t.ob_s,'')) as ob_s, " +
                           " max(" + DBManager.sNvlWord + "(t.gil_s,'')) as gil_s, " +
                           " max(" + DBManager.sNvlWord + "(t.count_room,'')) as count_room, " +
                           " max(" + DBManager.sNvlWord + "(t.is_priv,0)) as is_priv, " +
                           " max(" + DBManager.sNvlWord + "(t.is_komm,0)) as is_komm, " +
                           " max(" + DBManager.sNvlWord + "(t.has_naem,0)) as has_naem, " +
                           " max(" + DBManager.sNvlWord + "(t.pasp_gil_count,0)) as pasp_gil_count, " +
                           " max(" + DBManager.sNvlWord + "(t.sobstv_gil_count,0)) as sobstv_gil_count, " +
                           " max(" + DBManager.sNvlWord + "(t.sobstv_reg_count,0)) as sobstv_reg_count " +
                           " FROM " + prm.pref + DBManager.sDataAliasRest + "kvar k, " +
                           "  t_pers_account t, " +
                                      prm.pref + DBManager.sDataAliasRest + "dom d, " +
                                      prm.pref + DBManager.sDataAliasRest + "s_ulica su" +
                           " WHERE k.nzp_dom=d.nzp_dom " +
                           "      AND d.nzp_ul=su.nzp_ul " +
                           "      AND k.nzp_kvar=" + prm.nzp_kvar +
                           " group by 1,2,3,4,5,6,7,8 ");
                DT = DBManager.ExecSQLToTable(conn_db, sql.ToString());
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Выписка из лицевого счета для Тулы " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_pers_account ", true);
              
                conn_db.Close();

            }
           
            return DT;
        }


        public DataTable GetReportTable(ReportPrm prm)
        {
            DataTable dtResult =  null;
            switch (prm.reportName)
            {
                case "Выписка из лицевого счета": dtResult = GetSpravVipis(prm); break;
            }
            return dtResult;
        }

    }   

}

