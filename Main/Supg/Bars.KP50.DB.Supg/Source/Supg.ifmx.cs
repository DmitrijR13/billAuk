using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Процедура экранирующая спец символы
        /// </summary>
        /// <param name="str">входная строка</param>
        /// <returns>выходная строка</returns>
        public string SymbolsScreening(string str)
        {
            #region Экранирование символов
            str = str.Replace("\n", " ");
            str = str.Replace("\'", "\"");
            str = str.Replace("\r", " ");
            #endregion

            return str;
        }



        /// <summary>
        /// процедура добавления заявки в базу данных
        /// </summary>
        /// <returns>bool</returns>
        public int Add_Order(OrderContainer Container, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
            int nzp_zvk = 0;
            string pref = Container.pref;

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
                    return 0;
                }
                #endregion

                sql.Remove(0, sql.Length);

                #region Получение локального юзера
                int local_user = Container.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = Container.nzp_user, pref = pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                dbU.Close();*/
                #endregion

                //определение номера
                //DbEditInterData dbEID = new DbEditInterData();
                nzp_zvk = 0; // dbEID.GetSeriesProc(Points.Pref, 14, out ret);
                //dbEID.Close();
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка добавления заявки(определение номера) : " + ret.text, MonitorLog.typelog.Error, true);
                //    return -1;
                //}

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
                string sToday_sec = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
                string sToday_sec = Utils.GetSupgCurDate("T", "s");
#endif
#if PG
                sql.Append(" Insert Into " + Points.Pref + "_supg. zvk (nzp_zvk, zvk_date, nzp_kvar, nzp_user, demand_name, nzp_ztype, comment,  phone, nzp_res, last_modified, nzp_payer) " +
                                           " Values (DEFAULT,\'" + sToday_sec + "\', " + Container.nzp_kvar + ", " + local_user + ", \'" +
                                                    Container.demand_name + "\', " + Container.nzp_ztype + ", :comment, \'" + Container.phone + "\'," + Container.nzp_res + ", \'" + sToday + "\'," + Container.nzp_payer + ")");
#else
sql.Append(" Insert Into " + Points.Pref + "_supg: zvk (nzp_zvk, zvk_date, nzp_kvar, nzp_user, demand_name, nzp_ztype, comment,  phone, nzp_res, last_modified, nzp_payer) " +
                           " Values (" + nzp_zvk + ",\'" + sToday_sec + "\', " + Container.nzp_kvar + ", " + local_user + ", \'" +
                                    Container.demand_name + "\', " + Container.nzp_ztype + ", ?, \'" + Container.phone + "\'," + Container.nzp_res + ", \'" +sToday+ "\'," + Container.nzp_payer + ")");
#endif
                IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), con_db);
#if PG
                DBManager.addDbCommandParameter(cmd, "comment", DbType.String, Container.comment);
#else
                DBManager.addDbCommandParameter(cmd, "@BinaryValue", DbType.String, Container.comment);
#endif
                cmd.ExecuteNonQuery();
                nzp_zvk = GetSerialValue(con_db);

#if PG
                //sql.Remove(0, sql.Length);
                //sql.Append("Select MAX(nzp_zvk) as index From " + pref + "_data. zvk");
                //if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return 0;
                //}
#else
//sql.Remove(0, sql.Length);
                //sql.Append("Select MAX(nzp_zvk) as index From " + pref + "_data: zvk");
                //if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return 0;
                //}
