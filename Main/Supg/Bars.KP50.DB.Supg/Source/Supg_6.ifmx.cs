using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Получить список нисправностей по конкретной услуге
        /// </summary>
        /// <param name="nzp_serv">услуга</param>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret">результат</param>
        /// <returns>список неисправностей</returns>
        public Dictionary<int, string> GetDest(int nzp_serv, int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDbConnection con_web = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            Dictionary<int, string> retDic = new Dictionary<int, string>();
            retDic.Add(-1, "<не выбрано>");

            //List<string> prefix = new List<string>();

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                // #region Определяем префиксы
#if PG
                //sql.Append(" select distinct  pref from  " + "public. t" + nzp_user + "_spls; ");
#else
                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + nzp_user + "_spls; ");
#endif
                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return null;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return null;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select nzp_dest, dest_name  ");
                sql.Append(" from   " + Points.Pref + "_supg. s_dest ");
                sql.Append(" where nzp_serv = " + nzp_serv);
#else
sql.Append(" select nzp_dest, dest_name  ");
                sql.Append(" from   " + Points.Pref + "_supg: s_dest ");
                sql.Append(" where nzp_serv = " + nzp_serv);
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при получения списка неисправностей " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        int nzp_dest = -1;
                        string dest_name = "";
                        if (reader["nzp_dest"] != DBNull.Value) nzp_dest = Convert.ToInt32(reader["nzp_dest"]);
                        if (reader["dest_name"] != DBNull.Value) dest_name = Convert.ToString(reader["dest_name"]);

                        retDic.Add(nzp_dest, dest_name);
                    }
                }
                //}

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDest : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

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

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить недопоставку(по услуге + неисправность)
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Недопоставка</returns>
        public Dest GetNedops(Dest finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDbConnection con_web = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            Dest retDest = new Dest();


            //List<string> prefix = new List<string>();

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                //#region Определяем префиксы
#if PG
                //sql.Append(" select distinct  pref from  " + "public. t" + finder.nzp_user + "_spls; ");
#else
                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + finder.nzp_user + "_spls; ");
#endif
                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return null;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return null;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select s.nzp_serv, s.service, ");
                sql.Append(" d.nzp_dest, ");
                sql.Append(" d.dest_name, d.term_days, d.term_hours, ");
                sql.Append(" u.name, u.is_param ");
                sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).services + " s, ");
                sql.Append(" " + Points.Pref + "_supg. s_dest d, ");
                sql.Append(" " + Points.Pref + "_data.  upg_s_kind_nedop u ");
                sql.Append(" where   s.nzp_serv = d.nzp_serv ");
                sql.Append(" and d.num_nedop = u.nzp_kind ");
                sql.Append(" and u.kod_kind = 1 ");
                //sql.Append(" and s.nzp_serv = " + finder.nzp_serv + " ");
                sql.Append(" and d.nzp_dest = " + finder.nzp_dest + " ");
