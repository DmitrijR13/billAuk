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

        /// <summary>
        /// Возвращает информацию по расчетам с населением
        /// </summary>
        /// <param name="finder">Объект поиска типа Ls</param>
        /// <param name="nzp_supp">Номер поставщика услуг</param>
        /// <param name="month_today">Текущий месяц</param>
        /// <param name="year_today">Текущий год</param>
        /// <returns>Объект DataTable</returns>
        public DataTable GetInfPoRaschetNasel(Ls finder, int nzp_supp, int month_today, int year_today)
        {

            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;
            IDataReader reader2 = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();
            try
            {

                #region Открытие соединения с БД
                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
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


                if (finder.RolesVal != null)
                {
                    if (finder.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in finder.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_serv)
                                    where_serv += " and nzp_serv in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_supp)
                                    where_supp += " and c.nzp_supp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_wp)
                                    where_wp += " and nzp_wp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_area)
                                    where_area += " and k.nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_geu += " and k.nzp_geu in (" + role.val + ") ";


                            }
                        }
                    }
                }
                if (finder.nzp_geu > 0) where_geu += " and k.nzp_geu=" + finder.nzp_geu.ToString();
                if (finder.nzp_area > 0) where_area += " and k.nzp_area=" + finder.nzp_area.ToString();
                if (finder.nzp_dom > 0) where_dom += " and k.nzp_dom=" + finder.nzp_dom.ToString();
#if PG
                if (finder.nzp_ul > 0) where_dom += " and k.nzp_dom in (select nzp_dom from " +
                             con_db.Database.Replace("_kernel", "data") +  
                             DBManager.getServer(con_db) + ".dom where nzp_ul=" + finder.nzp_ul.ToString() + ")";
#else
       if (finder.nzp_ul > 0) where_dom += " and k.nzp_dom in (select nzp_dom from " +
                    con_db.Database.Replace("_kernel", "data") + "@" +
                    DBManager.getServer(con_db) + ":dom where nzp_ul=" + finder.nzp_ul.ToString() + ")";
#endif

                #endregion

                #region Получение списка префиксов
                sql.Remove(0, sql.Length);
                sql.Append(" select * ");
#if PG
                sql.Append(" from  " + "public" + ".s_point ");
#else
           sql.Append(" from  " + con_db.Database + "@" + DBManager.getServer(con_db) + ":s_point ");
#endif
                sql.Append(" where nzp_wp>1 " + where_wp);


                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    if (reader["bd_kernel"] != null) prefix.Add(reader["bd_kernel"].ToString().Trim());
                }
                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }
                #endregion

                #region Цикл по префиксам + создание временной таблицы
                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table Inf_po_Raschet; ", false);

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table Inf_po_Raschet (" +
                               " nzp_area      INTEGER, " +
                               " area          CHAR(100), " +
                               " typek         INTEGER, " +
                               " numb_Contr    CHAR(100) default null, " +
                               " sum_in        NUMERIC(14,2), " +
                               " nach_erc_kvt  NUMERIC(14,2), " +
                               " nach_erc_rub  NUMERIC(14,2), " +
                               " post_oplat    NUMERIC(14,2), " +
                               " sum_out       NUMERIC(14,2), " +
                               " proc_ot_oplat NUMERIC(14,2) " +
                               " )  ; "
                            );