#endif

                //while (reader.Read())
                //{
                //    if (reader["index"] != DBNull.Value)
                //    {
                //        nzp_zvk = Convert.ToInt32(reader["index"]);
                //    }
                //    else
                //    {
                //        nzp_zvk = 1;
                //    }
                //}                

                return nzp_zvk;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Add_Order : " + ex.Message, MonitorLog.typelog.Error, true);
                return 0;
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
        /// возвращает список сообщений для лицевого счета
        /// </summary>
        /// <returns></returns>
        public List<OrderContainer> Find_Orders(OrderContainer Container, out Returns ret)
        {
            List<OrderContainer> Data = new List<OrderContainer>();
            //DataTable Orders = new DataTable ();
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

            // Если пустой адрес, вернуть пустой список (если только не происходит выборка конкретной заявки)
            if (Container.nzp_kvar == Convert.ToInt32(Constants.NzpEmptyAddress))
            {
                if (Container.nzp_zvk == 0) return null;
            }

            string pref = Container.pref;
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
                //sql.Append(" select distinct  pref from  public. t" + Container.nzp_user + "_spls; ");
#else
                    //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + Container.nzp_user + "_spls; ");
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

                #region фильтр по поставщикам
                //string filter = "";
                //string filter2 = "";
                //string filter3 = "";
                //string filter4 = "";                
                //if (Container.RolesVal != null)
                //    foreach (_RolesVal role in Container.RolesVal)
                //    {
                //        if (role.tip == Constants.role_sql)
                //        {
                //            if (role.kod == Constants.role_sql_supp)
                //            {
#if PG
                //                  filter2 = " ," + pref + "_data. zakaz zz";
                //                filter3 = "and zz.nzp_zvk = z.nzp_zvk";
                //                filter += " and zz.nzp_supp in (" + role.val + ") ";
#else
                //                  filter2 = " ," + pref + "_data: zakaz zz";
                //                filter3 = "and zz.nzp_zvk = z.nzp_zvk";
                //                filter += " and zz.nzp_supp in (" + role.val + ") ";
#endif
                //            }
                //        }
                //    }
                //if (Container.nzp_zvk != 0) filter4 = " and z.nzp_zvk = " + Container.nzp_zvk.ToString();
                string filter = "";
                string filter4 = "";


                if (Container.nzp_zvk != 0) filter4 = " and z.nzp_zvk = " + Container.nzp_zvk.ToString();
                #endregion

                #region Фильтр по организациям
                string filter5 = "";
                switch (Container.organization)
                {
                    case BaseUser.OrganizationTypes.DispatchingOffice:
                        {
                            filter5 = " and z.nzp_payer = " + Container.nzp_payer;
                            break;
                        }

                    case BaseUser.OrganizationTypes.Supplier:
                        {

#if PG
                            //filter2 = " ," + Points.Pref + "_supg. zakaz zz";
                            //filter3 = "and zz.nzp_zvk = z.nzp_zvk";
                            //filter += " and (zz.nzp_supp = " + Container.nzp_supp + " or " + " z.nzp_payer = " + Container.nzp_payer + ") ";
                            filter += " and (z.nzp_payer = " + Container.nzp_payer + " or (z.nzp_zvk in (select nzp_zvk from " + Points.Pref + "_supg.zakaz where nzp_supp = " + Container.nzp_supp + ")))";
                            //filter5 = " and z.nzp_payer = " + Container.nzp_payer;
#else
                            //filter2 = " ," + Points.Pref + "_supg: zakaz zz";
                            //filter3 = "and zz.nzp_zvk = z.nzp_zvk";
                            //filter += " and (zz.nzp_supp = " + Container.nzp_supp + " or " + " z.nzp_payer = " + Container.nzp_payer + ") ";
                            filter += " and (z.nzp_payer = " + Container.nzp_payer + " or (z.nzp_zvk in (select nzp_zvk from " + Points.Pref + "_supg:zakaz where nzp_supp = " + Container.nzp_supp + ")))";
                            //filter5 = " and z.nzp_payer = " + Container.nzp_payer;
#endif
                            break;
                        }

                    case BaseUser.OrganizationTypes.UK:
                        {
                            filter5 = " and z.nzp_payer in (" + Container.nzp_payer + "," + Container.nzp_payer_disp + ") ";
                            break;
                        }
                }
                #endregion

                // foreach (string pref in prefix)
                //{
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  Select z.nzp_zvk, z.zvk_date, t.nzp_ztype, z.comment,  z.phone,z.demand_name , z.nzp_payer , t.zvk_type, r.res_name,z.result_comment   ");
                sql.Append(" From " + Points.Pref + "_supg. zvk z, ");
                sql.Append("  " + Points.Pref + "_supg. s_result r, ");
                sql.Append("  " + Points.Pref + "_supg. s_zvktype t ");
                //sql.Append(filter2);
                sql.Append(" Where  z.nzp_ztype = t.nzp_ztype ");
                sql.Append(" and z.nzp_res = r.nzp_res ");
                if (Container.nzp_kvar != 0)
                    sql.Append(" and z.nzp_kvar = " + Container.nzp_kvar);
                //sql.Append(filter3);
                sql.Append(filter);
                sql.Append(filter4);
                sql.Append(filter5);
                sql.Append(" order by z.zvk_date DESC ");
#else
 sql.Append("  Select z.nzp_zvk, z.zvk_date, t.nzp_ztype, z.comment,  z.phone,z.demand_name , z.nzp_payer , t.zvk_type, r.res_name,z.result_comment   ");
                sql.Append(" From " + Points.Pref + "_supg: zvk z, ");                    
                sql.Append("  " + Points.Pref + "_supg: s_result r, ");
                sql.Append("  " + Points.Pref + "_supg: s_zvktype t ");
                //sql.Append(filter2);
                sql.Append(" Where  z.nzp_ztype = t.nzp_ztype ");
                sql.Append(" and z.nzp_res = r.nzp_res ");
                if (Container.nzp_kvar != 0)
                sql.Append(" and z.nzp_kvar = " + Container.nzp_kvar);
                //sql.Append(filter3);
                sql.Append(filter);
                sql.Append(filter4);
                sql.Append(filter5);
                sql.Append(" order by z.zvk_date DESC ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                //}

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        OrderContainer item = new OrderContainer();
                        if (reader["nzp_zvk"] != DBNull.Value) item.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        if (reader["zvk_date"] != DBNull.Value) item.zvk_date = reader["zvk_date"].ToString();
                        if (reader["nzp_ztype"] != DBNull.Value) item.nzp_ztype = Convert.ToInt32(reader["nzp_ztype"]);

                        if (reader["zvk_type"] != DBNull.Value) item.zvk_type = reader["zvk_type"].ToString();
                        if (reader["res_name"] != DBNull.Value) item.res_name = reader["res_name"].ToString();

                        if (reader["comment"] != DBNull.Value) item.comment = reader["comment"].ToString();
                        if (reader["phone"] != DBNull.Value) item.phone = reader["phone"].ToString();
                        if (reader["demand_name"] != DBNull.Value) item.demand_name = Convert.ToString(reader["demand_name"]);
                        if (reader["nzp_payer"] != DBNull.Value) item.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                        if (reader["result_comment"] != DBNull.Value) item.result_comment = Convert.ToString(reader["result_comment"]);
                        Data.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Find_Orders : " + ex.Message, MonitorLog.typelog.Error, true);
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
            return Data;
        }

        /// <summary>
        /// Возвращает всю информацию(ДОПИСЫВАЕТСЯ ПРИ НЕОБХОДИМОСТИ!) о конкретном заявлении
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат поиска</param>
        /// <returns>Объект заявление</returns>
        public OrderContainer Find_Orders_One(OrderContainer finder, out Returns ret)
        {
            OrderContainer Order = new OrderContainer();
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
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
#if PG
                sql.Remove(0, sql.Length);
                sql.Append("  Select z.nzp_zvk, z.zvk_date, t.nzp_ztype, z.comment,  z.phone , t.zvk_type, r.res_name   ");
                sql.Append(" , z.exec_date, z.fact_date, z.last_modified, z.nzp_res, u.comment as user_name, z.nzp_kvar, z.result_comment ");
                sql.Append(" , z.demand_name, z.nzp_res ");
                sql.Append(" From " + Points.Pref + "_supg. zvk z, ");
                sql.Append("  " + Points.Pref + "_supg. s_result r, ");
                sql.Append("  " + Points.Pref + "_supg. s_zvktype t, ");
                sql.Append(" " + Points.Pref + "_data.users u ");
                sql.Append(" Where  z.nzp_ztype = t.nzp_ztype ");
                sql.Append(" and z.nzp_res = r.nzp_res ");
                sql.Append(" and u.nzp_user = z.nzp_user");
                //sql.Append(" and z.nzp_kvar = " + finder.nzp_kvar);
                sql.Append(" and z.nzp_zvk = " + finder.nzp_zvk);
#else
sql.Remove(0, sql.Length);
                sql.Append("  Select z.nzp_zvk, z.zvk_date, t.nzp_ztype, z.comment,  z.phone , t.zvk_type, r.res_name   ");
                sql.Append(" , z.exec_date, z.fact_date, z.last_modified, z.nzp_res, u.comment as user_name, z.nzp_kvar, z.result_comment ");
                sql.Append(" , z.demand_name, z.nzp_res ");
                sql.Append(" From " + Points.Pref + "_supg: zvk z, ");
                sql.Append("  " + Points.Pref + "_supg: s_result r, ");
                sql.Append("  " + Points.Pref + "_supg: s_zvktype t, ");
                sql.Append(" " + Points.Pref + "_data:users u ");
                sql.Append(" Where  z.nzp_ztype = t.nzp_ztype ");
                sql.Append(" and z.nzp_res = r.nzp_res ");
                sql.Append(" and u.nzp_user = z.nzp_user");
                //sql.Append(" and z.nzp_kvar = " + finder.nzp_kvar);
                sql.Append(" and z.nzp_zvk = " + finder.nzp_zvk);
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                //}                

                if (reader != null)
                {

                    if (reader.Read())
                    {

                        if (reader["nzp_zvk"] != DBNull.Value) Order.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        if (reader["zvk_date"] != DBNull.Value) Order.zvk_date = reader["zvk_date"].ToString();
                        if (reader["nzp_ztype"] != DBNull.Value) Order.nzp_ztype = Convert.ToInt32(reader["nzp_ztype"]);

                        if (reader["zvk_type"] != DBNull.Value) Order.zvk_type = reader["zvk_type"].ToString();
                        if (reader["res_name"] != DBNull.Value) Order.res_name = reader["res_name"].ToString();

                        if (reader["comment"] != DBNull.Value) Order.comment = reader["comment"].ToString();
                        if (reader["phone"] != DBNull.Value) Order.phone = reader["phone"].ToString();


                        if (reader["exec_date"] != DBNull.Value) Order.exec_date = reader["exec_date"].ToString();
                        if (reader["fact_date"] != DBNull.Value) Order.fact_date = reader["fact_date"].ToString();
                        if (reader["last_modified"] != DBNull.Value) Order.last_modified = reader["last_modified"].ToString();

                        if (reader["user_name"] != DBNull.Value) Order.user_name = reader["user_name"].ToString();

                        if (reader["nzp_kvar"] != DBNull.Value) Order.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

                        if (reader["result_comment"] != DBNull.Value) Order.result_comment = Convert.ToString(reader["result_comment"]);
                        if (reader["demand_name"] != DBNull.Value) Order.demand_name = Convert.ToString(reader["demand_name"]);

                        if (reader["nzp_res"] != DBNull.Value) Order.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Find_Orders : " + ex.Message, MonitorLog.typelog.Error, true);
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
            return Order;
        }


        /// <summary>
        /// возвращает Справочник Тематика из базы
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> TematicaLib(OrderContainer Container, out Returns ret)
        {
            Dictionary<int, string> TematicaLib = new Dictionary<int, string>();
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
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
                //sql.Append(" select distinct  pref from  public. t" + Container.nzp_user + "_spls; ");
#else
                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + Container.nzp_user + "_spls; ");
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
                //reader.Close();
                //#endregion

                //foreach (string pref in prefix)
                //{
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_ztype, zvk_type From " + Points.Pref + "_supg. s_zvktype ");
#else
                    sql.Append("Select nzp_ztype, zvk_type From " + Points.Pref + "_supg: s_zvktype ");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                //}

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        TematicaLib.Add(reader.GetInt32(0), reader.GetString(1));
                    }
                    //TematicaLib.Load(reader, LoadOption.OverwriteChanges);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Find_Orders : " + ex.Message, MonitorLog.typelog.Error, true);
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
            return TematicaLib;
        }

        /// <summary>
        /// Получает список служб из справочника s_slug.
        /// </summary>
        /// <param name="Container">Объект поиска</param>
        /// <param name="ret">результат выполнения</param>
        /// <returns>Список услуг</returns>
        public List<ServiceForwarding> GetServices(OrderContainer Container, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            List<ServiceForwarding> listServices = new List<ServiceForwarding>();
            StringBuilder sql = new StringBuilder();
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
                sql.Append(" Select  nzp_slug, slug_name, phone, dat_s, dat_po ");
                sql.Append(" From   " + Points.Pref + "_supg.  s_slug ");
                if (Container.nzp_zvk != 0)
                {
                    string sToday = Utils.GetSupgCurDate("D", "");  // только те службы, которые действуют на данный момент
                    sql.Append(" where \'" + sToday + "\' between coalesce(dat_s,public.mdy(1,1,1900)) and coalesce(dat_po, public.mdy(1,1,4000))");
                    sql.Append(" or nzp_slug in (select nzp_slug from " + Points.Pref + "_supg. readdress where nzp_zvk=" + Container.nzp_zvk.ToString() + ")");
                }
#else
 sql.Append(" Select  nzp_slug, slug_name, phone, dat_s, dat_po ");
                sql.Append(" From   " + Points.Pref + "_supg:  s_slug ");
                if (Container.nzp_zvk != 0)
                {
                    string sToday = Utils.GetSupgCurDate("D", "");  // только те службы, которые действуют на данный момент
                    sql.Append(" where \'" + sToday + "\' between nvl(dat_s,mdy(1,1,1900)) and nvl(dat_po, mdy(1,1,4000))");
                    sql.Append(" or nzp_slug in (select nzp_slug from " + Points.Pref + "_supg: readdress where nzp_zvk=" + Container.nzp_zvk.ToString() + ")");
                }
#endif
                sql.Append(" order by slug_name ");

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        ServiceForwarding service = new ServiceForwarding();

                        if (reader["nzp_slug"] != DBNull.Value) service.nzp_slug = Convert.ToInt32(reader["nzp_slug"]);
                        if (reader["slug_name"] != DBNull.Value) service.slug_name = Convert.ToString(reader["slug_name"]);
                        if (reader["phone"] != DBNull.Value) service.phone = Convert.ToString(reader["phone"]);
                        if (reader["dat_s"] != DBNull.Value) service.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                        if (reader["dat_po"] != DBNull.Value) service.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();

                        listServices.Add(service);
                    }
                }

                return listServices;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Find_Orders : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Добавление переадресации
        /// </summary>
        /// <param name="SericeForv">Объект добавления</param>
        /// <param name="ret">результат выполнения</param>
        /// <returns>bool</returns>
        public bool AddReaddress(ServiceForwarding SericeForv, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();
            string pref = SericeForv.pref;

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

                //#region Определяем префиксы

#if PG
                //sql.Append(" select distinct  pref from  public. t" + SericeForv.nzp_user + "_spls; ");
#else
//sql.Append(" select unique  pref from  " + con_web.Database + ": t" + SericeForv.nzp_user + "_spls; ");
#endif
                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return false;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{
                sql.Remove(0, sql.Length);

                #region Получение локального юзера
                int local_user = SericeForv.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = SericeForv.nzp_user, pref = pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/
                #endregion

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "s");
#endif
#if PG
                sql.Append(" Insert Into " + Points.Pref + "_supg. readdress (nzp_zvk, nzp_slug, nzp_user, _date, comment) " +
                                          " Values (" + SericeForv.nzp_zvk + ", " + SericeForv.nzp_slug + ", " + local_user + ", " +
                                                    "\'" + sToday + "\', \'" + SericeForv.comment + "\')");
                //SericeForv._date + "\', \'" + SericeForv.comment + "\')");
#else
 sql.Append(" Insert Into " + Points.Pref + "_supg: readdress (nzp_zvk, nzp_slug, nzp_user, _date, comment) " +
                           " Values (" + SericeForv.nzp_zvk + ", " + SericeForv.nzp_slug + ", " + local_user + ", "+
                                     "\'" + sToday + "\', \'" + SericeForv.comment + "\')");
                                    //SericeForv._date + "\', \'" + SericeForv.comment + "\')");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при добавлении переадресации " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                //}
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddReaddress : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить спискок переалресаций на конкретное сообщение
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>список переадресаций</returns>
        public List<ServiceForwarding> GetReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

            List<ServiceForwarding> listReadress = new List<ServiceForwarding>();

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
                sql.Append(" select  r.nzp_readdr, r._date, s.slug_name,s.phone,u.comment as user_name,r.comment, r.result_comment ");
                sql.Append(" from " + Points.Pref + "_supg" + " . readdress r, ");
                sql.Append(" " + Points.Pref + "_supg.s_slug s, ");
                sql.Append(" " + Points.Pref + "_data.users u ");
                sql.Append(" where r. nzp_slug =  s. nzp_slug ");
                sql.Append(" and r.nzp_user = u.nzp_user ");
                sql.Append(" and r.nzp_zvk = " + finder.nzp_zvk);
#else
sql.Append(" select  r.nzp_readdr, r._date, s.slug_name,s.phone,u.comment as user_name,r.comment, r.result_comment ");
                sql.Append(" from " + Points.Pref + "_supg" + " : readdress r, ");
                sql.Append(" " + Points.Pref + "_supg:  s_slug s, ");
                sql.Append(" " + Points.Pref + "_data:users u ");
                sql.Append(" where r. nzp_slug =  s. nzp_slug ");
                sql.Append(" and r.nzp_user = u.nzp_user ");
                sql.Append(" and r.nzp_zvk = " + finder.nzp_zvk);
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при добавлении переадресации " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        ServiceForwarding service = new ServiceForwarding();

                        if (reader["nzp_readdr"] != DBNull.Value) service.nzp_readdr = Convert.ToInt32(reader["nzp_readdr"]);
                        if (reader["_date"] != DBNull.Value) service._date = Convert.ToString(reader["_date"]);
                        if (reader["slug_name"] != DBNull.Value) service.slug_name = Convert.ToString(reader["slug_name"]);
                        if (reader["phone"] != DBNull.Value) service.phone = Convert.ToString(reader["phone"]);
                        if (reader["user_name"] != DBNull.Value) service.user_name = Convert.ToString(reader["user_name"]);
                        if (reader["comment"] != DBNull.Value) service.comment = Convert.ToString(reader["comment"]);
                        if (reader["result_comment"] != DBNull.Value) service.result_comment = Convert.ToString(reader["result_comment"]);

                        listReadress.Add(service);
                    }
                }
                //}

                return listReadress;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetReadress : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// получение информации о переадресации по конкретной заявке
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Информация о переадресации</returns>
        public ServiceForwarding GetServiceForward_One(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

            ServiceForwarding service = null;

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
#if PG
                //sql.Remove(0, sql.Length);
                //sql.Append(" select  nzp_readdr, nzp_zvk, nzp_slug, nzp_user, _date, comment, result_comment ");
                //sql.Append(" from " + pref + "_data .  readdress ");
                //sql.Append(" where  nzp_readdr =" + finder.nzp_readdr + " ");
                //sql.Append(" and  nzp_zvk =" + finder.nzp_zvk + " ");
#else
//sql.Remove(0, sql.Length);
                    //sql.Append(" select  nzp_readdr, nzp_zvk, nzp_slug, nzp_user, _date, comment, result_comment ");
                    //sql.Append(" from " + pref + "_data :  readdress ");
                    //sql.Append(" where  nzp_readdr =" + finder.nzp_readdr + " ");
                    //sql.Append(" and  nzp_zvk =" + finder.nzp_zvk + " ");
#endif

#if PG
                sql.Remove(0, sql.Length);
                sql.Append(" select  r.nzp_readdr, r._date, s.slug_name,s.phone,u.comment as user_name,r.comment, r.result_comment, r.nzp_slug ");
                sql.Append(" from " + Points.Pref + "_supg" + " . readdress r, ");
                sql.Append(" " + Points.Pref + "_supg.  s_slug s, ");
                sql.Append(" " + Points.Pref + "_data.users u ");
                sql.Append(" where r. nzp_slug =  s. nzp_slug ");
                sql.Append(" and r.nzp_user = u.nzp_user ");
                sql.Append(" and r.nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(" and r.nzp_readdr =" + finder.nzp_readdr + " ");
#else
  sql.Remove(0, sql.Length);
                sql.Append(" select  r.nzp_readdr, r._date, s.slug_name,s.phone,u.comment as user_name,r.comment, r.result_comment, r.nzp_slug ");
                sql.Append(" from " + Points.Pref + "_supg" + " : readdress r, ");
                sql.Append(" " + Points.Pref + "_supg:  s_slug s, ");
                sql.Append(" " + Points.Pref + "_data:users u ");
                sql.Append(" where r. nzp_slug =  s. nzp_slug ");
                sql.Append(" and r.nzp_user = u.nzp_user ");
                sql.Append(" and r.nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(" and r.nzp_readdr =" + finder.nzp_readdr + " ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при добавлении переадресации " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        service = new ServiceForwarding();

#if PG
                        //if (reader["nzp_readdr"] != DBNull.Value) service.nzp_readdr = Convert.ToInt32(reader["nzp_readdr"]);
                        //if (reader["nzp_zvk"] != DBNull.Value) service.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        //if (reader["nzp_slug"] != DBNull.Value) service.nzp_slug = Convert.ToInt32(reader["nzp_slug"]);
                        //if (reader["nzp_user"] != DBNull.Value) service.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                        //if (reader["_date"] != DBNull.Value) service._date = Convert.ToString(reader["_date"]);                            
                        //if (reader["comment"] != DBNull.Value) service.comment = Convert.ToString(reader["comment"]);
                        //if (reader["result_comment"] != DBNull.Value) service.result_comment = Convert.ToString(reader["result_comment"]);  
                        if (reader["nzp_readdr"] != DBNull.Value) service.nzp_readdr = Convert.ToInt32(reader["nzp_readdr"]);
                        if (reader["_date"] != DBNull.Value) service._date = Convert.ToString(reader["_date"]);
                        if (reader["slug_name"] != DBNull.Value) service.slug_name = Convert.ToString(reader["slug_name"]);
                        if (reader["phone"] != DBNull.Value) service.phone = Convert.ToString(reader["phone"]);
                        if (reader["user_name"] != DBNull.Value) service.user_name = Convert.ToString(reader["user_name"]);
                        if (reader["comment"] != DBNull.Value) service.comment = Convert.ToString(reader["comment"]);
                        if (reader["result_comment"] != DBNull.Value) service.result_comment = Convert.ToString(reader["result_comment"]);
                        if (reader["nzp_slug"] != DBNull.Value) service.nzp_slug = Convert.ToInt32(reader["nzp_slug"]);
#else
 //if (reader["nzp_readdr"] != DBNull.Value) service.nzp_readdr = Convert.ToInt32(reader["nzp_readdr"]);
                        //if (reader["nzp_zvk"] != DBNull.Value) service.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"]);
                        //if (reader["nzp_slug"] != DBNull.Value) service.nzp_slug = Convert.ToInt32(reader["nzp_slug"]);
                        //if (reader["nzp_user"] != DBNull.Value) service.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                        //if (reader["_date"] != DBNull.Value) service._date = Convert.ToString(reader["_date"]);                            
                        //if (reader["comment"] != DBNull.Value) service.comment = Convert.ToString(reader["comment"]);
                        //if (reader["result_comment"] != DBNull.Value) service.result_comment = Convert.ToString(reader["result_comment"]);  
                        if (reader["nzp_readdr"] != DBNull.Value) service.nzp_readdr = Convert.ToInt32(reader["nzp_readdr"]);
                        if (reader["_date"] != DBNull.Value) service._date = Convert.ToString(reader["_date"]);
                        if (reader["slug_name"] != DBNull.Value) service.slug_name = Convert.ToString(reader["slug_name"]);
                        if (reader["phone"] != DBNull.Value) service.phone = Convert.ToString(reader["phone"]);
                        if (reader["user_name"] != DBNull.Value) service.user_name = Convert.ToString(reader["user_name"]);
                        if (reader["comment"] != DBNull.Value) service.comment = Convert.ToString(reader["comment"]);
                        if (reader["result_comment"] != DBNull.Value) service.result_comment = Convert.ToString(reader["result_comment"]);
                        if (reader["nzp_slug"] != DBNull.Value) service.nzp_slug = Convert.ToInt32(reader["nzp_slug"]);
#endif
                    }
                }
                //}

                return service;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetReadress : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Сохранение результата редактирования переадресации
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool SaveCommentsReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

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
                //    return false;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}
                ////проверка на префиксы
                //if (prefix.Count == 0)
                //{
                //    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                //    return false;
                //}

                //#endregion

                //foreach (string pref in prefix)
                //{
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update  " + Points.Pref + "_supg. readdress set ( comment, result_comment, nzp_slug) = (\'" + finder.comment + "\',\'" + finder.result_comment + "\'," + finder.nzp_slug + ") ");
                sql.Append(" where  nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(" and  nzp_readdr = " + finder.nzp_readdr + " ");
#else
sql.Append(" update  " + Points.Pref + "_supg: readdress set ( comment, result_comment, nzp_slug) = (\'" + finder.comment + "\',\'" + finder.result_comment + "\'," + finder.nzp_slug + ") ");
                sql.Append(" where  nzp_zvk = " + finder.nzp_zvk + " ");
                sql.Append(" and  nzp_readdr = " + finder.nzp_readdr +" ");
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при добавлении переадресации " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                //}

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetReadress : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// выполнение процедуры генерации результата заявки
        /// </summary>
        /// <param name="Container">nzp_user, nzp_zvk</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public OrderContainer Result_Generating_Procedure(OrderContainer Container, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            OrderContainer RetData = new OrderContainer();

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

                #region Отметить дату изменения
#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("update " + Points.Pref + "_supg. zvk set last_modified= \'" + sToday + "\' where nzp_zvk=" + Container.nzp_zvk + ";");
#else
sql.Append("update " + Points.Pref + "_supg: zvk set last_modified= \'" + sToday + "\' where nzp_zvk=" + Container.nzp_zvk + ";");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выполнения операции " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                //sql.Append(" select " + Points.Pref + "_supg. gen_zvk_res (" + Container.nzp_zvk + ");");
                sql.Append("SELECT a.mx_exec_date, a.mx_fact_date, a.CurResult_no FROM " + Points.Pref +
                           "_supg. gen_zvk_res (" +
                           Container.nzp_zvk + ") a(mx_exec_date DATE, mx_fact_date DATE, CurResult_no INTEGER)");
#else
                sql.Append("execute procedure " + Points.Pref + "_supg: gen_zvk_res (" + Container.nzp_zvk + ");");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {

                    reader.Read();

                    int nzp_res = -1;
                    string res = "";
                    Dictionary<int, string> resDic = new Dictionary<int, string>();

#if PG
                    //string[] st = Convert.ToString(reader[0]).Split(new char[1] {','});
                    //if (string.IsNullOrEmpty(st[0])) RetData.exec_date = st[0];
                    //if (string.IsNullOrEmpty(st[1])) RetData.fact_date = st[1];
                    //if (string.IsNullOrEmpty(st[2])) nzp_res = Convert.ToInt32(st[2]);

                    if (reader["mx_exec_date"] != DBNull.Value) RetData.exec_date = Convert.ToString(reader["mx_exec_date"]);
                    if (reader["mx_fact_date"] != DBNull.Value) RetData.fact_date = Convert.ToString(reader["mx_fact_date"]);
                    if (reader["CurResult_no"] != DBNull.Value) nzp_res = Convert.ToInt32(reader["CurResult_no"]);
#else
                    if (reader["exec_date"] != DBNull.Value) RetData.exec_date = Convert.ToString(reader["exec_date"]);
                    if (reader["fact_date"] != DBNull.Value) RetData.fact_date = Convert.ToString(reader["fact_date"]);
                    if (reader["nzp_res"] != DBNull.Value) nzp_res = Convert.ToInt32(reader["nzp_res"]);
#endif
                    //if (reader["name_res"] != DBNull.Value) res = Convert.ToString(reader["name_res"]);
                    resDic.Add(nzp_res, res);
                    if (nzp_res != 4)
                    {
                        resDic.Add(4, "Отклонено");
                    }
                    RetData.result = resDic;

                }

                return RetData;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Result_Generating_Procedure : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Сохранение результата наряд-заказа
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateZakaz(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

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

                //Экранирование символов
                finder.comment_n = SymbolsScreening(finder.comment_n);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update " + Points.Pref + "_supg.zakaz set  ");
#else
                sql.Append(" update " + Points.Pref + "_supg:zakaz set  ");
#endif
                if ((finder.fact_date == "0001-01-01 00") || (finder.fact_date == null))
                {
                    sql.Append(" fact_date =   null, ");
                }
                else
                {
#if PG
                    sql.Append(" fact_date =   \'" + finder.fact_date + ":00:00" + "\', ");
#else
                    sql.Append(" fact_date =   \'" + finder.fact_date + "\', ");
#endif
                }
                //если результат плановый ремонт
#if PG
                if (finder.nzp_plan_no != 0)
                {
                    sql.Append("  nzp_plan_no  = " + finder.nzp_plan_no + ", ");
                }
                sql.Append("  nzp_res  = " + finder.nzp_res + ", ");
                sql.Append(" comment_n =   \'" + finder.comment_n + "\', ");
                if (finder.nzp_res != 3) finder.nzp_atts = 1;
                sql.Append(" nzp_atts = " + finder.nzp_atts + ", ");
                sql.Append(" nzp_answer =  " + finder.nzp_answer + ",");
                sql.Append(" nzp_user = " + local_user + ", ");
#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
                sql.Append(" last_modified = \'" + sToday + "\' ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
#else
 if (finder.nzp_plan_no != 0)
                {
                    sql.Append("  nzp_plan_no  = " + finder.nzp_plan_no + ", ");
                }
                sql.Append("  nzp_res  = " + finder.nzp_res + ", ");
                sql.Append(" comment_n =   \'" + finder.comment_n + "\', ");
                if (finder.nzp_res != 3) finder.nzp_atts = 1;
                sql.Append(" nzp_atts = " + finder.nzp_atts + ", ");
                sql.Append(" nzp_answer =  " + finder.nzp_answer + ",");
                sql.Append(" nzp_user = " + local_user + ", ");
                string sToday = Utils.GetSupgCurDate("T", "m");
                sql.Append(" last_modified = \'" + sToday + "\' ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении заявок " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }


                #region Выполнение процедуры формированиия недопоставок
                int norm = 0;
                int nzp_serv = 0;
                int num_nedop = 0;
#if PG
                string sqlStr = "select z.norm, d.nzp_serv, d.num_nedop ";
                sqlStr += " from " + Points.Pref + "_supg.zakaz z, ";
                sqlStr += " " + Points.Pref + "_supg. s_dest d ";
                sqlStr += " where z.nzp_dest = d.nzp_dest ";
                sqlStr += " and z.nzp_zk = " + finder.nzp_zk + " ";
#else
string sqlStr = "select z.norm, d.nzp_serv, d.num_nedop ";
                sqlStr += " from " + Points.Pref + "_supg:zakaz z, ";
                sqlStr += " " + Points.Pref + "_supg: s_dest d ";
                sqlStr += " where z.nzp_dest = d.nzp_dest ";
                sqlStr += " and z.nzp_zk = " + finder.nzp_zk + " ";
#endif
                if (!ExecRead(con_db, out reader, sqlStr, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки norm " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                if (reader.Read())
                {
                    if (reader["norm"] != DBNull.Value) norm = Convert.ToInt32(reader["norm"]);
                    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["num_nedop"] != DBNull.Value) num_nedop = Convert.ToInt32(reader["num_nedop"]);
                }
                //процедура
                JobOrder fproc = new JobOrder();
                fproc.nzp_user = finder.nzp_user;
                fproc.norm = norm;
                fproc.nzp_serv = nzp_serv;
                fproc.act_num_nedop = num_nedop;

                bool resProc = this.ExecGenZakazNedop(fproc, out ret);
                if (!resProc)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры gen_zakaz_nedop : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                #endregion


                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateZvk : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        //обновление таблицы заявок - обновление результат и комментария
        public bool UpdateZvk(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

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

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif

                //Экранирование символов
                finder.result_comment = this.SymbolsScreening(finder.result_comment);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update " + Points.Pref + "_supg .  zvk set ( nzp_res, result_comment,last_modified, comment, nzp_ztype) = (" + finder.nzp_res + ",\'" + finder.result_comment + "\', \'" + sToday + "\',:comment," + finder.nzp_ztype + ") ");
                sql.Append(" where  nzp_zvk = " + finder.nzp_zvk + " ");
#else
 sql.Append(" update " + Points.Pref + "_supg :  zvk set ( nzp_res, result_comment,last_modified, comment, nzp_ztype) = (" + finder.nzp_res + ",\'" + finder.result_comment + "\', \'" + sToday + "\',?," + finder.nzp_ztype + ") ");
                sql.Append(" where  nzp_zvk = " + finder.nzp_zvk + " ");
#endif
                IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), con_db);
#if PG
                DBManager.addDbCommandParameter(cmd, "comment", DbType.String, finder.comment);
#else
                DBManager.addDbCommandParameter(cmd, "@BinaryValue", DbType.String, finder.comment);
#endif
                int affRows = cmd.ExecuteNonQuery();
                if (affRows == 0)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении заявок " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                //if (!ExecSQL(con_db, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка при обновлении заявок " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}


                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateZvk : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Одбновление таблицы заявок (арм оператор)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool UpdateZvk_armOperator(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            //List<string> prefix = new List<string>();
            StringBuilder sql = new StringBuilder();

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
#if PG
                sql.Append(" update " + Points.Pref + "_supg . zvk ");
                sql.Append(" set nzp_kvar = " + finder.nzp_kvar + ", ");
                sql.Append(" demand_name = \'" + finder.demand_name + "\', ");
                sql.Append(" phone = \'" + finder.phone + "\', ");
                sql.Append(" nzp_ztype = " + finder.nzp_ztype + ", ");
                sql.Append(" comment = default ");
                sql.Append(" where nzp_zvk = " + finder.nzp_zvk + " ");
#else
 sql.Append(" update " + Points.Pref + "_supg : zvk ");
                sql.Append(" set nzp_kvar = " + finder.nzp_kvar + ", ");
                sql.Append(" demand_name = \'" + finder.demand_name + "\', ");
                sql.Append(" phone = \'" + finder.phone + "\', ");
                sql.Append(" nzp_ztype = " + finder.nzp_ztype + ", ");
                sql.Append(" comment = ? ");
                sql.Append(" where nzp_zvk = " + finder.nzp_zvk + " ");
#endif

                IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), con_db);
                DBManager.addDbCommandParameter(cmd, "@binaryValue", DbType.String, finder.comment);
                int countRows = cmd.ExecuteNonQuery();

                if (countRows <= 0)
                {
                    MonitorLog.WriteLog("Ошибка при обновлении заявки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateZvk : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получает список всех возможных услуг
        /// </summary>
        /// <returns>список услуг</returns>
        public Dictionary<int, string> GetAllServices(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDbConnection con_web = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            //List<string> prefix = new List<string>();
            Dictionary<int, string> retDic = new Dictionary<int, string>();
            retDic.Add(-1, "<не выбрано>");

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
                sql.Append(" Select nzp_serv, service from " + new DbTables(DBManager.getServer(con_db)).services);
                sql.Append(" where nzp_serv <> 1 ");
                sql.Append(" and nzp_serv in (select nzp_serv from " + Points.Pref + "_supg. s_dest)");
                sql.Append("  union all ");
                sql.Append(" select 1000,'-' ");
                sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).services);
                sql.Append(" where nzp_serv = 1 ");
                sql.Append(" order by service ");
#else
 sql.Append(" Select nzp_serv, service from " + new DbTables(DBManager.getServer(con_db)).services);
                sql.Append(" where nzp_serv <> 1 ");
                sql.Append(" and nzp_serv in (select nzp_serv from " + Points.Pref + "_supg: s_dest)");
                sql.Append("  union all ");
                sql.Append(" select 1000,'-' ");
                sql.Append(" from " + new DbTables(DBManager.getServer(con_db)).services);
                sql.Append(" where nzp_serv = 1 ");
                sql.Append(" order by service ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка при получения списка всевозможных услуг " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        int nzp_serv = -1;
                        string service = "";
                        if (reader["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["service"] != DBNull.Value) service = Convert.ToString(reader["service"]);

                        retDic.Add(nzp_serv, service);
                    }
                }
                //}

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAllServices : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Отчет по начислениям по поставщикам 
        /// </summary>
        /// <param name="nzp_user">номер пользователя</param>
        /// <param name="nzp_serv">код услуги</param>
        /// <param name="nzp_supp">код поставщика</param>
        /// <param name="nzp_area">код балансодержателя</param>
        /// <param name="nzp_geu">код участка</param>
        /// <param name="po_date">период по</param>
        /// <param name="s_date">период до</param>
        /// <returns></returns>
        public DataTable GetNachSupp(int supp, SupgFinder finder, out Returns ret, int yearr, bool serv)
        {
            ret = Utils.InitReturns();
            IDataReader reader = null;
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            DataTable res_table = new DataTable();
            StringBuilder sql = new StringBuilder();
            List<_Point> prefixs = new List<_Point>();
            _Point point = new _Point();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref != "")
            {
                point.pref = finder.pref;
                prefixs.Add(point);
            }
            else
            {
                prefixs = Points.PointList;
            }


            try
            {

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
                //Создание временной таблицы
#if PG
                string tXX_spls = "public." + "t" + finder.nzp_user + "_spls";
#else
                string tXX_spls = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + "t" + finder.nzp_user + "_spls";
#endif
                try
                {
                    sql.Append("drop table t_svod ");
                    ret = ExecSQL(con_db, sql.ToString(), false);
                    string sql1;
                    sql.Remove(0, sql.Length);
#if PG
                    //sql.Append(
                    sql1 = "create unlogged table t_svod" +
                              "(nzp_serv integer," +
                              " nzp_area  integer," +
                              " nzp_supp integer," +
                              " service_small char(20)," +
                              " name_supp char(30)," +
                              " tarif Decimal(14,2), " +
                              " sum_tarif Decimal(14,2)," +
                               "sum_lgota Decimal(14,2)," +
                              " sum_charge Decimal(14,2)," +
                             "  sum_subsidy Decimal(14,2)," +
                             "  perr_nach Decimal(14,2)," +
                              " real_charge Decimal(14,2), " +
                            "   rashod Decimal(14,2),  " +
                              " sum_smo Decimal(14,2));";
                    //);
#else
//sql.Append(
                    sql1 = "create temp table t_svod" +
                              "(nzp_serv integer," +
                              " nzp_area  integer," +
                              " nzp_supp integer," +
                              " service_small char(20)," +
                              " name_supp char(30)," +
                              " tarif Decimal(14,2), " +
                              " sum_tarif Decimal(14,2)," +
                               "sum_lgota Decimal(14,2)," +
                              " sum_charge Decimal(14,2)," +
                             "  sum_subsidy Decimal(14,2)," +
                             "  perr_nach Decimal(14,2)," +
                              " real_charge Decimal(14,2), " +
                            "   rashod Decimal(14,2),  " +
                              " sum_smo Decimal(14,2)) with no log;";
                    //);
#endif

                    ret = ExecSQL(con_db, sql1, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при создании таблицы supg" + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                //string s = Convert.ToDateTime(finder._date_from).ToString("yyyy.MM.dd HH:mm:ss");
                string po = Convert.ToDateTime(finder._date_to).ToString("yyyy.MM.dd HH:mm:ss");
                string s_fact_year = Convert.ToDateTime(finder._date_from).ToString("yy");
                string po_fact_year = Convert.ToDateTime(finder._date_to).ToString("yy");
                string s_fact_month = Convert.ToDateTime(finder._date_from).ToString("MM");
                string po_fact_month = Convert.ToDateTime(finder._date_to).ToString("MM");



                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                #region заполнение временной таблицы
                /*  try
                {
                    foreach (_Point items in prefixs)
                    {
                        
                    
                            
                                      


                    

                    
                    }
                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + " : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }
                catch (Exception ex) { }*/
                foreach (_Point items in prefixs)
                {
                    int m = int.Parse(s_fact_month);

                    for (int y = int.Parse(s_fact_year); y <= int.Parse(po_fact_year); y++)
                    {
                        do
                        {
                            sql.Remove(0, sql.Length);

                            sql.Append("insert into t_svod (");
                            if (serv)
                            {
                                sql.Append("nzp_serv, nzp_area, ");
                            }
                            else
                            {
                                sql.Append("nzp_supp, nzp_area, ");
                            }
                            if (serv)
                            {
                                sql.Append("service_small, ");
                            }
                            else
                            {
                                sql.Append("name_supp, ");
                            }

                            sql.Append("tarif, " +
                                      "sum_tarif," +
                                      "sum_lgota," +
                                      "sum_charge," +
                                      "sum_subsidy," +
                                      "perr_nach," +
                                      "real_charge,");

                            if (serv)
                            {
                                sql.Append("rashod, ");
                            }
                            sql.Append("sum_smo) ");
                            sql.Append("SELECT ");
                            if (serv)
                            {
                                sql.Append("c.nzp_serv, a.nzp_area,");
                            }
                            else
                            {
                                sql.Append("c.nzp_supp, a.nzp_area, ");
                            }
                            if (serv)
                            {
                                sql.Append("s.service_small, ");
                            }
                            else
                            {
                                sql.Append("s.name_supp,");
                            }

                            sql.Append("c.tarif, " +
                                       "sum(c.sum_tarif), " +
                                       "sum(c.sum_lgota), " +
                                       "sum(c.sum_tarif+c.real_charge) as sum_charge, " +
                                       "sum(c.sum_subsidy), " +
                                       "sum(c.reval) as perr_nach, " +
                                       "sum(c.real_charge), ");
                            if (serv)
                            {
                                sql.Append("sum(c.c_calc) as rashod, ");
                            }
                            string str = "";
                            if (m <= 9)
                            {
                                str = "0" + m.ToString();
                            }

#if PG
                            sql.Append("sum(0) as sum_smo FROM " + items.pref + "_charge_" + y + ".charge_" + str + " c," + items.pref + "_data.kvar a, " + tXX_spls + " f, ");
                            if (serv)
                            {
                                sql.Append(items.pref + "_kernel.services s");
                            }
                            else
                            {
                                sql.Append(items.pref + "_kernel.supplier s");
                            }
                            if (serv)
                            {
                                sql.Append(" WHERE c.nzp_serv>1 and c.nzp_kvar=a.nzp_kvar  and s.nzp_serv=c.nzp_serv and c.tarif>0 and a.num_ls=f.num_ls ");
                                if (finder.nzp_serv != -1)
                                {
                                    sql.Append("and c.nzp_serv=" + finder.nzp_serv.ToString());
                                }
                            }
                            else
                            {
                                sql.Append(" WHERE c.nzp_supp>1 and c.nzp_kvar=a.nzp_kvar and s.nzp_supp=c.nzp_supp and c.tarif>0 and a.num_ls=f.num_ls ");
                                if (finder.zk_nzp_supp != -1)
                                {
                                    sql.Append("and c.nzp_supp=" + finder.zk_nzp_supp.ToString());
                                }
                            }
                            sql.Append(" group by 1,2,3,4");
#else
sql.Append("sum(0) as sum_smo FROM " + items.pref + "_charge_" + y + ":charge_" + str + " c," + items.pref + "_data:kvar a, " + tXX_spls + " f, ");
                            if (serv)
                            {
                                sql.Append(items.pref + "_kernel:services s");
                            }
                            else
                            {
                                sql.Append(items.pref + "_kernel:supplier s");
                            }
                            if (serv)
                            {
                                sql.Append(" WHERE c.nzp_serv>1 and c.nzp_kvar=a.nzp_kvar  and s.nzp_serv=c.nzp_serv and c.tarif>0 and a.num_ls=f.num_ls ");
                                if (finder.nzp_serv != -1)
                                {
                                    sql.Append("and c.nzp_serv=" + finder.nzp_serv.ToString());
                                }
                            }
                            else
                            {
                                sql.Append(" WHERE c.nzp_supp>1 and c.nzp_kvar=a.nzp_kvar and s.nzp_supp=c.nzp_supp and c.tarif>0 and a.num_ls=f.num_ls ");
                                if (finder.zk_nzp_supp != -1)
                                {
                                    sql.Append("and c.nzp_supp=" + finder.zk_nzp_supp.ToString());
                                }
                            }
                            sql.Append(" group by 1,2,3,4");
#endif


                            ExecSQL(con_db, sql.ToString(), true);
                            sql.Remove(0, sql.Length);

                            sql.Append("select ");
                            if (serv)
                            {
                                sql.Append("b.service_small,");
                            }
                            else
                            {
                                sql.Append("b.name_supp,");
                            }
                            sql.Append("sum(a.sum_tarif) as sum_tarif," +
                                       "sum(a.tarif) as tarif, " +
                                       "sum(a.sum_lgota) as sum_lgota, " +
                                       "sum(a.sum_charge) as sum_charge, " +
                                       "sum(a.sum_subsidy) as sum_subsidy, " +
                                       "sum(a.perr_nach) as perr_nach, " +
                                       "sum(a.real_charge) as real_charge,");
                            if (serv)
                            {
                                sql.Append("sum(a.rashod) as rashod, ");
                            }

                            sql.Append("sum(a.sum_smo) as sum_smo " +
                                       "from t_svod a, ");
#if PG
                            if (serv)
                            {
                                sql.Append(items.pref + "_kernel.services b ");
                            }
                            else
                            {
                                sql.Append(items.pref + "_kernel.supplier b ");
                            }
#else
if (serv)
                            {
                                sql.Append(items.pref + "_kernel:services b ");
                            }
                            else
                            {
                                sql.Append(items.pref + "_kernel:supplier b ");
                            }
#endif

                            if (serv)
                            {
                                sql.Append(" where a.nzp_serv=b.nzp_serv  and a.nzp_serv>1 group by 1 order by 1 ");
                            }
                            else
                            {
                                sql.Append(" where a.nzp_supp=b.nzp_supp  and a.nzp_supp>1 group by 1 order by 1 ");
                            }


                            if (y == int.Parse(po_fact_year) && m == int.Parse(po_fact_month))
                            {
                                break;
                            }
                            m++;


                            if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка выборки GetNachSupp" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                ret.result = false;
                                return null;
                            }
                            sql.Remove(0, sql.Length);
                        } while (m <= 12);

                        m = 1;
                    }
                }
                try
                {
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.PreserveChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetNachSupp " + ex.Message, MonitorLog.typelog.Error, true);
                    reader.Close();
                    return null;
                }
                return res_table;
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNachSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table t_svod " + "; ", false);


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
        /// Формирование данных о долгах по л.с. и данных о поставщиках услуг для работы в режиме "Работа с центральным банком данных" 
        /// </summary>
        public bool FillLSSaldo(out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Функция обновления информации о долгах по лицевым счетам недоступна, т.к. установлен режим работы с центральным банком данных", -1);
                return false;
            }

            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<_Point> points = Points.PointList;

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
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region цикл по префиксам + создание временной таблицы
                foreach (_Point point in points)
                {
                    string cur_charge_db = "";
                    string cur_saldo_date = "null";

                    #region текущий закрытый месяц
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("select * from " + point.pref + "_data.saldo_date where iscurrent=1 order by dat_saldo desc limit 1");
#else
                    sql.Append("select first 1 * from " + point.pref + "_data:saldo_date where iscurrent=1 order by dat_saldo desc");
#endif
                    ret = ExecRead(con_db, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка определения текущего месяца : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }

                    while (reader.Read())
                    {
                        if (reader["yearr"] != DBNull.Value && reader["month_"] != DBNull.Value)
                        {
#if PG
                            cur_charge_db = point.pref + "_charge_" + reader["yearr"].ToString().Substring(2) + ".charge_" + String.Format("{0:00}", reader["month_"]);
                            cur_saldo_date = "mdy(" + reader["month_"].ToString() + ",1," + reader["yearr"].ToString() + ")";
#else
                            cur_charge_db = point.pref + "_charge_" + reader["yearr"].ToString().Substring(2) + ":charge_" + String.Format("{0:00}", reader["month_"]);  
                            cur_saldo_date = "mdy(" + reader["month_"].ToString()+",1," + reader["yearr"].ToString()+")";
#endif
                        }
                        break;
                    }

                    #endregion

                    #region обновление данных по л.с.

                    #region сальдо по л.с.

                    #region Добавить недостающие лиц.счета
                    if (TempTableInWebCashe(con_db, "tmp_kvarlist"))
                    {
                        if (!ExecSQL(con_db, "DROP TABLE tmp_kvarlist", true).result)
                        {
                            MonitorLog.WriteLog("Ошибка удаление временной таблицы : " + ret.text, MonitorLog.typelog.Error, true);
                            return false;
                        }
                    }
                    sql.Remove(0, sql.Length);
                    sql.Append("CREATE " + DBManager.sCrtTempTable + " TABLE tmp_kvarlist (nzp_kvar INTEGER, num_ls INTEGER)" + DBManager.sUnlogTempTable);
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка создания временной таблицы : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }

                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append("insert into tmp_kvarlist (nzp_kvar, num_ls) " +
                        " select nzp_kvar, num_ls from " + point.pref + "_data.kvar where nzp_kvar not in " +
                        " (select nzp_kvar from " + Points.Pref + "_supg. ls_saldo where nzp_wp=" + point.nzp_wp.ToString() + ")");
#else
                    sql.Append("insert into tmp_kvarlist (nzp_kvar, num_ls) "+
                        " select nzp_kvar, num_ls from "+point.pref + "_data:kvar where nzp_kvar not in "+
                        " (select nzp_kvar from "+ Points.Pref + "_supg: ls_saldo where nzp_wp="+point.nzp_wp.ToString()+")");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка заполения временной таблицы  : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("insert into " + Points.Pref + "_supg. ls_saldo (nzp_kvar, num_ls) select nzp_kvar, num_ls from tmp_kvarlist");
#else
                    sql.Append("insert into " + Points.Pref + "_supg: ls_saldo (nzp_kvar, num_ls) select nzp_kvar, num_ls from tmp_kvarlist");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления лиц.счетов : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append("drop table tmp_kvarlist");
                    ret = ExecSQL(con_db, sql.ToString(), false);

                    #endregion

                    #region Занести сальдо
                    //обнулить сальдо л.с. данного nzp_wp
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("update " + Points.Pref + "_supg. ls_saldo set sum_debt=0, sum_real=0, sum_money=0, saldo_date=null where nzp_wp=" + point.nzp_wp.ToString());
#else
                    sql.Append("update " + Points.Pref + "_supg: ls_saldo set sum_debt=0, sum_real=0, sum_money=0, saldo_date=null where nzp_wp="+point.nzp_wp.ToString());
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка обновления данных сальдо по л.с. : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }
                    //занести сальдо л.с. данного nzp_wp
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("update " + Points.Pref + "_supg. ls_saldo set (saldo_date, sum_debt, sum_real, sum_money) =" +
                               " ((select " + cur_saldo_date + ", sum(sum_outsaldo), sum(sum_tarif), sum(sum_money) from " + cur_charge_db +
                               " where nzp_serv=1 and nzp_kvar=" + Points.Pref + "_supg. ls_saldo.nzp_kvar group by 1)) " +
                               " where nzp_wp=" + point.nzp_wp.ToString());
#else
                    sql.Append("update " + Points.Pref + "_supg: ls_saldo set (saldo_date, sum_debt, sum_real, sum_money) =" +
                               " ((select " + cur_saldo_date + ", sum(sum_outsaldo), sum(sum_tarif), sum(sum_money) from " + cur_charge_db + 
                               " where nzp_serv=1 and nzp_kvar=" + Points.Pref + "_supg: ls_saldo.nzp_kvar group by 1)) " +
                               " where nzp_wp=" + point.nzp_wp.ToString());
#endif
                    ret = ExecSQL(con_db, sql.ToString(), false);
                    //if (!ret.result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка обновления данных сальдо по л.с. : " + ret.text, MonitorLog.typelog.Error, true);
                    //    return false;
                    //}

                    #endregion

                    #endregion

                    #endregion
                }

                #region Отметить дату выполнения операции
#if PG
                sql.Remove(0, sql.Length);
                sql.Append("delete from " + Points.Pref + "_supg. settings where set_id=1");
                ret = ExecSQL(con_db, sql.ToString(), false);
                sql.Remove(0, sql.Length);
                sql.Append("insert into " + Points.Pref + "_supg. settings (set_id, set_value) values (1, today)");
                ret = ExecSQL(con_db, sql.ToString(), false);
#else
 sql.Remove(0, sql.Length);
                sql.Append("delete from " + Points.Pref + "_supg: settings where set_id=1");
                ret = ExecSQL(con_db, sql.ToString(), false);
                sql.Remove(0, sql.Length);
                sql.Append("insert into " + Points.Pref + "_supg: settings (set_id, set_value) values (1, today)");
                ret = ExecSQL(con_db, sql.ToString(), false);
#endif
                #endregion

                return true;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FillLSSaldo : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Формирование данных о долгах по л.с. и данных о поставщиках услуг для работы в режиме "Работа с центральным банком данных" 
        /// </summary>
        public bool FillLSTarif(out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Функция обновления информации о поставщиках услуг по лицевым счетам недоступна, т.к. установлен режим работы с центральным банком данных", -1);
                return false;
            }

            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<_Point> points = Points.PointList;

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
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Добавить недостающие лиц.счета
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("create unlogged table tmp_kvarlist (nzp_kvar integer, nzp_serv integer) ");

#else
                sql.Append("create temp table tmp_kvarlist (nzp_kvar integer, nzp_serv integer) with no log");

#endif
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы : " + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                sql.Remove(0, sql.Length);

                sql.Append("create index ix_temp_table on tmp_kvarlist (nzp_kvar, nzp_serv)");

                ret = ExecSQL(con_db, sql.ToString(), false);

                sql.Remove(0, sql.Length);

#if PG
                sql.Append("insert into tmp_kvarlist (nzp_kvar, nzp_serv) " +
                                  " select k.nzp_kvar, s.nzp_serv " +
                                  " from " + Points.Pref + "_data.kvar k, " + Points.Pref + "_kernel.services s " +
                                  " where s.nzp_serv in (5,12,13,26) and k.is_open <> '2' and not exists " +
                                  " (select * from " + Points.Pref + "_data. tarif t " +
                                  "  where " +
                                  " t.nzp_kvar=k.nzp_kvar and " +
                                  " t.nzp_serv=s.nzp_serv " +
                                  ")");
#else
  sql.Append("insert into tmp_kvarlist (nzp_kvar, nzp_serv) " +
                    " select k.nzp_kvar, s.nzp_serv "+
                    " from " + Points.Pref + "_data:kvar k, " + Points.Pref + "_kernel:services s " +
                    " where s.nzp_serv in (5,12,13,26) and k.is_open <> '2' and not exists " +
                    " (select * from " + Points.Pref + "_data: tarif t " +
                    "  where "+
                    " t.nzp_kvar=k.nzp_kvar and " +
                    " t.nzp_serv=s.nzp_serv " +
                    ")");
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка заполения временной таблицы  : " + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("insert into " + Points.Pref + "_data. tarif (nzp_kvar, nzp_serv) select nzp_kvar, nzp_serv from tmp_kvarlist");
#else
                sql.Append("insert into " + Points.Pref + "_data: tarif (nzp_kvar, nzp_serv) select nzp_kvar, nzp_serv from tmp_kvarlist");
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка добавления лиц.счетов : " + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }

                sql.Remove(0, sql.Length);
                sql.Append("drop table tmp_kvarlist");
                ret = ExecSQL(con_db, sql.ToString(), false);

                #endregion

                #region цикл по префиксам + создание временной таблицы
                foreach (_Point point in points)
                {

                    #region Поставщики услуг по л.с. (tarif)


                    #region Занести tarif
                    //обнулить сальдо л.с. данного nzp_wp
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append("update " + Points.Pref + "_data. tarif set is_actual=100, nzp_supp = 0 " +
                               "where nzp_kvar in (select nzp_kvar from " + point.pref + "_data. kvar)");
#else
                    sql.Append("update " + Points.Pref + "_data: tarif set is_actual=100, nzp_supp = 0 "+
                               "where nzp_kvar in (select nzp_kvar from " + point.pref + "_data: kvar)");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка обновления данных о поставщиках по л.с. : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }
                    //занести сальдо л.с. данного nzp_wp
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" UPDATE " + Points.Pref + "_data. tarif " +
                               " SET dat_s = _dat_s, " +
                                   " dat_po = _dat_po, " +
                                   " is_actual = _is_actual, " +
                                   " nzp_supp = _nzp_supp " +
                               " FROM (SELECT nzp_kvar, nzp_serv, " +
                                            " mdy(1,1,1900) AS _dat_s, " +
                                            " mdy(1,1,3000) AS _dat_po, " +
                                            " 1 AS _is_actual, " +
                                            " MAX(nzp_supp) AS _nzp_supp " +
                                     " FROM " + point.pref + "_data.tarif " +
                                     " WHERE is_actual=1 " +
                                       " AND NOW() BETWEEN dat_s AND dat_po " +
                                     " GROUP BY 1,2,3,4,5) t " +
                               " WHERE t.nzp_kvar=" + Points.Pref + "_data.tarif.nzp_kvar " +
                                 " AND t.nzp_serv=" + Points.Pref + "_data.tarif.nzp_serv " +
                                 " AND " + Points.Pref + "_data.tarif.nzp_kvar IN (SELECT nzp_kvar FROM " + point.pref + "_data.kvar)");
#else
                    sql.Append("update " + Points.Pref + "_data: tarif set (dat_s, dat_po, is_actual, nzp_supp) =" +
                               //" ((select dat_s, dat_po, nzp_supp "+
                               " ((select mdy(1,1,1900), mdy(1,1,3000), 1, max(nzp_supp) " +
                               " from " + point.pref + "_data: tarif "+
                               " where nzp_kvar=" + Points.Pref + "_data: tarif.nzp_kvar and nzp_serv=" + Points.Pref + "_data: tarif.nzp_serv "+
                               " and is_actual=1 and today between dat_s and dat_po group by 1,2,3)) " +
                               "where nzp_kvar in (select nzp_kvar from " + point.pref + "_data: kvar)");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), false);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка обновления данных о поставщиках по л.с. : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }

                    //ExecSQL(con_db, "update statistics for table tarif", true);


                    #endregion

                    #endregion

                }

                #region Отметить дату выполнения операции
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("delete from " + Points.Pref + "_supg. settings where set_id=2");
                ret = ExecSQL(con_db, sql.ToString(), false);
                sql.Remove(0, sql.Length);
                sql.Append("insert into " + Points.Pref + "_supg. settings (set_id, set_value) values (2, today)");
#else
                sql.Append("delete from " + Points.Pref + "_supg: settings where set_id=2");
                ret = ExecSQL(con_db, sql.ToString(), false);
                sql.Remove(0, sql.Length);
                sql.Append("insert into " + Points.Pref + "_supg: settings (set_id, set_value) values (2, today)");
#endif
                ret = ExecSQL(con_db, sql.ToString(), false);

                #endregion

                return true;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FillLSTarif : " + ex.Message, MonitorLog.typelog.Error, true);
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

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// получить долг по лицевому счету
        /// </summary>
        /// <param name="finder">pref, num</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public decimal GetDolgLs(Ls finder, int dat_y, int dat_m, out Returns ret)
        {

            //if (GlobalSettings.WorkOnlyWithCentralBank)
            //{
            //    ret = new Returns(false, "Данные о долге по лицевому счету недоступны, т.к. установлен режим работы с центральным банком данных", -1);
            //    return -1;
            //} 

            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            decimal dolg = 0;
            decimal sum1 = 0;
            decimal sum2 = 0;

            string pref = finder.pref;

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return -1;
                }
                #endregion

                if (GlobalSettings.WorkOnlyWithCentralBank)
                {
                    //ret = new Returns(false, "Данные о долге по лицевому счету недоступны, т.к. установлен режим работы с центральным банком данных", -1);
                    //return -1;

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select saldo_date, sum_debt  ");
                    sql.Append(" from " + Points.Pref + "_supg.ls_saldo");
                    sql.Append(" where nzp_kvar = " + finder.nzp_kvar);
#else
                    sql.Append(" select saldo_date, sum_debt  ");
                    sql.Append(" from " + Points.Pref + "_supg:ls_saldo");
                    sql.Append(" where nzp_kvar = " + finder.nzp_kvar );
#endif
                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return -1;
                    }

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["sum_debt"] != DBNull.Value) dolg = Convert.ToDecimal(reader["sum_debt"]);
                            if (reader["saldo_date"] != DBNull.Value) ret.text = reader["saldo_date"].ToString();
                        }
                    }
                }

                else
                {
                    #region Обработка даты
                    string y = "";
                    string m = "";

                    string dat_s = "";
                    string dat_po = "";

                    string m_past = "";
                    string y_past = "";


                    m = String.Format("{0:00}", dat_m);
                    y = dat_y.ToString().Substring(2);

                    DateTime d1;
                    DateTime.TryParse("01." + m + "." + dat_y, out d1);
                    if (d1 == DateTime.MinValue)
                    {
                        MonitorLog.WriteLog("Ошибка вычисления даты GetDolgLs", MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    //dat_s = "01." + m + "." + dat_y;
                    //dat_po = DateTime.DaysInMonth(dat_y, dat_m) + "." + m + "." + dat_y;
                    dat_s = "01." + String.Format("{0:00}", Points.CalcMonth.month_) + "." + Points.CalcMonth.year_;
                    dat_po = DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_) + "." + String.Format("{0:00}", Points.CalcMonth.month_) + "." + Points.CalcMonth.year_;


                    DateTime d2 = d1.AddMonths(-1);
                    m_past = String.Format("{0:00}", d2.Month);
                    y_past = d2.Year.ToString().Substring(2);

                    #endregion

                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" select sum (ch.sum_insaldo) as sum1  ");
                    sql.Append(" from " + pref + "_charge_" + y_past + ".charge_" + m_past + " ch ");
                    sql.Append(" where ch.num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
                    sql.Append(" and ch.nzp_serv >1 ");
                    sql.Append(" and ch.dat_charge is null ");
#else
                    sql.Append(" select sum (ch.sum_insaldo) as sum1  ");
                    sql.Append(" from " + pref + "_charge_" + y_past + ":charge_" + m_past + " ch ");
                    sql.Append(" where ch.num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
                    sql.Append(" and ch.nzp_serv >1 ");
                    sql.Append(" and ch.dat_charge is null ");
#endif



                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return -1;
                    }


                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["sum1"] != DBNull.Value) sum1 = Convert.ToDecimal(reader["sum1"]);
                        }
                    }

                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" select sum(g_sum_ls) as sum2   ");
                    sql.Append(" from " + Points.Pref + "_fin_" + y + ".pack_ls ");
                    sql.Append(" where num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
                    //sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
                    sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
#else
                    sql.Append(" select sum(g_sum_ls) as sum2   ");
                    sql.Append(" from " + pref + "_fin_" + y + ":pack_ls ");
                    sql.Append(" where num_ls = (select  num_ls from " + new DbTables(DBManager.getServer(con_db)).kvar + " where  nzp_kvar = " + finder.nzp_kvar + ") ");
                    //sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
                    sql.Append(" and ((dat_uchet >= \'" + dat_s + "\' and dat_uchet <= \'" + dat_po + "\') or dat_uchet is null) ");
#endif
                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return -1;
                    }


                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader["sum2"] != DBNull.Value) sum2 = Convert.ToDecimal(reader["sum2"]);
                        }
                    }

                    dolg = sum1 - sum2;
                }

                return dolg;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDolgLs : " + ex.Message, MonitorLog.typelog.Error, true);
                return -1;
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


    }
}
