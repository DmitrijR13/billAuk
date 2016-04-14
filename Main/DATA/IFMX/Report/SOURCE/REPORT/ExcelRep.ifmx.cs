using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Global;
using FastReport;

using STCLINE.KP50.Interfaces;
using Bars.KP50.DB.Faktura;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    //tXXX_prmall , tXXX_spls
    public partial class ExcelRep : ExcelRepClient
    {
        //процедура для получения данных из tXXX_prmall , tXXX_spls
        //по nzp_user


        public DataTable GetDataReportGenertor(out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();
            DataTable Data_Table = new DataTable();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД кеш ", MonitorLog.typelog.Error, true);
                return null;
            }



            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT a.area, g.geu ");

            //Если Самара, то выводим pkod10
            if (Points.IsSmr)
            {
                sql.Append(" ,spl.pkod10 ");
            }
            //для всех остальных num_ls
            else
            {
                sql.Append(" ,spl.num_ls ");
            }
            sql.Append(" ,u.ulica ");
            sql.Append(" ,CASE WHEN d.nkor <> '-' AND d.nkor <> '' AND d.nkor is not null THEN  trim(d.ndom)   || ' корп. ' || d.nkor ELSE d.ndom END ");
            sql.Append(" ,k.nkvar ");

            sql.Append("  ,prm.* ");
#if PG
            sql.Append(" FROM  " +  "public.t" + Nzp_user + "_spls spl, ");
            sql.Append(" " +   "public.t" + Nzp_user + "_prmall prm ");
            sql.Append(" ," + Points.Pref + "_data. s_area a ");
            sql.Append(" ," + Points.Pref + "_data. s_geu g ");
            sql.Append(" ," + Points.Pref + "_data. s_ulica u ");
            sql.Append(" ," + Points.Pref + "_data. dom d ");
            sql.Append(" ," + Points.Pref + "_data. kvar k ");
#else
            sql.Append(" FROM  " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Nzp_user + "_spls spl, ");
            sql.Append(" " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Nzp_user + "_prmall prm ");
            sql.Append(" ," + Points.Pref + "_data: s_area a ");
            sql.Append(" ," + Points.Pref + "_data: s_geu g ");
            sql.Append(" ," + Points.Pref + "_data: s_ulica u ");
            sql.Append(" ," + Points.Pref + "_data: dom d ");
            sql.Append(" ," + Points.Pref + "_data: kvar k ");
#endif

            sql.Append(" WHERE spl. nzp_kvar = prm. nzp_kvar ");
            sql.Append(" and spl.nzp_area = a.nzp_area ");
            sql.Append(" and g.nzp_geu = spl.nzp_geu ");
            sql.Append(" and spl.nzp_ul = u.nzp_ul ");
            sql.Append(" and spl.nzp_dom = d.nzp_dom ");
            sql.Append(" and spl.nzp_kvar = k.nzp_kvar ");



            sql.Append(" and spl. mark  = 1 ");
            try
            {

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }


                if (reader != null)
                {
                    try
                    {
                        //заполнение DataTable
                        Data_Table.Load(reader, LoadOption.OverwriteChanges);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        return null;
                    }
                    //исключение nzp_kvar
                    Data_Table.Columns.Remove("nzp_kvar");

                    return Data_Table;
                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    return null;
                }

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
                if (reader != null)
                {
                    reader.Close();
                }
                sql.Remove(0, sql.Length);
                conn_db.Close();
                conn_web.Close();
            }
        }


        //прроцедура получения данных из tXX_calcs_MM_ГГ
        public DataTable GetCalcs(out Returns ret, string Nzp_user, string mm, string yy)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

            ret = Utils.InitReturns();
            DataTable Data_Table = new DataTable();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            IDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            StringBuilder sql = new StringBuilder();
            sql.Append(" select  c.* ");
            sql.Append(" from  "+sDefaultSchema+"t" + Nzp_user + "_calcs_" + mm + "_" + yy + " c ");

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
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
                        Data_Table.Load(reader, LoadOption.OverwriteChanges);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        conn_db.Close();
                        reader.Close();
                        return null;
                    }
                    //исключение nzp_calc
                    Data_Table.Columns.Remove("nzp_calc");
                    //исключение nzp_dom
                    Data_Table.Columns.Remove("nzp_dom");

                    sql.Remove(0, sql.Length);
                    reader.Close();
                    conn_db.Close(); //закрыть соединение с основной базой     
                    MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
                    return Data_Table;
                }
                else
                {
                    MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
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
                sql.Remove(0, sql.Length);
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
        }

        ////прроцедура получения КОЛЛЕКЦИИ из tXX_calcs_MM_ГГ
        //public List<Calcs> GetCalcsCollection(out Returns ret, string Nzp_user, string mm, string yy)
        //{
        //    MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

        //    ret = Utils.InitReturns();
        //    List<Calcs> CalcsCollect = new List<Calcs>();

        //    IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
        //    IDataReader reader;

        //    ret = OpenDb(conn_db, true);
        //    if (!ret.result)
        //    {
        //        MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
        //        return null;
        //    }


        //    StringBuilder sql = new StringBuilder();
        //    sql.Append(" select  c.* ");
        //    sql.Append(" from  t" + Nzp_user + "_calcs_" + mm + "_" + yy + " c ");

        //    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
        //    {
        //        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
        //        conn_db.Close();
        //        sql.Remove(0, sql.Length);
        //        ret.result = false;
        //        return null;
        //    }

        //    try
        //    {
        //        if (reader != null)
        //        {
        //            try
        //            {
        //                while (reader.Read())
        //                {
        //                    //заполнение CalcsCollect
        //                    Calcs calc = new Calcs();
        //                    if (reader["adr"] != DBNull.Value) calc.adr = reader["adr"].ToString().Trim();
        //                    if (reader["serv"] != DBNull.Value) calc.service = reader["serv"].ToString().Trim();
        //                    if (reader["ed_serv"] != DBNull.Value) calc.ed_serv = reader["ed_serv"].ToString().Trim();
        //                    if (reader["val1"] != DBNull.Value) calc.val1 = Convert.ToDecimal(reader["val1"]);
        //                    if (reader["val2"] != DBNull.Value) calc.val2 = Convert.ToDecimal(reader["val2"]);
        //                    if (reader["val3"] != DBNull.Value) calc.val3 = Convert.ToDecimal(reader["val3"]);
        //                    if (reader["squ1"] != DBNull.Value) calc.squ1 = Convert.ToDecimal(reader["squ1"]);
        //                    if (reader["squ2"] != DBNull.Value) calc.squ2 = Convert.ToDecimal(reader["squ2"]);
        //                    if (reader["cnt1"] != DBNull.Value) calc.cnt1 = Convert.ToInt32(reader["cnt1"]);
        //                    if (reader["gil1"] != DBNull.Value) calc.gil1 = Convert.ToDecimal(reader["gil1"]);
        //                    if (reader["dlt_calc"] != DBNull.Value) calc.dlt_calc = Convert.ToDecimal(reader["dlt_calc"]);
        //                    if (reader["kf307"] != DBNull.Value) calc.kf307 = Convert.ToDecimal(reader["kf307"]);

        //                    CalcsCollect.Add(calc);
        //                }

        //            }
        //            catch (Exception ex)
        //            {
        //                MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
        //                conn_db.Close();
        //                reader.Close();
        //                return null;
        //            }


        //            sql.Remove(0, sql.Length);
        //            reader.Close();
        //            conn_db.Close(); //закрыть соединение с основной базой     

        //            return CalcsCollect;
        //        }
        //        else
        //        {
        //            MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
        //            ret.text = "Reader пуст!";
        //            ret.result = false;
        //            return null;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("ExcelReport : " + ex.Message, MonitorLog.typelog.Error, true);

        //        if (reader != null)
        //        {
        //            reader.Close();
        //        }
        //        sql.Remove(0, sql.Length);
        //        conn_db.Close();
        //        ret.result = false;
        //        ret.text = ex.Message;
        //        return null;
        //    }
        //}


        //постановка отчета на задание
        public Returns AddToPoolThread(int nzp_user, string parametrs, int typeR, string repName, ref string time, string comment)
        {
            Returns ret = Utils.InitReturns();


            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }

            #region Проверка на существование таблицы excel_utility, если нет, то создаем

            if (DBManager.DbCreateTable(conn_db, DBManager.CreateTableArgs.CreateIfNotExists, false,
                DBManager.GetFullBaseName(conn_db), "excel_utility",
                "nzp_exc serial not null",
                "nzp_user integer not null",
                "prms char(200) not null",
                "stats integer default 0",
                "dat_in " + sDateTimeType,
                "dat_start " + sDateTimeType,
                "dat_out " + sDateTimeType,
                "tip integer default 0 not null",
                "rep_name char(100)",
                "exc_path char(200)",
                "exc_comment char(200)",
                "dat_today date").result)
            {
                ExecSQL(conn_db,
                    " create unique index " + sDefaultSchema + "ix_exc_1 on " + sDefaultSchema +
                    "excel_utility (nzp_exc); ", false);
                ExecSQL(conn_db,
                    " create        index " + sDefaultSchema + "ix_exc_2 on " + sDefaultSchema +
                    "excel_utility (nzp_user); ", false);
                ExecSQL(conn_db, sUpdStat + " "+sDefaultSchema +"excel_utility ", true);
            }
            #endregion


            StringBuilder sql = new StringBuilder();

            string DateTimeString = IfmxFormatDatetimeToTime(DateTime.Now.ToString(), out ret);
            time = DateTimeString;

            sql.Append(" insert into "+sDefaultSchema+"excel_utility ( nzp_user, prms, dat_in,  tip, rep_name, exc_comment, dat_today) ");
            sql.Append(" values (" + nzp_user + ", \'" + parametrs + "\'," + "\'" + DateTimeString + "\'," + typeR + ", \'" + repName + "\', \'" + comment + "\', \'" + DateTime.Now.ToShortDateString() + "\');");

            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);

            try
            {
                ret.text = "Записано строк : " + cmd.ExecuteNonQuery().ToString();
                ret.tag = GetSerialValue(conn_db, null);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка записи в БД (AddToPoolThread): " + ex.Message, MonitorLog.typelog.Error, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
            }


            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        //отметка отчета как успешно выполненного
        public Returns MarkPoolThread(int nzp_user, string stats, int typeR, string path, string time)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }

            StringBuilder sql = new StringBuilder();

            string DateTimeString = IfmxFormatDatetimeToTime(DateTime.Now.ToString(), out ret);