#else
              sql.Append(" create temp table Inf_po_Raschet (" +
                             " nzp_area      INTEGER, " +
                             " area          CHAR(100), " +
                             " typek         INTEGER, " +
                             " numb_Contr    CHAR(100) default null, " +
                             " sum_in        DECIMAL(14,2), " +
                             " nach_erc_kvt  DECIMAL(14,2), " +
                             " nach_erc_rub  DECIMAL(14,2), " +
                             " post_oplat    DECIMAL(14,2), " +
                             " sum_out       DECIMAL(14,2), " +
                             " proc_ot_oplat DECIMAL(14,2) " +
                             " ) With no log; "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы Inf_po_Raschet : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                foreach (string pref in prefix)
                {
                    //цикл по месяцам с начала года
                    for (int i = 1; i <= month_today; i++)
                    {
                        //Сальдо на начало года по начислению
                        string sum_in_saldo = i == 1 ? "sum_insaldo" : "0";//сальдо берется только для первого месяца

                        sql.Remove(0, sql.Length);

#if PG
                        sql.Append(" insert into Inf_po_Raschet (nzp_area,area,typek,sum_in,nach_erc_kvt,nach_erc_rub,post_oplat)  " +
                                    " select k.nzp_area, a. area, k.typek, " +
                                    " sum(" + sum_in_saldo + ") as sum_insaldo," +
                                    " sum(CASE WHEN c.tarif>0 then (c.c_calc + c.c_reval + c.real_charge/c.tarif) else 0 end) as kvt_chas, " +
                                    " sum((c.sum_tarif + c.reval + c.real_charge)) as nach_erc_rub, " +
                                    " sum( c.sum_money) as sum_mon " +
                                    " from " + pref + "_data. kvar k," +
                                    " " + pref + "_charge_" + year_today.ToString().Substring(year_today.ToString().Length - 2) + ". charge_" + String.Format("{0:00}", i) + " c, " +
                                    " " + pref + "_data.  s_area a " +
                                    " where k.nzp_kvar = c.nzp_kvar " +
                                    " and k.nzp_area = a.nzp_area " + where_area + where_geu + where_serv + where_supp + where_dom +
                                    " and c.nzp_serv > 1   " +
                                    " and c.dat_charge is null " + " and c.nzp_serv in (25,210) ");
#else
                        sql.Append(" insert into Inf_po_Raschet (nzp_area,area,typek,sum_in,nach_erc_kvt,nach_erc_rub,post_oplat)  " +
                                    " select k.nzp_area, a. area, k.typek, " +
                                    " sum(" + sum_in_saldo + ") as sum_insaldo," +
                                    " sum(CASE WHEN c.tarif>0 then (c.c_calc + c.c_reval + c.real_charge/c.tarif) else 0 end) as kvt_chas, " +
                                    " sum((c.sum_tarif + c.reval + c.real_charge)) as nach_erc_rub, " +
                                    " sum( c.sum_money) as sum_mon " +
                                    " from " + con_db.Database.Replace("kernel", "data") + ": kvar k," +
                                    " " + pref + "_charge_" + year_today.ToString().Substring(year_today.ToString().Length - 2) + ": charge_" + String.Format("{0:00}", i) + " c, " +
                                    " " + con_db.Database.Replace("kernel", "data") + ":  s_area a " +
                                    " where k.nzp_kvar = c.nzp_kvar " +
                                    " and k.nzp_area = a.nzp_area " + where_area + where_geu + where_serv + where_supp + where_dom +
                                    " and c.nzp_serv > 1   " +
                                    " and c.dat_charge is null " +" and c.nzp_serv in (25,210) "   );
#endif
                        //фильтр по поставщикам
                        if (nzp_supp != -1)
                        {
                            sql.Append(" and c.nzp_supp = " + nzp_supp);
                        }

                        sql.Append(" group by 1,2,3 ; ");

                        ret = ExecSQL(con_db, sql.ToString(), true);

                        //проверка на успех вставки
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка заполнения таблицы Inf_po_Raschet : " + ret.text, MonitorLog.typelog.Error, true);
                            return null;
                        }
                    }

                    //обновление : вставка значений Сальдо на конец периода, Процент от оплаты населения
                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" update Inf_po_Raschet set sum_out =  " +
                               "   (sum_in + nach_erc_rub - post_oplat), ");
                    sql.Append("  proc_ot_oplat =  (CASE WHEN nach_erc_rub>0 then (post_oplat/nach_erc_rub) else 0 end)  ; ");
