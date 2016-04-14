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
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {


        /// <summary>
        /// Отчет по начислениям поставщиков для Тулы
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetServSuppNach(ReportPrm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion


            #region Загружаем наименование расчетного центра

            Prm finder = new Prm();
            finder.nzp_prm = 80;
            finder.prm_num = 10;
            finder.month_ = prm.month;
            finder.year_ = prm.year;
            finder.nzp_user = prm.nzp_user;
            finder.pref = Points.Pref;

            DbParameters dbPrm = new DbParameters();
            Prm erc = dbPrm.FindSimplePrmValue(conn_db, finder, out ret);
            if (ret.result)
            {
                prm.reportDopParams.Add("ЕРЦ", erc.val_prm);
            }
            else
            {
                prm.reportDopParams.Add("ЕРЦ", " ");
            }
            dbPrm.Close();
            #endregion


            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_nach ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create " + sCrtTempTable + " table t_nach (     ");
            sql.Append(" nzp_serv integer default 0,");
            sql.Append(" nzp_supp integer default 0,");
            sql.Append(" sum_nach " + sDecimalType + "(14,2) default 0.00 "); //Начислено за месяц
            sql.Append(" ) " + sUnlogTempTable);
            if (ExecSQL(conn_db, sql.ToString(), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            try
            {
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
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                while (reader.Read())
                {
                    string pref = reader["bd_kernel"].ToString().ToLower().Trim();
#if PG
                    string chargeXX = pref + "_charge_" + (prm.year - 2000).ToString("00") +
                        ".charge_" + prm.month.ToString("00");
#else
                    string chargeXX = pref + "_charge_" + (prm.year - 2000).ToString("00") +
                        ":charge_" + prm.month.ToString("00");
#endif

                    string vidNach = "";
                    if (prm.reportDopParams["Вид начислено"] == "Начислено к оплате")
                        vidNach = "a.sum_charge";
                    else if (prm.reportDopParams["Вид начислено"] == "Расчитано по тарифу")
                        vidNach = "a.rsum_tarif";
                    else vidNach = "a.sum_tarif + a.real_charge + a.reval";

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_nach (nzp_serv, nzp_supp, sum_nach )");
                    sql.Append(" SELECT a.nzp_serv, a.nzp_supp, sum(" + vidNach + ")  ");
                    sql.Append(" FROM " + chargeXX + " a, " + pref + sDataAliasRest + "kvar k");
                    sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar ");
                    sql.Append("        AND dat_charge is null ");
                    sql.Append("        AND a.nzp_serv>1 ");
                    sql.Append(where_adr + where_supp + where_serv);
                    sql.Append(" GROUP BY  1,2            ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                        return null;

                }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT s.service, su.name_supp, ");
                sql.Append("        sum(t.sum_nach) as sum_charge ");
                sql.Append(" FROM t_nach t, " + Points.Pref + sKernelAliasRest + "services s, ");
                sql.Append(Points.Pref + sKernelAliasRest + "supplier su ");
                sql.Append(" WHERE t.nzp_supp = su.nzp_supp ");
                sql.Append("        AND t.nzp_serv = s.nzp_serv ");
                sql.Append(" GROUP BY 1,2 ");
                sql.Append(" ORDER BY 1,2 ");
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
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_nach ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            return DT;
        }

        /// <summary>
        /// Отчет по поступлению платежей для Тулы
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetServSuppMoney(ReportPrm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;

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
            sql.Append(" create " + sCrtTempTable + " table t_distrib (     ");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" nzp_serv integer default 0,");
            sql.Append(" nzp_supp integer default 0,");
            sql.Append(" sum_rasp " + sDecimalType + "(14,2) default 0.00, "); //Рапределено
            sql.Append(" sum_ud " + sDecimalType + "(14,2) default 0.00, "); //Удержано
            sql.Append(" sum_charge " + sDecimalType + "(14,2) default 0.00 "); //К перечислению
            sql.Append(" ) " + sUnlogTempTable);
            if (ExecSQL(conn_db, sql.ToString(), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            try
            {
                #region Выборка по локальным банкам

                #region Ограничения
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

                for (int i = DateTime.Parse(prm.reportDatBegin).Year * 12 + DateTime.Parse(prm.reportDatBegin).Month; i <
                    DateTime.Parse(prm.reportDatEnd).Year * 12 + DateTime.Parse(prm.reportDatEnd).Month + 1; i++)
                {
                    string pref = Points.Pref;
                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 12;
                    }

                    string distribXX = pref + "_fin_" + (year_ - 2000).ToString("00") +
                            tableDelimiter + "fn_distrib_dom_" + month_.ToString("00");

                    //Проверка на существование базы

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_distrib (nzp_area,nzp_serv, nzp_supp, sum_rasp, sum_ud, sum_charge )");
                    sql.Append(" SELECT a.nzp_area,a.nzp_serv, sp.nzp_supp, sum(a.sum_rasp), sum(a.sum_ud), sum(a.sum_charge)  ");
                    sql.Append(" FROM " + distribXX + " a,  " + Points.Pref + sKernelAliasRest + "s_payer sp");
                    sql.Append(" WHERE  dat_oper>='" + prm.reportDatBegin + "' AND dat_oper<='" + prm.reportDatEnd + "'");
                    sql.Append(" and a.nzp_payer=sp.nzp_payer ");
                    sql.Append(where_adr + where_supp + where_serv);
                    sql.Append(" GROUP BY  1,2,3          ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                        return null;

                }
                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT sa.area, s.service, su.name_supp, ");
                sql.Append("        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, ");
                sql.Append("        sum(t.sum_ud) as sum_ud ");
                sql.Append(" FROM t_distrib t, ");
                sql.Append(Points.Pref + sKernelAliasRest + "services s, ");
                sql.Append(Points.Pref + sKernelAliasRest + "supplier su, ");
                sql.Append(Points.Pref + sDataAliasRest + "s_area sa ");
                sql.Append(" WHERE t.nzp_supp = su.nzp_supp ");
                sql.Append("        AND t.nzp_serv = s.nzp_serv ");
                sql.Append("        AND t.nzp_area = sa.nzp_area ");
                sql.Append(" GROUP BY 1,2,3 ORDER BY 1,2,3 ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                Utils.setCulture();

                if (reader != null)
                {
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();
                }
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


        /// <summary>
        /// Сводный отчет по принятым и перечисленным средствам для Тулы
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

        /// <summary>
        /// Отчет по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetListDolgTula(ReportPrm prm, out Returns ret)
        {


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
        public DataTable GetSpravDolgTula(ReportPrm prm, out Returns ret)
        {


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
            try
            {
                #region создание временной таблицы
                ExecSQL(conn_db, " drop table t_svod ", false);
                sql.Remove(0, sql.Length);
                sql.Append(" create temp table t_svod (     " +
                           " month_ integer, " +
                           " year_ integer, " +
                           " nzp_serv integer, " +
                           " nzp_supp integer, " +
                           " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                           " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                           " reval " + DBManager.sDecimalType + "(14,2), " +
                           " real_pere " + DBManager.sDecimalType + "(14,2), " +
                           " sum_money " + DBManager.sDecimalType + "(14,2), " +
                           " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) ) " + DBManager.sUnlogTempTable);
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
                sql.Append(" SELECT '01' as day, year_, month_, service, name_supp,  sum(sum_insaldo) as sum_insaldo, " +
                           "        sum(sum_tarif) as sum_tarif, " +
                           "        sum(reval) as reval, " +
                           "        sum(real_pere) as real_pere, " +
                           "        sum(sum_money) as sum_money, " +
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
        public DataTable GetSpravSuppTula(ReportPrm prm, out Returns ret)
        {


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
                        string tarifTable = pref + "_data" + DBManager.tableDelimiter + "tarif ";



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
                            " AND lf.nzp_serv=s.nzp_serv " +
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
    }
}