#else
  sql.Append(" select s.nzp_serv, s.service, ");
                    sql.Append(" d.nzp_dest, ");
                    sql.Append(" d.dest_name, d.term_days, d.term_hours, ");
                    sql.Append(" u.name, u.is_param ");
                    sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).services + " s, ");
                    sql.Append(" " + Points.Pref + "_supg: s_dest d, ");
                    sql.Append(" " + Points.Pref + "_data:  upg_s_kind_nedop u ");
                    sql.Append(" where   s.nzp_serv = d.nzp_serv ");
                    sql.Append(" and d.num_nedop = u.nzp_kind ");
                    sql.Append(" and u.kod_kind = 1 ");
                    //sql.Append(" and s.nzp_serv = " + finder.nzp_serv + " ");
                    sql.Append(" and d.nzp_dest = " + finder.nzp_dest + " ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при получении недопоставки (по претензии и услуге) " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["nzp_serv"] != DBNull.Value) retDest.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["service"] != DBNull.Value) retDest.service = Convert.ToString(reader["service"]);
                        if (reader["nzp_dest"] != DBNull.Value) retDest.nzp_dest = Convert.ToInt32(reader["nzp_dest"]);
                        if (reader["dest_name"] != DBNull.Value) retDest.dest_name = Convert.ToString(reader["dest_name"]);
                        if (reader["term_days"] != DBNull.Value) retDest.term_days = Convert.ToInt32(reader["term_days"]);
                        if (reader["term_hours"] != DBNull.Value) retDest.term_hours = Convert.ToInt32(reader["term_hours"]);
                        if (reader["name"] != DBNull.Value) retDest.nedop_name = Convert.ToString(reader["name"]);
                        if (reader["is_param"] != DBNull.Value) retDest.is_param = Convert.ToInt32(reader["is_param"]);
                    }
                }
                //}

                return retDest;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNedops : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

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

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Получает библиотеку со списками для заполнения полей поиска
        /// </summary>
        /// <returns>Библиотека со списками для заполнения полей поиска</returns>
        public Dictionary<string, Dictionary<int, string>> GetSupgLists(SupgFinder finder, out Returns ret)
        {
            Dictionary<string, Dictionary<int, string>> List = new Dictionary<string, Dictionary<int, string>>();
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            //List<string> prefix = new List<string>();

            string supp = "";

            //if (finder.RolesVal != null)
            //{
            //    if (finder.RolesVal.Count > 0)
            //    {
            //        foreach (_RolesVal role in finder.RolesVal)
            //        {
            //            if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp)
            //                supp += " nzp_supp in (" + role.val + ")";
            //        }
            //    }
            //}

            #region роли

            BaseUser.OrganizationTypes types = finder.organization;

            switch (types)
            {
                case BaseUser.OrganizationTypes.Supplier:
                    {
                        supp = " nzp_supp = " + finder.nzp_supp;
                        break;
                    }
            }

            #endregion

            try
            {
                #region Открываем соединение с базой

                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в GetSupgLists", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                #region Классификация сообщения

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_ztype, zvk_type from " + Points.Pref + "_supg. s_zvktype");
#else
                sql.Append("Select nzp_ztype, zvk_type from " + Points.Pref + "_supg: s_zvktype");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_ztype"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_ztype"]);
                        if (reader["zvk_type"] != DBNull.Value) b = Convert.ToString(reader["zvk_type"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Классификация сообщения", temp_dict);
                }

                #endregion

                #region Результат заявки

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_res, res_name from " + Points.Pref + "_supg. s_result");
#else
 sql.Append("Select nzp_res, res_name from " + Points.Pref + "_supg: s_result");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_res"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_res"]);
                        if (reader["res_name"] != DBNull.Value) b = Convert.ToString(reader["res_name"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Результат заявки", temp_dict);
                }

                #endregion

                #region Службы переадресации

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_slug, slug_name from " + Points.Pref + "_supg. s_slug order by slug_name");
#else
sql.Append("Select nzp_slug, slug_name from " + Points.Pref + "_supg: s_slug order by slug_name");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_slug"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_slug"]);
                        if (reader["slug_name"] != DBNull.Value) b = Convert.ToString(reader["slug_name"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Службы переадресации", temp_dict);
                }

                #endregion

                #region Исполнители наряда заказа

                sql.Remove(0, sql.Length);
#if PG
                string sql_supp = "Select nzp_supp, name_supp from " + new DbTables(DBManager.getServer(con_db)).supplier;
                if (supp != "")
                {
                    sql_supp += " Where " + supp;
                }
                sql_supp += " Order by name_supp ";
#else
                string sql_supp = "Select nzp_supp, name_supp from " + new DbTables(DBManager.getServer(con_db)).supplier;
                if (supp != "")
                {
                    sql_supp += " Where " + supp;
                }
                sql_supp += " Order by name_supp ";
#endif

                sql.Append(sql_supp);

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_supp"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_supp"]);
                        if (reader["name_supp"] != DBNull.Value) b = Convert.ToString(reader["name_supp"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Исполнители наряда заказа", temp_dict);
                }

                #endregion

                #region Услуги наряда заказа

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_serv, Service From " + new DbTables(DBManager.getServer(con_db)).services + " where nzp_serv in (select nzp_serv from " + Points.Pref + "_supg. s_dest) Union Select 1000, '-' From " + Points.Pref + "_supg. s_dest Order by service");
#else
 sql.Append("Select nzp_serv, Service From " + new DbTables(DBManager.getServer(con_db)).services + " where nzp_serv in (select nzp_serv from " + Points.Pref + "_supg: s_dest) Union Select 1000, '-' From " + Points.Pref + "_supg: s_dest Order by service");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_serv"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["service"] != DBNull.Value) b = Convert.ToString(reader["service"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Услуги наряда заказа", temp_dict);
                }

                #endregion

                #region Подтверждение выполнения

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_atts, atts_name from " + Points.Pref + "_supg. s_attestation");
#else
sql.Append("Select nzp_atts, atts_name from " + Points.Pref + "_supg: s_attestation");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_atts"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_atts"]);
                        if (reader["atts_name"] != DBNull.Value) b = Convert.ToString(reader["atts_name"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Подтверждение выполнения", temp_dict);
                }

                #endregion

                #region Регистратура

                sql.Remove(0, sql.Length);

                if (finder.nzp_payer != finder.nzp_disp)
                {
#if PG
                    if (finder.nzp_area != 0)
                    {
                        sql.Append("select nzp_payer, payer from " + Points.Pref + "_kernel.s_payer where nzp_payer in (" + finder.nzp_disp + " , " + finder.nzp_payer + ")");
                    }
                    else
                    {
                        sql.Append(" select nzp_payer, payer " +
                                   " from " + Points.Pref + "_kernel.s_payer " +
                                   " where coalesce(nzp_supp, 0) <> 0");
                    }
#else
if (finder.nzp_area != 0)
                   {
                       sql.Append("select nzp_payer, payer from " + Points.Pref + "_kernel:s_payer where nzp_payer in (" + finder.nzp_disp + " , " + finder.nzp_payer + ")");
                   }
                   else
                   {
                     sql.Append(" select nzp_payer, payer "+
                                " from " + Points.Pref + "_kernel:s_payer " +
                                " where nvl(nzp_supp, 0) <> 0");
                   }
#endif
                }
                else
                {
#if PG
                    sql.Append("select nzp_payer, payer from " + Points.Pref + "_kernel.s_payer  where nzp_payer=" + finder.nzp_disp);
#else
                    sql.Append("select nzp_payer, payer from " + Points.Pref + "_kernel:s_payer  where nzp_payer=" + finder.nzp_disp);
#endif
                }

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    Dictionary<int, string> temp_dict = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        int a = 0;
                        string b = "";
                        if (reader["nzp_payer"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_payer"]);
                        if (reader["payer"] != DBNull.Value) b = Convert.ToString(reader["payer"]).Trim();
                        temp_dict.Add(a, b);
                    }
                    List.Add("Регистратура", temp_dict);
                }

                #endregion

                return List;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNedops : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }


                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Возвращает список претензий в зависимости от услуг
        /// </summary>
        /// <param name="nzp_serv"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Dest> GetDestName(int nzp_serv, out Returns ret)
        {
            List<Dest> DestName = new List<Dest>();
            Dest tt = new Dest();
            tt.dest_name = "<По всем претензиям>";
            tt.nzp_dest = 0;
            DestName.Add(tt);
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базой

                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в GetDestName", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                #region Претензии наряда заказа

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_dest, dest_name From " + Points.Pref + "_supg. s_dest Where nzp_serv = " + nzp_serv.ToString() + ";");
#else
                sql.Append("Select nzp_dest, dest_name From " + con_db.Database.Replace("kernel", "supg") + ": s_dest Where nzp_serv = " + nzp_serv.ToString() + ";");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    bool empty = true;
                    while (reader.Read())
                    {
                        empty = false;
                        Dest item = new Dest();
                        if (reader["dest_name"] != DBNull.Value) item.dest_name = Convert.ToString(reader["dest_name"]).Trim();
                        if (reader["nzp_dest"] != DBNull.Value) item.nzp_dest = Convert.ToInt32(reader["nzp_dest"]);
                        DestName.Add(item);
                    }
                    if (empty)
                    {
                        Dest item = new Dest();
                        item.dest_name = "Нет данных";
                        item.nzp_serv = -1;
                        DestName.Add(item);
                    }
                }


                #endregion

                return DestName;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDestName : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }


                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить поставщика
        /// </summary>
        /// <param name="nzp_kvar">nzp_kvar</param>
        /// <param name="nzp_user">nzp_user</param>
        /// <param name="nzp_serv">nzp_serv</param>
        /// <param name="act_date">act_date</param>
        /// <returns>поставщик</returns>
        //public string GetSupplier(int nzp_kvar, int nzp_user, int nzp_serv, string act_date, out Returns ret)
        public string GetSupplier(JobOrder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDbConnection con_web = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            string retStr = "";

            string pref;
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                // При работе с центральным БД извлекается сохраненная в таблице tarif информация
                pref = Points.Pref;
            }
            else
            {
                pref = finder.pref;
            }

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return "";
                }
                #endregion

                //проверка на доступность банка