#else
            sql.Append(" update Inf_po_Raschet set (sum_out,proc_ot_oplat) =  " +
                               " ( " +
                                        " (sum_in + nach_erc_rub - post_oplat), " +
                                        " (CASE WHEN nach_erc_rub>0 then (post_oplat/nach_erc_rub) else 0 end) " +
                               "  ); "
                               );
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                    //проверка на успех вставки
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполнения таблицы Inf_po_Raschet : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }
                #endregion

                #region Выборка из  Inf_po_Raschet
                sql.Remove(0, sql.Length);

                sql.Append(" select area,typek,  sum(sum_in) as Saldo_in , sum(nach_erc_kvt) as nach_kilovat, " +
                            " sum(nach_erc_rub) as nach_rub, sum(post_oplat) as sumOplat, " +
                            " sum(sum_out) as Saldo_out, sum(proc_ot_oplat) as Proc " +
                            " from Inf_po_Raschet " +
                            " group by 1,2; "
                          );

                if (!ExecRead(con_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                if (reader2 != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader2, LoadOption.OverwriteChanges);
                }


                ////удаление nzp_kvar
                //dt.Columns.Remove("nzp_kvar");


                return dt;

                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetDT_SpravkaPoOtklKomUsl : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table Inf_po_Raschet; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (reader2 != null)
                {
                    reader2.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

        }

        /// <summary>
        /// Получить все данные из таблицы tXXX_saldoall,
        ///                                tXXX_saldoall_2 
        /// </summary>
        /// <param name="nzp_user">Номер пользователя</param>
        /// <returns>Объект DataTable</returns>
        public DataTable GetReportParamNach(string nzp_user, int mode)
        {
            Returns ret = Utils.InitReturns();

            DataTable dt = new DataTable();

            List<string> delCol = new List<string>();

            IDbConnection con_web = null;

            IDataReader reader = null;

            StringBuilder sql = new StringBuilder();


            #region Открытие соединения с БД
            con_web = GetConnection(Constants.cons_Webdata);

            ret = OpenDb(con_web, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            #region Условия и параметры запроса
            string tableName_saldoAll = "";
            string lsNumber = "";

            //выводить pkod10 или num_ls
            if (Points.IsSmr)
            {
                lsNumber = "pkod10";
            }
            else
            {
                lsNumber = "num_ls";
            }

            //мод
            switch (mode)
            {
                case 1:
                    {
                        //берем saldoAll
                        tableName_saldoAll = "t" + nzp_user + "_saldoall";
                        break;
                    }

                case 2:
                    {
                        tableName_saldoAll = "t" + nzp_user + "_saldoall_2";
                        break;
                    }
            }



            //флаг nzp_kvar
            bool f_nzp_kvar = false;
            //флаг nzp_dom
            bool f_nzp_dom = false;
            //флаг ordering
            bool f_ordering = false;

            //проверка на наличие перечисленных столбцов
#if PG
            string sqltemp = " select column_name as colname  from information_schema.columns " +
                             " where column_name in ('nzp_kvar', 'nzp_dom', 'ordering') " +
                             " and table_name = \'" + tableName_saldoAll + "\'" +
                             " and table_schema='public'";

#else
string sqltemp = "select  colname from syscolumns where colname in ('nzp_kvar','nzp_dom','ordering') and tabid  = (select tabid from systables where tabname =  \'" + tableName_saldoAll + "\')";
#endif
            
            
            if (!ExecRead(con_web, out reader, sqltemp, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                con_web.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
                    if (reader["colname"] != DBNull.Value)
                    {
                        string colname = reader["colname"].ToString().Trim();
                        switch (colname)
                        {
                            case "nzp_kvar":
                                {
                                    f_nzp_kvar = true;
                                    break;
                                }

                            case "nzp_dom":
                                {
                                    f_nzp_dom = true;
                                    break;
                                }

                            case "ordering":
                                {
                                    f_ordering = true;
                                    break;
                                }
                        }
                    }
                }
            }
            #endregion

            #region Получение информации
            string selectedColumns = "";
            string connectedTables = "";
            string connectedConditions = "";

            if (f_nzp_kvar)
            {
                selectedColumns = " spl." + lsNumber + ", spl.adr, s.* ";
#if PG
                connectedTables = ", " + "public" + " .  t" + nzp_user + "_spls spl ";
                connectedConditions = " where spl.nzp_kvar = s.nzp_kvar and spl.mark = 1 " +
                                      " order by spl.ulica,spl.idom,spl.ikvar,spl.pkod ";
#else
      connectedTables = "," + con_web.Database + " :  t" + nzp_user + "_spls spl ";
            connectedConditions = " where spl.nzp_kvar = s.nzp_kvar and spl.mark = 1 " +
                                  " order by spl.ulica,spl.idom,spl.ikvar,spl.pkod ";
#endif
                if (f_ordering)
                {
                    connectedConditions += ",ordering";
                }

            }
            else
            {
                if (f_nzp_dom)
                {
                    selectedColumns = " distinct trim(spl.ulica) || 'дом ' || spl.ndom as adr,spl.ulica, spl.idom, s.* ";
#if PG
                    connectedTables = "," + con_web.Database + " .  t" + nzp_user + "_spls spl ";
#else
 connectedTables = "," + con_web.Database + " :  t" + nzp_user + "_spls spl ";
#endif
                    connectedConditions = " where spl.nzp_dom = s.nzp_dom and spl.mark = 1 " +
                                            " order by spl.ulica,spl.idom ";
                    if (f_ordering)
                    {
                        connectedConditions += ",ordering";
                    }

                    //удаляемые столбцы
                    delCol.Add("ulica");
                    delCol.Add("idom");
                }
                else
                {
                    selectedColumns = " s.* ";
                    connectedTables = "";
                    connectedConditions = "order by no";
                    if (f_ordering)
                    {
                        connectedConditions += ",ordering";
                    }
                }
            }



            sql.Append(" Select " + selectedColumns);
            sql.Append(" From ");
#if PG
            sql.Append("public" + "." + tableName_saldoAll + " s ");
#else
  sql.Append(con_web.Database + ":" + tableName_saldoAll + " s ");
#endif
            sql.Append(connectedTables);
            sql.Append(connectedConditions);



            //sql.Append(" Select "); 
            //sql.Append(" from ");
            //sql.Append(" spl." + lsNumber + ", spl.adr, s.* ");
            //sql.Append(con_web.Database + " :  t" + nzp_user + "_spls spl, ");
            //sql.Append(" " + con_web.Database + ": " + tableName_saldoAll + " s ");
            //sql.Append(" where spl.nzp_kvar = s.nzp_kvar ");
            //sql.Append(" and spl.mark = 1 ");
            //sql.Append(" order by spl.ulica,spl.idom,spl.ikvar,spl.pkod ");


            //#region проверка на наличие 'ordering'            
            //string sqltemp = "select count(*) as count_ from syscolumns where colname = 'ordering' and tabid  = (select tabid from systables where tabname =  \'" + tableName_saldoAll + "\')";
            //if (!ExecRead(con_web, out reader, sqltemp, true).result)
            //{
            //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //    con_web.Close();
            //    sql.Remove(0, sql.Length);
            //    ret.result = false;
            //    return null;
            //}

            //if (reader != null)
            //{
            //    if (reader.Read())
            //    {
            //        if (reader["count_"] != DBNull.Value)
            //        {
            //            if (Convert.ToInt32(reader["count_"]) != 0)
            //            {
            //                sql.Append(" ,ordering;  ");
            //            }
            //        }
            //    }
            //}
            //#endregion

            if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                con_web.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }

            try
            {
                if (reader != null)
                {
                    #region Устанавливаем разделитель '.'
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    #endregion
                    dt.Load(reader, LoadOption.OverwriteChanges);


                    //удаление nzp_kvar
                    if (dt.Columns.Contains("nzp_kvar"))
                    {
                        dt.Columns.Remove("nzp_kvar");
                    }
                    //удаление ordering
                    if (dt.Columns.Contains("ordering"))
                    {
                        dt.Columns.Remove("ordering");
                    }
                    //удедение no
                    if (dt.Columns.Contains("no"))
                    {
                        dt.Columns.Remove("no");
                    }

                    //удаление nzp_supp
                    if (dt.Columns.Contains("nzp_supp"))
                    {
                        dt.Columns.Remove("nzp_supp");
                    }
                    //удаление nzp_serv
                    if (dt.Columns.Contains("nzp_serv"))
                    {
                        dt.Columns.Remove("nzp_serv");
                    }
                    //удаление nzp_dom
                    if (dt.Columns.Contains("nzp_dom"))
                    {
                        dt.Columns.Remove("nzp_dom");
                    }
                    //удаление nzp_geu
                    if (dt.Columns.Contains("nzp_geu"))
                    {
                        dt.Columns.Remove("nzp_geu");
                    }
                    //удаление nzp_area
                    if (dt.Columns.Contains("nzp_area"))
                    {
                        dt.Columns.Remove("nzp_area");
                    }

                    //удаление дополнительных столбцов
                    foreach (string col in delCol)
                    {
                        if (dt.Columns.Contains(col))
                        {
                            dt.Columns.Remove(col);
                        }
                    }

                    return dt;
                }

                return dt;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetReportParamNach : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Получить флаг ExcelIsInstalled
        /// </summary>
        /// <returns></returns>
        public bool GetFlagExcelIsInstalled(ref Returns ret)
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result)
            {
                ret.result = false;
                return false;
            }

#if PG
            if (!ExecSQL(connectionID, " set search_path to 'public'", true).result)
            {
                connectionID.Close();
                ret.result = false;
                return false;
            }
#else
            if (!ExecSQL(connectionID,
                  " set encryption password '" + BasePwd + "'"
                  , true).result)
            {
                connectionID.Close();
                ret.result = false;
                return false;
            }
#endif

            //SysPrtData
            IDataReader reader; //, readerImg;
            if (!ExecRead(connectionID, out reader,
                " Select num_prtd, decrypt_char(val_prtd) as val_prtd " +
                " From sysprtdata " +
                " where num_prtd = 33 ", true).result)
            {
                connectionID.Close();
                ret.result = false;
                return false;
            }
            try
            {
                int ExcelIsInstalled = -1;
                while (reader.Read())
                {
                    if (reader["val_prtd"] != DBNull.Value) Int32.TryParse(reader["val_prtd"].ToString(), out ExcelIsInstalled);

                }
                reader.Close();

                if (ExcelIsInstalled == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine("Exception: " + ex.Message);
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения SysPort " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                ret.result = false;
                return false;
            }
        }
    }
}