#if PG
            ExecSQL(conn_db, "set search_path to public", true);
#else
#endif
            sql.Append(" update excel_utility set ( stats ,  dat_out,  exc_path) = ");
            sql.Append(" (" + stats + ",\'" + DateTimeString + "\', \'" + path + "\'" + ") where  nzp_user =" + nzp_user + " and  tip = " + typeR + " and dat_in = \'" + time + "\';");

            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);

            try
            {
                ret.text = "Обновлено строк : " + cmd.ExecuteNonQuery().ToString();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка записи в БД (AddToPoolThread): " + ex.Message, MonitorLog.typelog.Error, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
            }


            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        //Отметка о старте формирования отчета
        public Returns MarkStartPoolThread(int nzp_user, string stats, int typeR, string comment, string time)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }

            StringBuilder sql = new StringBuilder();

            string DateTimeString = IfmxFormatDatetimeToTime(DateTime.Now.ToString(), out ret);


            sql.Append(" update excel_utility set ( stats ,   dat_start) =  ");
            sql.Append(" (" + stats + ",\'" + DateTimeString + "\')" + " where  nzp_user =" + nzp_user + " and  tip = " + typeR + " and dat_in = \'" + time + "\';");

            IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), conn_db);

            try
            {
                ret.text = "Обновлено строк : " + cmd.ExecuteNonQuery().ToString();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка записи в БД (AddToPoolThread): " + ex.Message, MonitorLog.typelog.Error, true);
                conn_db.Close();
                sql.Remove(0, sql.Length);
            }


            conn_db.Close();
            sql.Remove(0, sql.Length);

            return ret;
        }

        public DataTable GetChargeStatistics(int year_, StringBuilder sql, out Returns ret)
        {

            ret = Utils.InitReturns();
            DataTable DT = new DataTable();

            #region Подключение к БД
            IDataReader reader;

            IDbConnection conn_db;
            conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                ret.result = false;
                return DT;
            }
            #endregion

            string prefix = Points.Pref + "_fin_" + (year_ - 2000).ToString("00") + "@" + DBManager.getServer(conn_db);
            sql.Replace("{prefix}", prefix);
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return DT;
            }
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            if (reader == null)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return DT;

            }

            try
            {

                //заполнение DataTable
                DT.Load(reader, LoadOption.OverwriteChanges);
                reader.Close();
                conn_db.Close();
                return DT;

            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                conn_db.Close();
                return DT;
            }


        }

        public DataTable GetAnalisKartTableLS(List<Prm> listprm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            IDataReader reader2 = null;

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
            string tXX_spls = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter
                + "t" + Nzp_user + "_spls";

            string pref = String.Empty;
            string where_supp = String.Empty;
            string where_serv = String.Empty;
            conn_web.Close();
            try
            {

                #region Ограничения на выборку
                if (listprm[0].RolesVal != null)
                {
                    if (listprm[0].RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in listprm[0].RolesVal)
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


                ExecSQL(conn_db, "drop table sel_kvar1", false);
                sql.Remove(0, sql.Length);
                sql.Append(" Create temp table sel_kvar1 ( " +
                           " nzp_geu integer, " +
                           " nzp_kvar integer, " +
                           " pref char(20))" + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" insert into sel_kvar1(nzp_geu,nzp_kvar, pref) " +
                           " select nzp_geu,nzp_kvar, pref from " + tXX_spls);
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "create index ix_tmp01192 on sel_kvar1(nzp_kvar)", true);
                ExecSQL(conn_db, DBManager.sUpdStat + " sel_kvar1", true);



                ExecRead(conn_db, out reader2, "drop table t_svod", false);

                sql.Remove(0, sql.Length);
                sql.Append(" Create temp table t_svod( ");
                sql.Append(" nzp_geu integer, ");
                sql.Append(" nzp_serv integer, ");
                sql.Append(" nzp_supp integer, ");
                sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2),");
                sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2),");
                sql.Append(" izm_tarif " + DBManager.sDecimalType + "(14,2),");
                sql.Append(" vozv " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" reval_k " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" reval_d " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_ito " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_odn " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_insaldo_odn " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), ");
                sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
                sql.Append(" select pref ");
                sql.Append(" from  sel_kvar1 group by 1");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    throw new Exception("Ошибка выброрки таблицы sel_kvar1 ");

                while (reader.Read())
                {
                    if (reader["pref"] != null)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();
                        string sAliasm = pref + "_charge_" + (listprm[0].year_ - 2000).ToString("00");

                        sql.Remove(0, sql.Length);
                        sql.Append("insert into t_svod (nzp_geu,nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, izm_tarif,vozv, ");
                        sql.Append(" reval_k, reval_d, sum_ito, sum_charge, sum_money, sum_outsaldo, sum_odn, sum_insaldo_odn) ");
                        sql.Append("select b.nzp_geu,(case when nzp_supp = 612 and nzp_serv in (6,7,14) then -nzp_serv else nzp_serv end),  ");
                        sql.Append(" nzp_supp, sum(sum_insaldo) as sum_insaldo, sum(rsum_tarif) as rsum_tarif, ");
                        sql.Append("sum(0) as izm_tarif,sum(sum_nedop) as vozv, ");

                        sql.Append("sum(case when real_charge<0 then real_charge else 0 end) + ");
                        sql.Append("sum(case when reval<0 then reval else 0 end) as reval_k, ");
                        sql.Append("sum(case when real_charge<0 then 0 else real_charge end) + ");
                        sql.Append("sum(case when reval<0 then 0 else reval end) as reval_d, ");

                        sql.Append("sum(sum_tarif+reval+real_charge) as sum_ito, ");
                        sql.Append("sum(sum_charge) as sum_charge, ");
                        sql.Append("sum(sum_money) as sum_money, ");
                        sql.Append("sum(sum_outsaldo) as sum_outsaldo, sum(0) as sum_odn, sum(0) as sum_insaldo_odn");
                        sql.Append(" from " + sAliasm + DBManager.tableDelimiter + "charge_" +
                            listprm[0].month_.ToString("00") + " a, sel_kvar1 b ");
                        sql.Append(" where a.nzp_kvar=b.nzp_kvar and dat_charge is null and nzp_serv>1");
                        sql.Append(" and abs(sum_insaldo)+abs(rsum_tarif)+abs(real_charge)+abs(reval)+abs(sum_money)+abs(sum_charge)>0.001");
                        sql.Append(where_serv + where_supp);
                        sql.Append(" group by 1,2,3 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //Перекидки ОДН по статье содержание жилья
                        sql.Remove(0, sql.Length);
                        sql.Append("insert into t_svod (nzp_geu, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, izm_tarif,vozv, ");
                        sql.Append(" reval_k, reval_d, sum_ito, sum_charge, sum_money, sum_outsaldo, sum_odn) ");
                        sql.Append(" select b.nzp_geu,nzp_serv, nzp_supp,  ");
                        sql.Append(" sum(0) as sum_insaldo, sum(0) as rsum_tarif, ");
                        sql.Append(" sum(0) as izm_tarif,sum(0) as vozv, ");
                        sql.Append(" -sum(sum_rcl) as reval_k, ");
                        sql.Append(" sum(0) as reval_d, ");
                        sql.Append(" sum(0) as sum_ito, ");
                        sql.Append(" sum(0) as sum_charge, ");
                        sql.Append(" sum(0) as sum_money, ");
                        sql.Append(" sum(0) as sum_outsaldo, sum(sum_rcl) as sum_odn");
                        sql.Append(" from " + sAliasm + DBManager.tableDelimiter + "perekidka a, sel_kvar1 b ");
                        sql.Append(" where a.nzp_kvar=b.nzp_kvar and type_rcl = 63 and month_=" + listprm[0].month_.ToString("00"));
                        sql.Append(" and abs(sum_rcl)>0.001");
                        sql.Append(where_serv + where_supp);
                        sql.Append(" group by 1,2,3 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        //Жигулевск недопоставка как перекидка
                        sql.Remove(0, sql.Length);
                        sql.Append("insert into t_svod (nzp_geu, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, izm_tarif,vozv, ");
                        sql.Append(" reval_k, reval_d, sum_ito, sum_charge, sum_money, sum_outsaldo, sum_odn) ");
                        sql.Append(" select b.nzp_geu,nzp_serv, nzp_supp,  ");
                        sql.Append(" sum(0) as sum_insaldo, sum(0) as rsum_tarif, ");
                        sql.Append(" sum(0) as izm_tarif,sum(-sum_rcl) as vozv, ");
                        sql.Append(" -sum(sum_rcl) as reval_k, ");
                        sql.Append(" sum(0) as reval_d, ");
                        sql.Append(" sum(0) as sum_ito, ");
                        sql.Append(" sum(0) as sum_charge, ");
                        sql.Append(" sum(0) as sum_money, ");
                        sql.Append(" sum(0) as sum_outsaldo, sum(0) as sum_odn");
                        sql.Append(" from " + sAliasm + DBManager.tableDelimiter + "perekidka a, sel_kvar1 b ");
                        sql.Append(" where a.nzp_kvar=b.nzp_kvar and type_rcl = 101 and month_=" + listprm[0].month_.ToString("00"));
                        sql.Append(" and abs(sum_rcl)>0.001");
                        sql.Append(where_serv + where_supp);
                        sql.Append(" group by 1,2,3 ");
                        ExecSQL(conn_db, sql.ToString(), true);

                        #region Выборка перерасчетов прошлого периода

                        ExecSQL(conn_db, "drop table t_nedop", false);
                        sql.Remove(0, sql.Length);
                        sql.Append(" Create temp table t_nedop ( " +
                                   " nzp_geu integer, " +
                                   " nzp_kvar integer, " +
                                   " month_s integer, " +
                                   " month_po integer)" + DBManager.sUnlogTempTable);
                        ExecSQL(conn_db, sql.ToString(), true);

                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_nedop(nzp_geu, nzp_kvar, month_s, month_po) ");
                        sql.Append(" select b.nzp_geu, a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                        sql.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar1 b ");
                        sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + listprm[0].month_.ToString("00") + "." +
                            listprm[0].year_.ToString("0000") + "' ");
                        sql.Append(" group by 1,2 ");
                        ExecSQL(conn_db, sql.ToString(), true);


                        sql.Remove(0, sql.Length);
                        sql.Append(" select month_, year_ ");
                        sql.Append(" from " + sAliasm + DBManager.tableDelimiter + "lnk_charge_" + listprm[0].month_.ToString("00") + " b, t_nedop d ");
                        sql.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                        sql.Append(" group by 1,2");
                        ExecRead(conn_db, out reader2, sql.ToString(), true);
                        while (reader2.Read())
                        {
                            string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                            sql.Remove(0, sql.Length);
                            sql.Append(" insert into t_svod (nzp_geu,nzp_serv,nzp_supp, vozv, reval_k, reval_d) ");
                            sql.Append(" select nzp_geu,(case when nzp_supp = 612 and nzp_serv in (6,7,14) then -nzp_serv else nzp_serv end), ");
                            sql.Append(" nzp_supp, sum(sum_nedop-sum_nedop_p),  ");
                            sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                            sql.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                            sql.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                            sql.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                            sql.Append(" from " + sTmpAlias + DBManager.tableDelimiter + "charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                            sql.Append(" b, t_nedop d ");
                            sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                            sql.Append(listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + "')");
                            sql.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                            sql.Append(where_serv + where_supp);
                            sql.Append(" group by 1,2,3");
                            ExecSQL(conn_db, sql.ToString(), true);

                        }
                        reader2.Close();
                        ExecSQL(conn_db, "drop table t_nedop", true);

                        #endregion

                    }

                }

                reader.Close();

                #endregion





                sql.Remove(0, sql.Length);
                sql.Append("update t_svod set nzp_serv = 17 ");
                sql.Append("where nzp_serv = 11");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "drop table t1_", false);


                sql.Remove(0, sql.Length);
                sql.Append("create temp table t1_(nzp_supp integer) " + DBManager.sUnlogTempTable);
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append("insert into t1_ ");
                sql.Append("select nzp_supp ");
                sql.Append("from t_svod ");
                sql.Append("where nzp_serv = 9  group by 1 ");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append("update t_svod set nzp_serv=9 ");
                sql.Append("where nzp_serv = 14 and nzp_supp in (select nzp_supp from t1_)");
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append("update t_svod set nzp_serv=513 ");
                sql.Append("where nzp_serv = 514 and nzp_supp in (select nzp_supp from t1_)");
                ExecSQL(conn_db, sql.ToString(), true);
               
                ExecSQL(conn_db, "drop table t1_", true);


                sql.Remove(0, sql.Length);
                sql.Append("select 2 as tip, geu,abs(a.nzp_serv) as nzp_serv, (case when a.nzp_serv in (6,7,14) then 'ВК '||trim(service) else service end) as service ,");
                sql.Append(" ordering, ");
                sql.Append("sum(sum_insaldo) as sum_insaldo, ");
                sql.Append("sum(rsum_tarif) as rsum_tarif, ");
                sql.Append("sum(0) as izm_tarif, ");
                sql.Append("sum(vozv) as vozv, ");
                sql.Append("sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, ");
                sql.Append("sum(reval_d) as reval_d, ");
                sql.Append("sum(sum_ito) as sum_ito, ");
                sql.Append("sum(sum_charge) as sum_charge, ");
                sql.Append("sum(sum_money) as sum_money, ");
                sql.Append("sum(sum_outsaldo) as sum_outsaldo, ");
                sql.Append("sum(sum_odn) as sum_odn, ");
                sql.Append("sum(sum_insaldo_odn) as sum_insaldo_odn ");
                sql.Append("from  t_svod a, " + Points.Pref + DBManager.sKernelAliasRest +
                            "services s, " + Points.Pref + DBManager.sDataAliasRest + "s_geu g ");
                sql.Append("where (case when a.nzp_serv in (-6,-7,-14) then -a.nzp_serv else a.nzp_serv end)=s.nzp_serv ");
                sql.Append(" and a.nzp_geu=g.nzp_geu ");
                sql.Append(" group by 1, 2, ordering,3, 4 ");
                sql.Append(" union all ");
                sql.Append("select 1 as tip,'0',abs(a.nzp_serv) as nzp_serv, (case when a.nzp_serv in (6,7,14) then 'ВК '||trim(service) else service end) as service ,");
                sql.Append(" ordering, ");
                sql.Append("sum(sum_insaldo) as sum_insaldo, ");
                sql.Append("sum(rsum_tarif) as rsum_tarif, ");
                sql.Append("sum(0) as izm_tarif, ");
                sql.Append("sum(vozv) as vozv, ");
                sql.Append("sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, ");
                sql.Append("sum(reval_d) as reval_d, ");
                sql.Append("sum(sum_ito) as sum_ito, ");
                sql.Append("sum(sum_charge) as sum_charge, ");
                sql.Append("sum(sum_money) as sum_money, ");
                sql.Append("sum(sum_outsaldo) as sum_outsaldo, ");
                sql.Append("sum(sum_odn) as sum_odn, ");
                sql.Append("sum(sum_insaldo_odn) as sum_insaldo_odn ");
                sql.Append("from  t_svod a, " + Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_geu g ");
                sql.Append("where (case when a.nzp_serv in (-6,-7,-14) then -a.nzp_serv else a.nzp_serv end)=s.nzp_serv ");
                sql.Append(" and a.nzp_geu=g.nzp_geu ");
                sql.Append(" group by 1, 2, 3,4,5 order by  1,2, 5, 4");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    throw new Exception("Ошибка выброрки на экран ");

                Utils.setCulture();

                if (reader != null)
                {
                    LocalTable.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();
                }


                LocalTable.Columns.Remove("ordering");

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Карточка аналитического учета " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                ExecSQL(conn_db, "drop table t_svod", false);
                ExecSQL(conn_db, "drop table sel_kvar1", false);
                conn_db.Close();
            }

            return LocalTable;


        }

        public DataTable GetCalcTarif(Prm prm, out Returns ret, string Nzp_user)
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

            IDbConnection conn_db;
            conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

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
            string tXX_spls =  "PUBLIC." + "t" + Nzp_user + "_spls";