#if PG
                if (!TempTableInWebCashe(con_db, pref + "_data.tarif"))
                {
                    MonitorLog.WriteLog("Получение поставщика для наряда-заказа. Банк данных " + pref + " недоступен", MonitorLog.typelog.Warn, true);
                    return "";
                }
#else
 if (!TempTableInWebCashe(con_db, pref + "_data:tarif"))
                {
                    MonitorLog.WriteLog("Получение поставщика для наряда-заказа: Банк данных " + pref + " недоступен", MonitorLog.typelog.Warn, true);
                    return "";
                }
#endif

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select distinct s.name_supp ");
                sql.Append(" from   " + new DbTables(DBManager.getServer(con_db)).supplier + " s, ");
                sql.Append(" " + pref + "_data.tarif t ");
                sql.Append(" where  s.nzp_supp=t.nzp_supp ");
                sql.Append(" and t.nzp_kvar = " + finder.nzp_kvar + " ");
                sql.Append(" and t.nzp_serv = " + finder.nzp_serv + " ");
                sql.Append(" and t.dat_s <= \'" + finder.dat_s_po + "\' ");
                sql.Append(" and t.dat_po >= \'" + finder.dat_s_po + "\' ");
                sql.Append(" and t.is_actual = 1 ");
#else
sql.Append(" select unique s.name_supp ");
                sql.Append(" from   " + new DbTables(DBManager.getServer(con_db)).supplier + " s, ");
                sql.Append(" " + pref + "_data@" + DBManager.getServer(con_db) + ":tarif t ");
                sql.Append(" where  s.nzp_supp=t.nzp_supp ");
                sql.Append(" and t.nzp_kvar = " + finder.nzp_kvar + " ");
                sql.Append(" and t.nzp_serv = " + finder.nzp_serv + " ");
                sql.Append(" and t.dat_s <= \'" + finder.dat_s_po + "\' ");
                sql.Append(" and t.dat_po >= \'" + finder.dat_s_po + "\' ");
                sql.Append(" and t.is_actual = 1 ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при получении поставщика " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return "";
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["name_supp"] != DBNull.Value) retStr = Convert.ToString(reader["name_supp"]);
                    }
                }

                return retStr;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNedops : " + ex.Message, MonitorLog.typelog.Error, true);
                return "";
            }
            finally
            {
                #region Закрытие соединений

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

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить всех поставщиков
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret">результат</param>
        /// <returns>список поставщиков</returns>
        public Dictionary<int, string> GetSuppliersAll(int nzp_user, int supp_filter, string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDbConnection con_web = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            Dictionary<int, string> retDic = new Dictionary<int, string>();
            retDic.Add(-1, "<не выбрано>");

            //List<string> prefix = new List<string>();

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                //#region Определяем префиксы
#if PG
                //sql.Append(" select distinct  pref from  public. t" + nzp_user + "_spls; ");
#else
                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + nzp_user + "_spls; ");
#endif
                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return null;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return null;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select nzp_supp, name_supp ");
                sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).supplier);
                sql.Append(" where ");
                if (supp_filter != 0)
                {
                    sql.Append(" nzp_supp =  " + supp_filter);
                }
                else
                {
                    sql.Append(" nzp_supp > 0 ");
                }
                sql.Append(" order by name_supp asc ");
#else
   sql.Append(" select nzp_supp, name_supp ");
                sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).supplier);
                sql.Append(" where ");
                if (supp_filter != 0)
                {
                    sql.Append(" nzp_supp =  " + supp_filter);
                }
                else
                {
                    sql.Append(" nzp_supp > 0 ");
                }
                sql.Append(" order by name_supp asc ");
#endif



                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при получении списка поставщиков " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        int nzp_supp = -1;
                        string name_supp = "";
                        if (reader["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                        if (reader["name_supp"] != DBNull.Value) name_supp = Convert.ToString(reader["name_supp"]);

                        retDic.Add(nzp_supp, name_supp);
                    }
                }
                //}

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSuppliersAll : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

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

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Добавить новый наряд-заказ
        /// </summary>
        /// <param name="finder">поисковик</param>
        /// <param name="ret">результат</param>
        /// <returns>результат</returns>
        public bool AddJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            int nzp_zk = 0;

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql.Remove(0, sql.Length);

                #region Получение локального юзера
                int local_user = finder.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/
                #endregion

                //определение номера
                //DbEditInterData dbEID = new DbEditInterData();
                nzp_zk = 0; // dbEID.GetSeriesProc(Points.Pref, 15, out ret);
                //dbEID.Close();
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка добавления наряда-заказа(определение номера) : " + ret.text, MonitorLog.typelog.Error, true);
                //    return  false;
                //}

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
#if PG
                sql.Append(" insert into " + Points.Pref + "_supg. zakaz ( nzp_zk,nzp_zvk, nzp_dest, nzp_supp, nzp_user, nzp_res, temperature, norm, is_replicate," +
                                                               " nzp_atts, repeated,order_date,exec_date, nedop_s, last_modified, nzp_payer )");
                sql.Append(" values(" + (nzp_zk != 0 ? nzp_zk.ToString() : "default") + ", " + finder.nzp_zvk + ", " + finder.nzp_dest + ", " + finder.nzp_supp + "," + local_user + ",5," + finder.temperature + ",0,0,1,0,\'" + 
                    Regex.Replace(finder.order_date, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\',\'" +
                    Regex.Replace(finder.exec_date, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\', \'" +
                    Regex.Replace(finder.nedop_s, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\', \'" +
                    Regex.Replace(sToday, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\'," + 
                    finder.nzp_payer + ");");
#else
sql.Append(" insert into "+Points.Pref+"_supg: zakaz ( nzp_zk,nzp_zvk, nzp_dest, nzp_supp, nzp_user, nzp_res, temperature, norm, is_replicate," +  
                                               " nzp_atts, repeated,order_date,exec_date, nedop_s, last_modified, nzp_payer )");
                sql.Append(" values(" + nzp_zk + ", " + finder.nzp_zvk + ", " + finder.nzp_dest + ", " + finder.nzp_supp + "," + local_user + ",5," + finder.temperature + ",0,0,1,0,\'" + finder.order_date + "\',\'" + finder.exec_date + "\', \'" + finder.nedop_s + "\', \'" + sToday + "\'," + finder.nzp_payer + ");");
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка добавления " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                nzp_zk = GetSerialValue(con_db);

                sql.Remove(0, sql.Length);
#if PG
                //sql.Append(" update " + pref + "_data . zakaz set norm = nzp_zk where nzp_zk = (select max(nzp_zk) from " + pref + "_data . zakaz where nzp_user = " + local_user + ") ");
                sql.Append(" update " + Points.Pref + "_supg . zakaz set norm = nzp_zk ");
                //sql.Append(" coalesce((select t.nzp_supp ");
                //sql.Append(" from " + pref + "_data . tarif t ");
                //sql.Append(" where  t.nzp_kvar = " + finder.nzp_kvar + " ");
                //sql.Append(" and t.nzp_serv = " + finder.nzp_serv + " ");
                //sql.Append(" and t.dat_s <= \'" + DateTime.Now.ToShortDateString() + "\' ");
                //sql.Append(" and t.dat_po >= \'" + DateTime.Now.ToShortDateString() + "\' ");
                //sql.Append(" ),1)) ");
                sql.Append(" where nzp_zk = " + nzp_zk);
#else
 //sql.Append(" update " + pref + "_data : zakaz set norm = nzp_zk where nzp_zk = (select max(nzp_zk) from " + pref + "_data : zakaz where nzp_user = " + local_user + ") ");
                sql.Append(" update " + Points.Pref + "_supg : zakaz set norm = nzp_zk ");
                //sql.Append(" nvl((select t.nzp_supp ");
                //sql.Append(" from " + pref + "_data : tarif t ");
                //sql.Append(" where  t.nzp_kvar = " + finder.nzp_kvar + " ");
                //sql.Append(" and t.nzp_serv = " + finder.nzp_serv + " ");
                //sql.Append(" and t.dat_s <= \'" + DateTime.Now.ToShortDateString() + "\' ");
                //sql.Append(" and t.dat_po >= \'" + DateTime.Now.ToShortDateString() + "\' ");
                //sql.Append(" ),1)) ");
                sql.Append(" where nzp_zk = " + nzp_zk);
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка добавления " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select z.nzp_zk, z.order_date ");
                sql.Append(" from " + Points.Pref + "_supg. zakaz z ");
                sql.Append(" where  z.nzp_zk = " + nzp_zk + " ");
#else
 sql.Append(" select z.nzp_zk, z.order_date ");
                sql.Append(" from " + Points.Pref+"_supg: zakaz z ");
                sql.Append(" where  z.nzp_zk = " + nzp_zk + " ");                
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения информации о новом наряд-заказе " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["nzp_zk"] != DBNull.Value) finder.nzp_zk = Convert.ToInt32(reader["nzp_zk"]);
                        if (reader["order_date"] != DBNull.Value) finder.order_date = Convert.ToString(reader["order_date"]);
                    }
                }
                return true;
            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddJobOrder : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }

            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить список наряд-заказов по заявке
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>список наряд-заказов</returns>
        public List<JobOrder> GetJobOrders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            List<JobOrder> retList = new List<JobOrder>();

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                //if (finder.RolesVal != null)
                //{
                //    foreach (_RolesVal role in finder.RolesVal)
                //    {
                //        if (role.tip == Constants.role_sql)
                //        {
                //            if (role.kod == Constants.role_sql_supp) filter += " and z.nzp_supp in (" + role.val + ") ";
                //        }
                //    }
                //}

                ////определение роли подрядчика                
                //Role finderRole = new Role();
                //finderRole.nzp_user = finder.nzp_user;
                //finderRole.nzp_role = Constants.roleUpgPodratchik;

                //DbAdmin db = new DbAdmin();
                //bool b = db.IsUserHasRole(finderRole, out ret);
                //db.Close();

                //if (b)
                //{
                //    if (ret.result)
                //    {
                //        filter += " and nzp_status <> 1 ";
                //    }
                //}
                //#endregion

                #region роли


                string filter5 = "";

                switch (finder.organization)
                {
                    case BaseUser.OrganizationTypes.DispatchingOffice:
                        {
                            filter5 = " and z.nzp_payer = " + finder.nzp_payer;
                            break;
                        }

                    case BaseUser.OrganizationTypes.Supplier:
                        {
                            filter5 += " and ((z.nzp_supp = " + finder.nzp_supp + " and nzp_status <> 1) or " + " z.nzp_payer = " + finder.nzp_payer + ") ";
                            //filter5 = " and z.nzp_payer = " + finder.nzp_payer;
                            break;
                        }

                    case BaseUser.OrganizationTypes.UK:
                        {
                            filter5 = " and z.nzp_payer in (" + finder.nzp_payer + "," + finder.nzp_payer_disp + ") ";
                            break;
                        }
                }
                #endregion

                //#region Определяем префиксы