#else
          string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string pref = "";

            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();


        

            ExecSQL(conn_db, "drop table t_svod", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create UNLOGGED  table t_svod ( ");
            sql2.Append(" nzp_serv      integer not null, ");
            sql2.Append(" rsum_tarif    NUMERIC(17,7) default 0, ");
            sql2.Append(" sum_nedop     NUMERIC(17,7) default 0, ");
            sql2.Append(" reval_d       NUMERIC(17,7) default 0, ");
            sql2.Append(" reval_k       NUMERIC(17,7) default 0, ");
            sql2.Append(" sum_money     NUMERIC(17,7) default 0, ");
            sql2.Append(" sum_charge    NUMERIC(17,7) default 0 ");
            sql2.Append(")  ");
#else
     sql2.Append(" create temp table t_svod ( ");
            sql2.Append(" nzp_serv      integer not null, ");
            sql2.Append(" rsum_tarif    decimal(17,7) default 0, ");
            sql2.Append(" sum_nedop     decimal(17,7) default 0, ");
            sql2.Append(" reval_d       decimal(17,7) default 0, ");
            sql2.Append(" reval_k       decimal(17,7) default 0, ");
            sql2.Append(" sum_money     decimal(17,7) default 0, ");
            sql2.Append(" sum_charge    decimal(17,7) default 0 ");
            sql2.Append(") with no log ");
#endif
            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "drop table sel_kvar2", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select nzp_kvar, nzp_area, pref INTO UNLOGGED sel_kvar2 from  " + tXX_spls );
#else
            sql2.Append(" select nzp_kvar, nzp_area, pref from  " + tXX_spls + " into temp sel_kvar2 with no log");
#endif
            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            ExecSQL(conn_db, "create index ixt_selkv_2 on sel_kvar2(nzp_kvar, nzp_area)  ", true);
#if PG
            ExecSQL(conn_db, "analyze sel_kvar2 ", true);
#else
            ExecSQL(conn_db, "update statistics for table sel_kvar2 ", true);
#endif
            conn_web.Close();

            sql2.Remove(0, sql2.Length);
            sql2.Append(" select pref from sel_kvar2 where pref is not null and trim(pref)<>'' group by 1");
            if (!ExecRead(conn_db, out reader, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            while (reader.Read())
            {
                
                    pref = Convert.ToString(reader["pref"]).Trim();

                    ExecSQL(conn_db, "drop table t_tarif17", false);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create UNLOGGED table t_tarif17 ( ");
                    sql2.Append(" nzp_area      integer not null, ");
                    sql2.Append(" nzp_serv      integer not null, ");
                    sql2.Append(" itarif        integer not null, ");
                    sql2.Append(" tarif         NUMERIC(14,2) default 0, ");
                    sql2.Append(" itog_tarif    NUMERIC(14,2) default 0 ");
                    sql2.Append(") ");
#else
         sql2.Append(" create temp table t_tarif17 ( ");
                    sql2.Append(" nzp_area      integer not null, ");
                    sql2.Append(" nzp_serv      integer not null, ");
                    sql2.Append(" itarif        integer not null, ");
                    sql2.Append(" tarif         decimal(14,2) default 0, ");
                    sql2.Append(" itog_tarif    decimal(14,2) default 0 ");
                    sql2.Append(") with no log ");
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


                    sql2.Append(" INSERT INTO t_tarif17 (nzp_area, nzp_serv, itarif, tarif, itog_tarif) ");
                    sql2.Append(" SELECT s.nzp_area, a.nzp_serv,s.nzp_tarif, max(a.sump) tarif, max(s.sumt) ");
                    sql2.Append(" FROM " + pref + sDataAliasRest+"s_calc_trf_lnk a, ");
                    sql2.Append(pref + sDataAliasRest + "s_calc_trf s ");
                    sql2.Append(" WHERE a.nzp_tarif = s.nzp_tarif ");
                    sql2.Append("       AND s.dat_s<=MDY(" + prm.month_.ToString() + ",01," + prm.year_ + ")  ");
                    sql2.Append("       AND s.dat_po>=MDY(" + prm.month_.ToString() + ",01," + prm.year_ + ")  ");
                    sql2.Append(" GROUP BY 1,2,3 ");
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    ExecSQL(conn_db, "drop table t1 ", false);




                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append("  select k.nzp_kvar,coalesce(s.nzp_serv, -1001) as nzp_serv , ");
#else
                    sql2.Append("  select k.nzp_kvar,nvl(s.nzp_serv, -1001) as nzp_serv , ");
#endif

                    sql2.Append(" (case when s.nzp_serv is null then 1 else s.tarif/itog_tarif end ) as proc ");

#if PG
                    sql2.Append("  INTO UNLOGGED t2 from  " + pref + "_data.tarif t, sel_kvar2 k left outer join  t_tarif17 s on  s.nzp_area=k.nzp_area "
                        + "left outer join " + Points.Pref + "_data.a_trf_smr f on s.itog_tarif = f.tarif_new  ");
                    sql2.Append(" where t.nzp_serv=17   and t.is_actual=1  ");
                    sql2.Append(" and t.nzp_kvar=k.nzp_kvar  and t.nzp_frm=f.nzp_frm ");
                    sql2.Append(" and t.dat_s<='01." + prm.month_ + "." + prm.year_ + "' and t.dat_po>='01." + prm.month_ + "." + prm.year_ + "' ");
                    sql2.Append(" group by 1,2,3 ");
    
#else
                    sql2.Append(" from sel_kvar2 k, " + pref + "_data:tarif t, outer (t_tarif17 s, " + Points.Pref + "_data:a_trf_smr f)");
                    sql2.Append(" where t.nzp_serv=17 and s.itog_tarif=f.tarif_new and t.is_actual=1  ");
                    sql2.Append(" and t.nzp_kvar=k.nzp_kvar and s.nzp_area=k.nzp_area and t.nzp_frm=f.nzp_frm ");
                    sql2.Append(" and t.dat_s<='01."+prm.month_+"."+prm.year_+"' and t.dat_po>='01."+prm.month_+"."+prm.year_+"' ");
                    sql2.Append(" group by 1,2,3 ");
                    sql2.Append(" into temp t2 with no log  ");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }
                    ExecSQL(conn_db,  "drop table t_tarif17 ", false);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod (nzp_serv, rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money) ");
                    sql2.Append(" select coalesce(t.nzp_serv, -1001) as nzp_serv, sum(rsum_tarif * coalesce(proc,1)) as rsum_tarif, ");
                    sql2.Append(" sum(sum_nedop*coalesce(proc,1)) as sum_nedop,  ");
                    sql2.Append(" sum(case when reval<0 then (reval)*coalesce(proc,1) else 0 end) + ");
                    sql2.Append(" sum(case when real_charge<0 then (real_charge)*coalesce(proc,1) else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when reval<0 then 0 else (reval)*coalesce(proc,1) end) + ");
                    sql2.Append(" sum(case when real_charge<0 then 0 else (real_charge)*coalesce(proc,1) end) as reval_d, ");
                    sql2.Append(" sum(sum_charge*coalesce(proc,1)) as sum_charge, ");
                    sql2.Append(" sum(sum_money*coalesce(proc,1)) as sum_money ");
                    sql2.Append(" from  sel_kvar2 s,  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ".charge_" + prm.month_.ToString("00") + " a left outer join t2 t  on  A .nzp_kvar = T .nzp_kvar");
                    sql2.Append(" where a.nzp_serv=17 and dat_charge is null  ");
                    sql2.Append("  and a.nzp_kvar=s.nzp_kvar ");
                    sql2.Append(" group by 1 ");
#else
             sql2.Append(" insert into t_svod (nzp_serv, rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money) ");
                    sql2.Append(" select nvl(t.nzp_serv, -1001) as nzp_serv, sum(rsum_tarif * nvl(proc,1)) as rsum_tarif, ");
                    sql2.Append(" sum(0) as sum_nedop,  ");
                    //sql2.Append(" sum(case when reval<0 then (reval)*nvl(proc,1) else 0 end) + ");
                    sql2.Append(" sum(0) + ");
                    sql2.Append(" sum(case when real_charge<0 then (real_charge)*nvl(proc,1) else 0 end) as reval_k, ");
                    //sql2.Append(" sum(case when reval<0 then 0 else (reval)*nvl(proc,1) end) + ");
                    sql2.Append(" sum(0) + ");
                    sql2.Append(" sum(case when real_charge<0 then 0 else (real_charge)*nvl(proc,1) end) as reval_d, ");
                    sql2.Append(" sum(sum_charge*nvl(proc,1)) as sum_charge, ");
                    sql2.Append(" sum(sum_money*nvl(proc,1)) as sum_money ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ":charge_" + prm.month_.ToString("00") + " a,sel_kvar2 s, outer t2 t");
                    sql2.Append(" where a.nzp_serv=17 and dat_charge is null and ");
                    sql2.Append(" a.nzp_kvar=t.nzp_kvar and a.nzp_kvar=s.nzp_kvar ");
                    sql2.Append(" group by 1 ");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    //Добавляем доп услуги
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" insert into t_svod (nzp_serv, rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money) ");
                    sql2.Append(" select nzp_serv, sum(rsum_tarif) as rsum_tarif, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(case when reval<0 then (reval) else 0 end) + ");
                    sql2.Append(" sum(case when real_charge<0 then (real_charge) else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when reval<0 then 0 else (reval) end) + ");
                    sql2.Append(" sum(case when real_charge<0 then 0 else (real_charge) end) as reval_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, ");
                    sql2.Append(" sum(sum_money) as sum_money ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ".charge_" + prm.month_.ToString("00") + " a, sel_kvar2 t");
                    sql2.Append(" where a.nzp_serv in (2,22,11) and dat_charge is null and ");
                    sql2.Append(" a.nzp_kvar=t.nzp_kvar  ");
                    sql2.Append(" group by 1 ");
#else
    sql2.Append(" insert into t_svod (nzp_serv, rsum_tarif, sum_nedop, reval_k, reval_d, sum_charge, sum_money) ");
                    sql2.Append(" select nzp_serv, sum(rsum_tarif) as rsum_tarif, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(case when reval<0 then (reval) else 0 end) + ");
                    sql2.Append(" sum(case when real_charge<0 then (real_charge) else 0 end) as reval_k, ");
                    sql2.Append(" sum(case when reval<0 then 0 else (reval) end) + ");
                    sql2.Append(" sum(case when real_charge<0 then 0 else (real_charge) end) as reval_d, ");
                    sql2.Append(" sum(sum_charge) as sum_charge, ");
                    sql2.Append(" sum(sum_money) as sum_money ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00")
                        + ":charge_" + prm.month_.ToString("00") + " a, sel_kvar2 t");
                    sql2.Append(" where a.nzp_serv in (2,22,11) and dat_charge is null and ");
                    sql2.Append(" a.nzp_kvar=t.nzp_kvar  ");
                    sql2.Append(" group by 1 ");
#endif
                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    #region Выборка перерасчетов прошлого периода

                    ExecSQL(conn_db, "drop table t_nedop_sum", false);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create unlogged table t_nedop_sum(nzp_kvar integer, nzp_serv integer, sum_nedop Decimal(14,2), ");
                    sql2.Append(" reval_k numeric(14,2), reval_d numeric(14,2))  ");
#else
                  sql2.Append(" create temp table t_nedop_sum(nzp_kvar integer, nzp_serv integer, sum_nedop Decimal(14,2), ");
                    sql2.Append(" reval_k Decimal(14,2), reval_d Decimal(14,2)) with no log");
#endif
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select a.nzp_kvar, min(extract(year from (dat_s))::int*12+extract( month from (dat_s))::int) as month_s,");
                    sql2.Append(" max(extract (year from (dat_po))::int*12+ extract (month from (dat_po))::int) as month_po");
                    sql2.Append(" into unlogged t_nedop  from " + pref + "_data.nedop_kvar a, sel_kvar2 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "' and a.nzp_serv in (17,2,11,22) ");
                    sql2.Append(" group by 1  ");
#else
        sql2.Append(" select a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,");
                    sql2.Append(" max(extract(year from dat_s)*12+month(dat_po)) as month_po");
                    sql2.Append(" from " + pref + "_data:nedop_kvar a, sel_kvar2 b ");
                    sql2.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                        prm.year_.ToString("0000") + "' and a.nzp_serv in (2,11,22) ");
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
#if PG
                        sql2.Append(" insert into t_nedop_sum (nzp_kvar, nzp_serv, sum_nedop, reval_k, reval_d)   ");
                        sql2.Append(" select b.nzp_kvar, nzp_serv,  ");
                        sql2.Append(" sum(sum_nedop-sum_nedop_p),  ");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then sum_nedop-sum_nedop_p else 0 end ) as reval_k,");
                        sql2.Append(" sum(case when (sum_nedop-sum_nedop_p)>0 ");
                        sql2.Append(" then 0 else sum_nedop-sum_nedop_p end ) as reval_d");
                        sql2.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                        sql2.Append(" b, t_nedop d ");
                        sql2.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                        sql2.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                        sql2.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                        sql2.Append(" and nzp_serv in (17,2,11,22)");
                        sql2.Append(" group by 1,2");
#else
   sql2.Append(" insert into t_nedop_sum (nzp_kvar, nzp_serv, sum_nedop, reval_k, reval_d)   ");
                        sql2.Append(" select b.nzp_kvar, nzp_serv,  ");
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
                        sql2.Append(" and nzp_serv in (2,11,22)");
                        sql2.Append(" group by 1,2");
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


                    //Добавляем доп услуги
                    sql2.Remove(0, sql2.Length);

                    //Добавляем доп услуги
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod (nzp_serv, sum_nedop, reval_k, reval_d) ");
                    sql2.Append(" select nzp_serv, ");
                    sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
                    sql2.Append(" sum(reval_k) as reval_k, ");
                    sql2.Append(" sum(reval_d) as reval_d ");
                    sql2.Append(" from t_nedop_sum a");
                    sql2.Append(" where a.nzp_serv in (2,11,22) ");
                    sql2.Append(" group by 1 ");
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }


                    ExecSQL(conn_db, "drop table t_nedop", true);
                    ExecSQL(conn_db, "drop table t_nedop_sum", false);
                    #endregion


                    ExecSQL(conn_db,  "drop table t2", true);




                

            }
            reader.Close();

            #region Вычисление перерасчетов по содержанию жилья
            GetSumNedopByStatCalc(conn_db, "sel_kvar2", prm);
            sql2.Remove(0, sql2.Length);
            sql2.Append(" insert into t_svod (nzp_serv, sum_nedop, reval_k, reval_d) ");
            sql2.Append(" select nzp_serv_sg, ");
            sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
            sql2.Append(" sum(0) as reval_k, ");
            sql2.Append(" sum(0) as reval_d ");
            sql2.Append(" from t_sprav_otkl_usl a");
            sql2.Append(" group by 1 ");
            ExecSQL(conn_db, sql2.ToString(), true);

            DataTable dt = DBManager.ExecSQLToTable(conn_db, "select sum(sum_nedop) from t_sprav_otkl_usl");
            ExecSQL(conn_db, "drop table t_sprav_otkl_usl ", true);

            #endregion
            
            ExecSQL(conn_db, "drop table sel_kvar2", true);



            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select (case when t.nzp_serv in (2,11,22) then ordering+100000 ");
            sql2.Append(" when t.nzp_serv = -1001 then 999 else ordering end) as ordering, ");
            sql2.Append(" coalesce(service,'СЖ - Нет раскладки ') as service, ");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
            sql2.Append(" sum(-1*reval_k) as reval_k, ");
            sql2.Append(" sum(reval_d) as reval_d, ");
            sql2.Append(" sum(sum_charge) as sum_charge, ");
            sql2.Append(" sum(sum_money) as sum_money ");
            sql2.Append(" from t_svod t left outer join " + Points.Pref + "_kernel" + DBManager.getServer(conn_db) + ".services s  on t.nzp_serv=s.nzp_serv   ");
            sql2.Append(" where  abs(coalesce(rsum_tarif,0))+abs(coalesce(sum_nedop,0))+");
            sql2.Append(" abs(coalesce(reval_k,0))+ abs(coalesce(reval_d,0))+ abs(coalesce(sum_charge,0)) + abs(coalesce(sum_money,0))>0.001");
            sql2.Append(" group by 1,2 order by 1,2 ");
#else
            sql2.Append(" select (case when t.nzp_serv in (2,11,22) then ordering+100000 ");
            sql2.Append(" when t.nzp_serv = -1001 then 999 else ordering end) as ordering, ");
            sql2.Append(" nvl(service,'СЖ - Нет раскладки ') as service, ");
            sql2.Append(" sum(rsum_tarif) as rsum_tarif, ");
            sql2.Append(" sum(sum_nedop) as sum_nedop,  ");
            sql2.Append(" sum(-1*reval_k) as reval_k, ");
            sql2.Append(" sum(reval_d) as reval_d, ");
            sql2.Append(" sum(sum_charge) as sum_charge, ");
            sql2.Append(" sum(sum_money) as sum_money ");
            sql2.Append(" from t_svod t, outer " + Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s ");
            sql2.Append(" where t.nzp_serv=s.nzp_serv and abs(nvl(rsum_tarif,0))+abs(nvl(sum_nedop,0))+");
            sql2.Append(" abs(nvl(reval_k,0))+ abs(nvl(reval_d,0))+ abs(nvl(sum_charge,0)) + abs(nvl(sum_money,0))>0.001");
            sql2.Append(" group by 1,2 order by 1,2 ");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
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
                    //заполнение DataTable
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
            reader2.Close();
            #endregion
            ExecSQL(conn_db,  "drop table t_svod", true);
            //LocalTable.Columns.Remove("ordering");
            conn_db.Close();
            
            return LocalTable;

        }

        public DataTable GetVedOplLs(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDataReader reader;
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с Webdata БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
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
#if PG
            string tXX_spls = "public" +  "." + "t" + Nzp_user + "_spls";
#else
 string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();


#if PG
            string baseFin = Points.Pref + "_fin_" + (prm.year_ - 2000).ToString("00");
            string baseData = Points.Pref + "_data";
            string baseKernel = Points.Pref + "_kernel" ;
#else
string baseFin = Points.Pref + "_fin_" + (prm.year_ - 2000).ToString("00") +
            "@" + DBManager.getServer(conn_db);
            string baseData = Points.Pref + "_data" + "@" + DBManager.getServer(conn_db);
            string baseKernel = Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db);
#endif

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select p.dat_pack, p.num_pack, geu, t.pkod10, ulica, ndom, idom,'' as nkor,");
            sql.Append(" nkvar, ikvar, '' as nkvar_n, coalesce(fio,' ') as fio, pl.dat_vvod, dat_month, sum_ls, g_sum_ls, 0 as peni, ");
            sql.Append(" coalesce(anketa,'0') as anketa, coalesce(p.num_pack,' ') as num_pack , coalesce(prefix_ls,0) as prefix_ls, coalesce(payer,' ') as payer ");
            sql.Append(" from " + baseFin + ".pack p, " + baseFin + ".pack_ls pl, ");
            sql.Append(baseData + ".s_geu b, " + tXX_spls + " t,");
            sql.Append(baseKernel + ".s_bank s left outer join " + baseKernel + ".s_payer sp  on s.nzp_payer = sp.nzp_payer");
            sql.Append(" where p.nzp_pack=pl.nzp_pack");
            sql.Append(" and t.num_ls=pl.num_ls and t.nzp_geu=b.nzp_geu ");
            sql.Append(" and p.nzp_bank=s.nzp_bank and s.nzp_payer=sp.nzp_payer");
            sql.Append(" AND EXTRACT(MONTH from  (P .dat_pack)) =" + prm.month_.ToString());
            sql.Append(" AND EXTRACT(YEAR from  (P .dat_pack)) =" + prm.year_.ToString());
            sql.Append(" order by ulica, idom, ndom, nkor, ikvar, nkvar, fio");
#else
          sql.Append(" select p.dat_pack, p.num_pack, geu, t.pkod10, ulica, ndom, idom,'' as nkor,");
            sql.Append(" nkvar, ikvar, '' as nkvar_n, nvl(fio,' ') as fio, pl.dat_vvod, dat_month, sum_ls, g_sum_ls, 0 as peni, ");
            sql.Append(" nvl(anketa,'0') as anketa, nvl(p.num_pack,' ') as num_pack , nvl(prefix_ls,0) as prefix_ls, nvl(payer,' ') as payer ");
            sql.Append(" from " + baseFin + ":pack p, " + baseFin + ":pack_ls pl, ");
            sql.Append(baseData + ":s_geu b, " + tXX_spls + " t,");
            sql.Append(baseKernel + ":s_bank s, outer " + baseKernel + ":s_payer sp");
            sql.Append(" where p.nzp_pack=pl.nzp_pack");
            sql.Append(" and t.num_ls=pl.num_ls and t.nzp_geu=b.nzp_geu ");
            sql.Append(" and p.nzp_bank=s.nzp_bank and s.nzp_payer=sp.nzp_payer");
            sql.Append(" and month(p.dat_pack)=" + prm.month_.ToString());
            sql.Append(" and year(p.dat_pack)=" + prm.year_.ToString());
            sql.Append(" order by ulica, idom, ndom, nkor, ikvar, nkvar, fio");
#endif
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            if (reader != null)
            {

                try
                {

                    LocalTable.Load(reader, LoadOption.OverwriteChanges);
                }

                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_db.Close();
                    return null;
                }
            }
            if (reader != null) reader.Close();
            conn_db.Close();
            #endregion

            return LocalTable;

        }

        public DataTable GetAnalisKartTable(List<Prm> listprm, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);


            ret = Utils.InitReturns();
            if (listprm[0].num_ls > 0)
            {
                return this.GetAnalisKartTableLS(listprm, out ret, Nzp_user);
            }

            DataTable Data_Table = new DataTable();

            ChargeFind finder = new ChargeFind();
            finder.nzp_user = Convert.ToInt32(Nzp_user);
            finder.month_ = listprm[0].month_;
            finder.year_ = listprm[0].year_;
            if (finder.year_ < 2000) finder.year_ = finder.year_ + 2000;
            finder.groupby = Constants.act_groupby_service.ToString();
            finder.RolesVal = new List<_RolesVal>();
            if (listprm[0].nzp_geu != -1)
            {
                _RolesVal rv = new _RolesVal();
                rv.kod = Constants.role_sql_geu;
                rv.tip = Constants.role_sql;
                rv.val = listprm[0].nzp_geu.ToString();
                finder.RolesVal.Add(rv);
            }

            if (listprm[0].nzp_area != -1)
            {
                _RolesVal rv = new _RolesVal();
                rv.kod = Constants.role_sql_area;
                rv.tip = Constants.role_sql;
                rv.val = listprm[0].nzp_area.ToString();
                finder.RolesVal.Add(rv);
            }


            DbCharge dbCharge = new DbCharge();
            dbCharge.FindChargeStatistics(finder, out ret);

            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            StringBuilder sql = new StringBuilder();
            sql.Append(" select service, sum_insaldo, rsum_tarif, 0 as izm_tarif, sum_nedop as vozv, ");
            sql.Append(" -1*(reval_k + real_charge_k) as reval_minus, reval_d + real_charge_d as reval_plus,   ");
            sql.Append(" sum_tarif+reval+real_charge, sum_tarif+reval+real_charge, sum_money, sum_outsaldo ");
            sql.Append(" from  t" + Nzp_user + "_ukrgucharge order by service ");



            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
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
                        conn_db.Close();
                        reader.Close();
                        return null;
                    }


                    sql.Remove(0, sql.Length);
                    reader.Close();
                    conn_db.Close(); //закрыть соединение с основной базой     

                    return Data_Table;
                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
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
                sql.Remove(0, sql.Length);
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
        }

   

        /// <summary>
        /// формирование отчета 
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        public DataTable GetPaspRasCommon(Prm prm, out Returns ret, string Nzp_user)
        {
            MonitorLog.WriteLog("отчет для губкина.", MonitorLog.typelog.Info, 20, 201, true);
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
                sql2.Append(" select nzp_kvar, pref from " + tXX_spls);
                sql2.Append(" into temp t_adr with no log");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);


                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index idx_pasp_ras1 on t_adr(nzp_kvar)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
                sql2.Append(" create index idx_pasp_ras2 on t_adr(pref)");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
                sql2.Append(" Update statistics for table t_adr");
                ExecRead(conn_db, out reader2, sql2.ToString(), true);

                sql2.Remove(0, sql2.Length);
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
                    my_num_ls = "pkod10";
                else
                    my_num_ls = "num_ls";

                DbTables tables = new DbTables(conn_db);

                while (reader.Read())
                {
                    if (reader["pref"] != null)
                    {
                        pref = Convert.ToString(reader["pref"]).Trim();

                        sql2.Remove(0, sql2.Length);
                        sql2.Append(" insert into t_svod");
                        sql2.Append(" select k.nzp_kvar, k." + my_num_ls + ", u.ulica, d.idom, d.ndom,d.nkor,k.ikvar, k.nkvar, k.nkvar_n, k.fio,");

                        sql2.Append(" (select case when p.val_prm=1 then 'открыт' when p.val_prm=2 then 'закрыт' else  'неопределено' end ");
                        sql2.Append(" from " + pref + "_data:prm_3 p");
                        sql2.Append(" where p.nzp= k.nzp_kvar");
                        sql2.Append(" and p.nzp_prm=51");
                        sql2.Append(" and p.is_actual<>100");
                        //                    sql2.Append(" and p.dat_s<=date('31.01.2012') ");
                        //                    sql2.Append(" and p.dat_po >= date('01.01.2012')) as sost,");
                        sql2.Append(" and today between p.dat_s and p.dat_po) as sost,");


                        sql2.Append(" " + pref + "_data:get_kol_gil (date('" + ds + "'),date('" + dpo + "'), 15, k.nzp_kvar,2) as kol_gil,");

                        sql2.Append(" (select p.val_prm  ");
                        sql2.Append(" from " + pref + "_data:prm_1 p");
                        sql2.Append(" where p.nzp= k.nzp_kvar");
                        sql2.Append(" and p.nzp_prm=2005");
                        sql2.Append(" and p.is_actual<>100");
                        sql2.Append(" and p.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p.dat_po >= date('" + ds + "')) as kol_prm_pasp ,");

                        sql2.Append(" t.pref as base ,");

                        sql2.Append(" (select p.val_prm  ");
                        sql2.Append(" from " + pref + "_data:prm_1 p");
                        sql2.Append(" where p.nzp= k.nzp_kvar");
                        sql2.Append(" and p.nzp_prm=5");
                        sql2.Append(" and p.is_actual<>100");
                        sql2.Append(" and p.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p.dat_po >= date('" + ds + "')) as kol_prm");

                        sql2.Append(" from " + tables.kvar + " k, " + tables.dom + " d, " + tables.ulica + " u, t_adr t");
                        sql2.Append(" where t.pref='" + pref + "'");
                        sql2.Append(" and k.nzp_kvar=t.nzp_kvar");
                        sql2.Append(" and k.nzp_dom=d.nzp_dom");
                        sql2.Append(" and d.nzp_ul=u.nzp_ul");

                        sql2.Append(" and 0=(select count(*)");
                        sql2.Append(" from " + pref + "_data:prm_1 p1");
                        sql2.Append(" where p1.nzp=k.nzp_kvar");
                        sql2.Append(" and p1.nzp_prm=130");
                        sql2.Append(" and p1.val_prm='1'");
                        sql2.Append(" and p1.is_actual<>100");
                        sql2.Append(" and p1.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p1.dat_po >= date('" + ds + "')) ");
                        sql2.Append(" ");
                        sql2.Append(" and  " + pref + "_data:get_kol_gil (date('" + ds + "'),date('" + dpo + "'), 15, k.nzp_kvar,2)||''<>nvl((select p.val_prm  ");
                        sql2.Append(" from " + pref + "_data:prm_1 p");
                        sql2.Append(" where p.nzp= k.nzp_kvar");
                        sql2.Append(" and p.nzp_prm=5");
                        sql2.Append(" and p.is_actual<>100");
                        sql2.Append(" and p.dat_s<=date('" + dpo + "') ");
                        sql2.Append(" and p.dat_po >= date('" + ds + "')),'0')");
                        sql2.Append(" and exists(select 1 from " + pref + "_data:kart g where g.nzp_kvar=k.nzp_kvar and g.dat_izm between date('" + ds_1 + "') and date('" + dpo + "') ) ");
                        
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
                ExecSQL(conn_db, " Drop table t_svod; ", true);
                ExecSQL(conn_db, " Drop table t_adr; ", true);

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






















        protected string BarcodeCrcSamara(string acode)
        {
            int sum_ = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 29)
                {
                    if ((i % 2) == 1)
                    {
                        sum_ = sum_ + System.Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum_ = sum_ + 3 * System.Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum_ % 10) % 10).ToString();

            return s.Substring(0, 1);

        }

        public List<_Rekvizit> GetListUkBankRekvizit(IDbConnection conn_db, string pref_data, out Returns ret)
        {
            IDataReader reader;
            List<_Rekvizit> Spis = new List<_Rekvizit>();
            ret = Utils.InitReturns();
            string s = " select *  " +
                       " from " + pref_data + ":s_bankstr " +
                       " order by nzp_area, nzp_geu  ";
            if (!ExecRead(conn_db, out reader, s, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = "Ошибка выборки " + s;
                return null;
            }
            while (reader.Read())
            {
                _Rekvizit uk = new _Rekvizit();

                if (reader["nzp_geu"] != DBNull.Value) uk.nzp_geu = System.Convert.ToInt32(reader["nzp_geu"]);
                if (reader["nzp_area"] != DBNull.Value) uk.nzp_area = System.Convert.ToInt32(reader["nzp_area"]);
                if (reader["sb1"] != DBNull.Value) uk.poluch = System.Convert.ToString(reader["sb1"]);
                if (reader["sb2"] != DBNull.Value) uk.bank = System.Convert.ToString(reader["sb2"]);
                if (reader["sb3"] != DBNull.Value) uk.rschet = System.Convert.ToString(reader["sb3"]);
                if (reader["sb4"] != DBNull.Value) uk.korr_schet = System.Convert.ToString(reader["sb4"]);
                if (reader["sb5"] != DBNull.Value) uk.bik = System.Convert.ToString(reader["sb5"]);
                if (reader["sb6"] != DBNull.Value) uk.inn = System.Convert.ToString(reader["sb6"]);
                if (reader["sb7"] != DBNull.Value) uk.phone = System.Convert.ToString(reader["sb7"]);
                if (reader["sb8"] != DBNull.Value) uk.adres = System.Convert.ToString(reader["sb8"]);
                if (reader["sb9"] != DBNull.Value) uk.pm_note = System.Convert.ToString(reader["sb9"]);
                if (reader["sb10"] != DBNull.Value) uk.poluch2 = System.Convert.ToString(reader["sb10"]);
                if (reader["sb11"] != DBNull.Value) uk.bank2 = System.Convert.ToString(reader["sb11"]);
                if (reader["sb12"] != DBNull.Value) uk.rschet2 = System.Convert.ToString(reader["sb12"]);
                if (reader["sb13"] != DBNull.Value) uk.korr_schet2 = System.Convert.ToString(reader["sb13"]);
                if (reader["sb14"] != DBNull.Value) uk.bik2 = System.Convert.ToString(reader["sb14"]);
                if (reader["sb15"] != DBNull.Value) uk.inn2 = System.Convert.ToString(reader["sb15"]);
                if (reader.FieldCount > 17)
                {
                    if (reader["sb16"] != DBNull.Value) uk.phone2 = System.Convert.ToString(reader["sb16"]);
                    if (reader["sb17"] != DBNull.Value) uk.adres2 = System.Convert.ToString(reader["sb17"]);
                    if (reader["filltext"] != DBNull.Value) uk.filltext = System.Convert.ToInt32(reader["filltext"]);
                }
                else
                {
                    uk.filltext = 1;
                }


                Spis.Add(uk);

            }
            reader.Close();
            ret.result = true;
            return Spis;

        }


        public _Rekvizit GetUkBankRekvizit(List<_Rekvizit> spis, int nzp_area, int nzp_geu, string pkod)
        {
            for (int i = 0; i < spis.Count; i++)
            {
                if (spis[i].nzp_area == nzp_area) return spis[i];
            }

            _Rekvizit uk = new _Rekvizit();
            if (pkod.Substring(0, 3) == "456")
            {
                uk.poluch = "МП городского округа Самара ''ЕИРЦ'' ";
                uk.inn = "6315856269";
                uk.bik = "043601863";
                uk.rschet = "40702810400000005350";
                uk.korr_schet = "30101810800000000706";
                uk.bank = " ОАО КБ ''Солидарность''";

                uk.poluch2 = "УК ООО ''Ремжилуниверсал''";
                uk.adres2 = "г.Самара, 443011, ул. Советской Армии, д.223";
                uk.phone2 = "(846) 341-78-15";
                uk.rschet2 = "40702810700000001607";
                uk.bank2 = "ЗАО АКБ ''ГАЗБАНК'' г.Самара, Октябрьский район";
            }
            if (pkod.Substring(0, 3) == "405")
            {
                uk.poluch = "МП городского округа Самара ''ЕИРЦ'' ";
                uk.inn = "6315856269";
                uk.bik = "043601863";
                uk.rschet = "40702810300020001032";
                uk.korr_schet = "30101810400000000863";
                uk.bank = " ЗАО АКБ ''ГАЗБАНК''";

                uk.poluch2 = "ООО ''СУТЭК''";
                uk.adres2 = "г.Самара, 443015, ул. Главная, д.3, офис 221-222";
                uk.phone2 = "(846) 310-34-85";
                uk.rschet2 = "40702810800020001396";
                uk.bank2 = "ЗАО АКБ ''ГАЗБАНК'' г.Самара";

            }
            else
            {
                uk.poluch = "МП городского округа Самара ''ЕИРЦ'' ";
                uk.inn = "6315856269";
                uk.bik = "043601706";
                uk.rschet = "40702810000020001015";
                uk.korr_schet = "30101810400000000863";
                uk.bank = " ЗАО АКБ ''ГАЗБАНК''";

                uk.poluch2 = "ЗАО ''ПТС-Сервис''";
                uk.adres2 = "г.Самара, 443110, ул. Осипенко, д.1,";
                uk.phone2 = "(846) 270-69-75";
                uk.rschet2 = "40702810800000008084";
                uk.bank2 = "ОАО ''Первый Объединенный Банк''";
            }
            return uk;
        }


        public List<string> GetFakturaFiles(Prm prm, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();

            #region Счет-фактура по новому
            try
            {
                Faktura finder = new Faktura();
                finder.pref = "";
                finder.destFileName = "Group_" + prm.year_.ToString() + prm.month_.ToString("00");
                finder.nzp_kvar = 0;
                finder.nzp_dom = 0;
                finder.nzp_user = Int32.Parse(Nzp_user);
                finder.workRegim = Faktura.WorkFakturaRegims.Group;
                finder.idFaktura = prm.nzp_key;
                finder.month_ = prm.month_;
                finder.year_ = prm.year_;
                finder.YM.year_ = finder.year_;
                finder.YM.month_ = finder.month_;
                finder.countListInPack = prm.num_page;
                finder.withDolg = Utils.GetParams(prm.prms, "withDolg");
                if (prm.pm_note == "PDF")
                    finder.resultFileType = Faktura.FakturaFileTypes.PDF;
                else
                    finder.resultFileType = Faktura.FakturaFileTypes.FPX;


                DbFaktura dbFaktura = new DbFaktura();
                List<string> fName = dbFaktura.GetFaktura(finder, out ret);
                dbFaktura.Close();
                ret.text = "Формирование счетов завершено";
                ret.result = true;
                return fName;
            }
            catch (Exception ex)
            {
                ret.text = "Формирование счетов завершилось неудачно, обратитесь к системному администратору ";
                ret.result = false;
                MonitorLog.WriteLog("ExcelReport : Ошибка формировании счетов по группе " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion



        }

        /*
        // Выгрузка реестра для загрузки в БС
        public Returns GetUploadReestr(out Returns ret, SupgFinder finder, string year, string month)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();

            #region показания ПУ
            try
            {

                DbCounter dbCounter = new DbCounter();
                ret = dbCounter.GetUploadReestr(out ret, finder, year, month);
                dbCounter.Close();
                ret.text = "Формирование выгрузки завершено";
                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.text = "Формирование выгрузки завершилось неудачно, обратитесь к системному администратору ";
                ret.result = false;
                MonitorLog.WriteLog("ExcelReport : Ошибка формировании выгрузки реестра " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion
        }*/

        //////////////////////////ПЕРЕНЕСТИ В ГЛОБАЛС/////////////////////////////////////////////////////
        //----------------------------------------------------------------------
        public string IfmxFormatDatetimeToTime(string datahour, out Returns ret)
        //----------------------------------------------------------------------
        {
            //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч:м:с"
            ret = new Returns(false);
            string outs = "";

            if (String.IsNullOrEmpty(datahour))
            {
                return outs;
            }

            datahour = datahour.Trim();

            string[] mas1 = datahour.Split(new string[] { " " }, StringSplitOptions.None);

            string dt = "";
            string hm = "";
            try
            {
                dt = mas1[0].Trim();
                hm = mas1[1].Trim();

                if (String.IsNullOrEmpty(dt) || String.IsNullOrEmpty(hm))
                {
                    return outs;
                }

                string[] mas2 = dt.Split(new string[] { "." }, StringSplitOptions.None);
                //string[] mas3 = hm.Split(new string[] { ":" }, StringSplitOptions.None);

                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + hm;
                ret.result = true;
            }
            catch
            {
                return outs;
            }

            return outs;
        }





        public DataSet GetDistribLog(PackFinder finder, out Returns ret)
        {
            try
            {
                ReturnsObjectType<DataSet> r = new ClassDB().RunSqlAction(finder, new DbPack().GetDistribLog);
                ret = r.GetReturns();
                return r.returnsData;
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return null;
            }
        }


       


    }

}