#if PG
                //sql.Append(" select distinct  pref from  public. t" + finder.nzp_user + "_spls; ");
#else
                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + finder.nzp_user + "_spls; ");
#endif
                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return null;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return null;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select z.nzp_zk, z.order_date, z.nzp_res ,s.service, att.atts_name,z.replno "); //r.res_name,
                //результат из s_result
                sql.Append(" ,(select res_name from " + Points.Pref + "_supg. s_result where nzp_res = z.nzp_res) as res_name ");
                //результат из act - плановый ремонт (если есть)
                sql.Append(" ,(select wt.name_work_type || '№' || a.plan_number ");
                sql.Append(" from " + Points.Pref + "_supg . act a , " + Points.Pref + "_supg . s_work_type wt");
                sql.Append(" where a.nzp_work_type = wt.nzp_work_type ");
                sql.Append(" and a.nzp_act = z.nzp_plan_no");
                sql.Append(") as res_name_act ");
                sql.Append(" from   " + Points.Pref + "_supg . zakaz z, ");
                sql.Append(" " + Points.Pref + "_supg. s_dest d ");
                sql.Append(" left outer join " + new DbTables(DBManager.getServer(con_db)).services + " AS s ON d.nzp_serv = s.nzp_serv , ");
                //sql.Append(" " + Points.Pref + "_supg . s_result r, ");
                sql.Append(" " + Points.Pref + "_supg . s_attestation att ");
                sql.Append(" where  z.nzp_dest =  d.nzp_dest ");
                sql.Append(" and z.nzp_atts = att.nzp_atts ");
                sql.Append(" and z.nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(filter5);
                sql.Append(" order by z.norm, z.nzp_zk ");
#else
                sql.Append(" select z.nzp_zk, z.order_date, z.nzp_res ,s.service, att.atts_name,z.replno "); //r.res_name,
                //результат из s_result
                sql.Append(" ,(select res_name from " + Points.Pref + "_supg: s_result where nzp_res = z.nzp_res) as res_name ");
                //результат из act - плановый ремонт (если есть)
                sql.Append(" ,(select wt.name_work_type || '№' || a.plan_number ");
                sql.Append(" from " + Points.Pref + "_supg : act a , " + Points.Pref  + "_supg : s_work_type wt");
                sql.Append(" where a.nzp_work_type = wt.nzp_work_type ");
                sql.Append(" and a.nzp_act = z.nzp_plan_no");
                sql.Append(") as res_name_act ");
                sql.Append(" from   " + Points.Pref+"_supg : zakaz z, ");
                sql.Append(" " + Points.Pref + "_supg: s_dest d, ");
                sql.Append(" outer " + new DbTables(DBManager.getServer(con_db)).services + " s, ");
                //sql.Append(" " + Points.Pref + "_supg : s_result r, ");
                sql.Append(" " + Points.Pref + "_supg : s_attestation att ");
                sql.Append(" where  z.nzp_dest =  d.nzp_dest ");
                sql.Append(" and d.nzp_serv = s.nzp_serv ");
                sql.Append(" and z.nzp_atts = att.nzp_atts ");
                sql.Append(" and z.nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(filter5);
                sql.Append(" order by z.norm, z.nzp_zk ");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения списка наряд-заказов " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }


                if (reader != null)
                {
                    while (reader.Read())
                    {
                        JobOrder jo = new JobOrder();

                        if (reader["nzp_zk"] != DBNull.Value) jo.nzp_zk = Convert.ToInt32(reader["nzp_zk"]);
                        if (reader["order_date"] != DBNull.Value) jo.order_date = Convert.ToString(reader["order_date"]);
                        if (reader["nzp_res"] != DBNull.Value) jo.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                        if (reader["service"] != DBNull.Value) jo.service = Convert.ToString(reader["service"]);


                        //если есть инфа о плановом ремонте
                        if (reader["res_name_act"] != DBNull.Value)
                        {
                            jo.result = Convert.ToString(reader["res_name_act"]);
                        }
                        else if (reader["res_name"] != DBNull.Value) //тогда из s_result
                        {
                            jo.result = Convert.ToString(reader["res_name"]);
                        }


                        if (reader["atts_name"] != DBNull.Value) jo.atts = Convert.ToString(reader["atts_name"]);
                        if (reader["replno"] != DBNull.Value)
                        {
                            jo.replnoStr = Convert.ToString(reader["replno"]);
                        }
                        else
                        {
                            jo.replnoStr = "-";
                        }

                        retList.Add(jo);
                    }
                }
                //}
                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJobOrders : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// получить отображение наряд-заказа
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public JobOrder GetJobOrderForm(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            JobOrder jo = new JobOrder();

            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select z.nzp_zk , z.nzp_dest, z.exec_date, z.nedop_s, z.nzp_supp, z. temperature, z.order_date, z.nzp_answer, ");
                sql.Append(" z.plan_date, z.control_date, z.nzp_plan_no, z.nzp_payer, ");
                sql.Append(" d.dest_name,d.nzp_serv, u.nzp_kind ,sup.name_supp as ispolnit, z.fact_date,z.comment_n, z.nzp_res,  ");
                //результат из s_result
                sql.Append(" (select res_name from " + Points.Pref + "_supg. s_result where nzp_res = z.nzp_res) as res_name ");
                //результат из act - плановый ремонт (если есть)
                sql.Append(" ,(select wt.name_work_type || '№' || a.plan_number ");
                sql.Append(" from " + Points.Pref + "_supg . act a , " + Points.Pref + "_supg . s_work_type wt");
                sql.Append(" where a.nzp_work_type = wt.nzp_work_type ");
                sql.Append(" and a.nzp_act = z.nzp_plan_no");
                sql.Append(") as res_name_act, ");
                sql.Append(" (CASE WHEN  s.service is null then '-' else s.service end) as service, ");
                sql.Append(" (CASE WHEN  u.name is null then '-' else u.name end) as name, ");
                sql.Append(" z.nzp_atts, z.last_modified, z.is_replicate, z.parentno, z.replno, z.act_num_nedop, z.act_po, z.act_s, z.act_temperature, z.nzp_status ");
                sql.Append(" ,z.norm, z.act_actual, z.ds_actual, z.ds_date, ds_user, us.comment as ds_user_name ");
                sql.Append(" from " + Points.Pref + "_supg.zakaz z ");
                sql.Append(" left outer join " + Points.Pref + "_data.users us on us.nzp_user = z.ds_user, ");
                sql.Append(" " + Points.Pref + "_supg.s_dest d ");
                sql.Append(" left outer join " + new DbTables(DBManager.getServer(con_db)).services + " s on d.nzp_serv=s.nzp_serv ");
                sql.Append(" left outer join " + Points.Pref + "_data.upg_s_kind_nedop u on d.num_nedop = u.nzp_kind and u.kod_kind = 1 ,");
                sql.Append(", " + new DbTables(DBManager.getServer(con_db)).supplier + " sup ");
                sql.Append(" where z.nzp_dest = d.nzp_dest ");
                sql.Append(" and sup.nzp_supp = z.nzp_supp ");
                sql.Append(" and z.nzp_zk = " + finder.nzp_zk + "  ");


#else
                sql.Append(" select z.nzp_zk , z.nzp_dest, z.exec_date, z.nedop_s, z.nzp_supp, z. temperature, z.order_date, z.nzp_answer, ");
                sql.Append(" z.plan_date, z.control_date, z.nzp_plan_no, z.nzp_payer, ");
                sql.Append(" d.dest_name,d.nzp_serv, u.nzp_kind ,sup.name_supp as ispolnit, z.fact_date,z.comment_n, z.nzp_res,  ");
                //результат из s_result
                sql.Append(" (select res_name from " + Points.Pref + "_supg: s_result where nzp_res = z.nzp_res) as res_name ");
                //результат из act - плановый ремонт (если есть)
                sql.Append(" ,(select wt.name_work_type || '№' || a.plan_number ");
                sql.Append(" from " + Points.Pref + "_supg : act a , " + Points.Pref  + "_supg : s_work_type wt");
                sql.Append(" where a.nzp_work_type = wt.nzp_work_type ");
                sql.Append(" and a.nzp_act = z.nzp_plan_no");
                sql.Append(") as res_name_act, ");
                sql.Append(" (CASE WHEN  s.service is null then '-' else s.service end) as service, ");
                sql.Append(" (CASE WHEN  u.name is null then '-' else u.name end) as name, ");                
                sql.Append(" z.nzp_atts, z.last_modified, z.is_replicate, z.parentno, z.replno, z.act_num_nedop, z.act_po, z.act_s, z.act_temperature, z.nzp_status ");
                sql.Append(" ,z.norm, z.act_actual, z.ds_actual, z.ds_date, ds_user, us.comment as ds_user_name ");
                sql.Append(" from " + Points.Pref+"_supg:zakaz z, ");
                sql.Append(" " + Points.Pref + "_supg:s_dest d, ");
                sql.Append(" outer " + new DbTables(DBManager.getServer(con_db)).services + " s, ");
                sql.Append(" outer " + Points.Pref + "_data:upg_s_kind_nedop u");
                sql.Append(", " + new DbTables(DBManager.getServer(con_db)).supplier + " sup, ");
                //sql.Append(" " + Points.Pref + "_data: s_result r, ");
                sql.Append(" outer " + Points.Pref + "_data: users us  ");
                sql.Append(" where z.nzp_dest = d.nzp_dest ");
                sql.Append(" and d.nzp_serv = s.nzp_serv ");
                sql.Append(" and d.num_nedop = u.nzp_kind ");
                sql.Append(" and sup.nzp_supp = z.nzp_supp ");
                //sql.Append(" and r.nzp_res = z.nzp_res ");
                sql.Append(" and us.nzp_user = z.ds_user ");
                sql.Append("  and u.kod_kind = 1  ");
                sql.Append(" and z.nzp_zk = " + finder.nzp_zk + "  ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения выбранного наряд-заказа " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }


                if (reader != null)
                {
                    while (reader.Read())
                    {

                        if (reader["nzp_zk"] != DBNull.Value) jo.nzp_zk = Convert.ToInt32(reader["nzp_zk"]);
                        if (reader["nzp_dest"] != DBNull.Value) jo.nzp_dest = Convert.ToInt32(reader["nzp_dest"]);
                        if (reader["exec_date"] != DBNull.Value) jo.exec_date = Convert.ToString(reader["exec_date"]);
                        if (reader["nedop_s"] != DBNull.Value) jo.nedop_s = Convert.ToString(reader["nedop_s"]);
                        if (reader["nzp_supp"] != DBNull.Value) jo.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                        if (reader["dest_name"] != DBNull.Value) jo.dest_name = Convert.ToString(reader["dest_name"]);
                        if (reader["service"] != DBNull.Value) jo.service = Convert.ToString(reader["service"]);
                        if (reader["name"] != DBNull.Value) jo.name = Convert.ToString(reader["name"]);
                        if (reader["ispolnit"] != DBNull.Value) jo.ispolnit = Convert.ToString(reader["ispolnit"]);
                        if (reader["temperature"] != DBNull.Value) jo.temperature = Convert.ToInt32(reader["temperature"]);

                        if (reader["order_date"] != DBNull.Value) jo.order_date = Convert.ToString(reader["order_date"]);
                        if (reader["nzp_serv"] != DBNull.Value) jo.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                        if (reader["fact_date"] != DBNull.Value) jo.fact_date = Convert.ToString(reader["fact_date"]);



                        //если есть инфа о плановом ремонте
                        if (reader["res_name_act"] != DBNull.Value)
                        {
                            jo.res_name = Convert.ToString(reader["res_name_act"]);
                        }
                        else if (reader["res_name"] != DBNull.Value)
                        {
                            jo.res_name = Convert.ToString(reader["res_name"]);
                        }

                        if (reader["nzp_plan_no"] != DBNull.Value) jo.nzp_plan_no = Convert.ToInt32(reader["nzp_plan_no"]);

                        if (reader["comment_n"] != DBNull.Value) jo.comment_n = Convert.ToString(reader["comment_n"]);

                        if (reader["nzp_res"] != DBNull.Value) jo.nzp_res = Convert.ToInt32(reader["nzp_res"]);

                        if (reader["nzp_atts"] != DBNull.Value) jo.nzp_atts = Convert.ToInt32(reader["nzp_atts"]);

                        if (reader["last_modified"] != DBNull.Value) jo.last_modified = Convert.ToString(reader["last_modified"]);

                        if (reader["is_replicate"] != DBNull.Value) jo.is_replicate = Convert.ToInt32(reader["is_replicate"]);

                        if (reader["parentno"] != DBNull.Value) jo.parentno = Convert.ToInt32(reader["parentno"]);
                        if (reader["replno"] != DBNull.Value) jo.replno = Convert.ToInt32(reader["replno"]);

                        if (reader["nzp_serv"] != DBNull.Value) jo.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                        if (reader["nzp_kind"] != DBNull.Value) jo.nzp_kind = Convert.ToInt32(reader["nzp_kind"]);

                        if (reader["act_num_nedop"] != DBNull.Value) jo.act_num_nedop = Convert.ToInt32(reader["act_num_nedop"]);
                        if (reader["act_po"] != DBNull.Value) jo.act_po = Convert.ToString(reader["act_po"]);
                        if (reader["act_s"] != DBNull.Value) jo.act_s = Convert.ToString(reader["act_s"]);
                        if (reader["act_temperature"] != DBNull.Value) jo.act_temperature = Convert.ToInt32(reader["act_temperature"]);

                        if (reader["nzp_status"] != DBNull.Value) jo.status = Convert.ToInt32(reader["nzp_status"]);
                        if (reader["norm"] != DBNull.Value) jo.norm = Convert.ToInt32(reader["norm"]);

                        if (reader["act_actual"] != DBNull.Value) jo.act_actual = Convert.ToInt32(reader["act_actual"]);

                        if (reader["ds_actual"] != DBNull.Value) jo.ds_actual = Convert.ToInt32(reader["ds_actual"]);
                        if (reader["ds_date"] != DBNull.Value) jo.ds_date = Convert.ToString(reader["ds_date"]);
                        if (reader["ds_user"] != DBNull.Value) jo.ds_user = Convert.ToString(reader["ds_user"]);
                        if (reader["ds_user_name"] != DBNull.Value) jo.ds_user_name = Convert.ToString(reader["ds_user_name"]);
                        if (reader["nzp_answer"] != DBNull.Value) jo.nzp_answer = Convert.ToInt32(reader["nzp_answer"]);
                        if (reader["plan_date"] != DBNull.Value) jo.document_date = Convert.ToString(reader["plan_date"]);
                        if (reader["control_date"] != DBNull.Value) jo.document_controlDate = Convert.ToString(reader["control_date"]);
                        if (reader["nzp_payer"] != DBNull.Value) jo.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    }
                }

                //получение оператора
                #region Получение оператора

                sql = new StringBuilder();
#if PG
                sql.Append(" select u.nzp_user,u.comment ");
                sql.Append(" from " + Points.Pref + "_data.users u, ");
                sql.Append(" " + Points.Pref + "_supg. zakaz z ");
                sql.Append(" where u.nzp_user = z.nzp_user ");
                sql.Append(" and z.nzp_zk = " + finder.nzp_zk + " ");
#else
 sql.Append(" select u.nzp_user,u.comment " );
                sql.Append(" from " + Points.Pref + "_data:users u, ");
                sql.Append(" " + Points.Pref + "_supg: zakaz z ");
                sql.Append(" where u.nzp_user = z.nzp_user ");
                sql.Append(" and z.nzp_zk = " + finder.nzp_zk + " ");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения оператора для выбранного наряд-заказа " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["comment"] != DBNull.Value) jo.user_comment = Convert.ToString(reader["comment"]);
                    }
                }
                #endregion

                return jo;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJobOrderForm : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить список всех результатов на наряд-заказ
        /// </summary>
        /// <param name="finder">поисковик</param>
        /// <param name="ret"></param>
        /// <returns>Список результатов</returns>
        public Dictionary<int, string> GetJobOrderResultsAll(Finder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            Dictionary<int, string> retDic = new Dictionary<int, string>();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion



                sql.Remove(0, sql.Length);
#if PG
                sql.Append("select  nzp_res, res_name from " + Points.Pref + "_supg. s_result where res_type =1 ");
#else
                sql.Append("select  nzp_res, res_name from " + Points.Pref + "_supg: s_result where res_type =1 ");
#endif


                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }


                if (reader != null)
                {
                    while (reader.Read())
                    {

                        int nzp_res = -1;
                        string res_name = "";

                        if (reader["nzp_res"] != DBNull.Value) nzp_res = Convert.ToInt32(reader["nzp_res"]);
                        if (reader["res_name"] != DBNull.Value) res_name = Convert.ToString(reader["res_name"]);

                        retDic.Add(nzp_res, res_name);

                    }
                }

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJobOrderResultsAll : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }


                sql.Remove(0, sql.Length);

                #endregion
            }
        }

//        /// <summary>
//        /// получить долг по лицевому счету
//        /// </summary>
//        /// <param name="finder">pref, num</param>
//        /// <param name="ret"></param>
//        /// <returns></returns>
//        public decimal GetDolgLs(Ls finder, int dat_y, int dat_m, out Returns ret)
//        {

//            //if (GlobalSettings.WorkOnlyWithCentralBank)
//            //{
//            //    ret = new Returns(false, "Данные о долге по лицевому счету недоступны, т.к. установлен режим работы с центральным банком данных", -1);
//            //    return -1;
//            //} 

//            ret = Utils.InitReturns();
//            IDbConnection con_db = null;

//            StringBuilder sql = new StringBuilder();
//            IDataReader reader = null;

//            decimal dolg = 0;
//            decimal sum1 = 0;
//            decimal sum2 = 0;

//            string pref = finder.pref;

//            try
//            {
//                #region Открываем соединение с базами

//                con_db = GetConnection(Constants.cons_Database);


//                ret = OpenDb(con_db, true);

//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
//                    return -1;
//                }
//                #endregion

//                if (GlobalSettings.WorkOnlyWithCentralBank)
//                {
//                    //ret = new Returns(false, "Данные о долге по лицевому счету недоступны, т.к. установлен режим работы с центральным банком данных", -1);
//                    //return -1;

//                    sql.Remove(0, sql.Length);
//#if PG
//                    sql.Append(" select saldo_date, sum_debt  ");
//                    sql.Append(" from " + Points.Pref + "_supg.ls_saldo");
//                    sql.Append(" where nzp_kvar = " + finder.nzp_kvar);
//#else
//                    sql.Append(" select saldo_date, sum_debt  ");
//                    sql.Append(" from " + Points.Pref + "_supg:ls_saldo");
//                    sql.Append(" where nzp_kvar = " + finder.nzp_kvar );
//#endif
//                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
//                    {
//                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
//                        ret.result = false;
//                        return -1;
//                    }

//                    if (reader != null)
//                    {
//                        while (reader.Read())
//                        {
//                            if (reader["sum_debt"] != DBNull.Value) dolg = Convert.ToDecimal(reader["sum_debt"]);
//                            if (reader["saldo_date"] != DBNull.Value) ret.text = reader["saldo_date"].ToString();
//                        }
//                    }
//                }

//                else
//                {
//                    #region Обработка даты
//                    string y = "";
//                    string m = "";

//                    string dat_s = "";
//                    string dat_po = "";

//                    string m_past = "";
//                    string y_past = "";


//                    m = String.Format("{0:00}", dat_m);
//                    y = dat_y.ToString().Substring(2);

//                    DateTime d1;
//                    DateTime.TryParse("01." + m + "." + dat_y, out d1);
//                    if (d1 == DateTime.MinValue)
//                    {
//                        MonitorLog.WriteLog("Ошибка вычисления даты GetDolgLs", MonitorLog.typelog.Error, true);
//                        return -1;
//                    }
//                    //dat_s = "01." + m + "." + dat_y;
//                    //dat_po = DateTime.DaysInMonth(dat_y, dat_m) + "." + m + "." + dat_y;
//                    dat_s = "01." + String.Format("{0:00}", Points.CalcMonth.month_) + "." + Points.CalcMonth.year_;
//                    dat_po = DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) + "." + String.Format("{0:00}", Points.CalcMonth.month_) + "." + Points.CalcMonth.year_;


//                    DateTime d2 = d1.AddMonths(-1);
//                    m_past = String.Format("{0:00}", d2.Month);
//                    y_past = d2.Year.ToString().Substring(2);

//                    #endregion

//                    sql.Remove(0, sql.Length);

//#if PG
//                    sql.Append(" select sum (ch.sum_insaldo) as sum1  ");
//                    sql.Append(" from " + pref + "_charge_" + y_past + ".charge_" + m_past + " ch ");
//                    sql.Append(" where ch.num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
//                    sql.Append(" and ch.nzp_serv >1 ");
//                    sql.Append(" and ch.dat_charge is null ");
//#else
//                    sql.Append(" select sum (ch.sum_insaldo) as sum1  ");
//                    sql.Append(" from " + pref + "_charge_" + y_past + ":charge_" + m_past + " ch ");
//                    sql.Append(" where ch.num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
//                    sql.Append(" and ch.nzp_serv >1 ");
//                    sql.Append(" and ch.dat_charge is null ");
//#endif



//                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
//                    {
//                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
//                        ret.result = false;
//                        return -1;
//                    }


//                    if (reader != null)
//                    {
//                        while (reader.Read())
//                        {
//                            if (reader["sum1"] != DBNull.Value) sum1 = Convert.ToDecimal(reader["sum1"]);
//                        }
//                    }

//                    sql.Remove(0, sql.Length);
//#if PG
//                    sql.Append(" select sum(g_sum_ls) as sum2   ");
//                    sql.Append(" from " + pref + "_fin_" + y + ".pack_ls ");
//                    sql.Append(" where num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
//                    //sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
//                    sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
//#else
//                    sql.Append(" select sum(g_sum_ls) as sum2   ");
//                    sql.Append(" from " + pref + "_fin_" + y + ":pack_ls ");
//                    sql.Append(" where num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
//                    //sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
//                    sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
//#endif
//                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
//                    {
//                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
//                        ret.result = false;
//                        return -1;
//                    }


//                    if (reader != null)
//                    {
//                        while (reader.Read())
//                        {
//                            if (reader["sum2"] != DBNull.Value) sum2 = Convert.ToDecimal(reader["sum2"]);
//                        }
//                    }

//                    dolg = sum1 - sum2;
//                }

//                return dolg;
//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDolgLs : " + ex.Message, MonitorLog.typelog.Error, true);
//                return -1;
//            }
//            finally
//            {
//                #region Закрытие соединений

//                if (con_db != null)
//                {
//                    con_db.Close();
//                }


//                sql.Remove(0, sql.Length);

//                #endregion
//            }
//        }

    }
}
