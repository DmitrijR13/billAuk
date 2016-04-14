using FastReport;
using SevenZip;
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
        /// Получить список значений факта выполнения
        /// </summary>
        /// <param name="finder">поисковик</param>
        /// <param name="ret">результат</param>
        /// <returns>список подтверждений</returns>
        public Dictionary<int, string> GetAttistation(Finder finder, out Returns ret)
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
                sql.Append(" Select  nzp_atts, atts_name from  " + Points.Pref + "_supg. s_attestation; ");
#else
                sql.Append(" Select  nzp_atts, atts_name from  " + Points.Pref + "_supg: s_attestation; ");
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

                        int nzp_atts = -1;
                        string atts_name = "";

                        if (reader["nzp_atts"] != DBNull.Value) nzp_atts = Convert.ToInt32(reader["nzp_atts"]);
                        if (reader["atts_name"] != DBNull.Value) atts_name = Convert.ToString(reader["atts_name"]);

                        retDic.Add(nzp_atts, atts_name);

                    }
                }

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAttistation : " + ex.Message, MonitorLog.typelog.Error, true);
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

        /// <summary>
        /// Добавление повторного наряд-заказа
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool AddRepeatedJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
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
                DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();
                #endregion

                //определение номера
                //DbEditInterData dbEID = new DbEditInterData();
                nzp_zk = 0; // dbEID.GetSeriesProc(Points.Pref, 15, out ret);
                //dbEID.Close();
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка добавления наряда-заказа(определение номера) : " + ret.text, MonitorLog.typelog.Error, true);
                //    return false;
                //}

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
#if PG
                sql.Append(" insert into " + Points.Pref + "_supg.zakaz    (nzp_zk,nzp_zvk, nzp_dest, nzp_supp, norm  , temperature,nedop_s,exec_date,  ");
                sql.Append(" order_date,nzp_user,nzp_res,nzp_atts,is_replicate,repeated,parentno,nzp_status,ds_actual,ds_date,ds_user,last_modified, nzp_payer) ");
                sql.Append("select " + nzp_zk + ", nzp_zvk, nzp_dest, nzp_supp,norm,temperature,nedop_s,exec_date, \'" + finder.order_date + "\'," + local_user + ",5,1,1,0, nzp_zk, 2, ds_actual,ds_date,ds_user,\'" + sToday + "\', " + finder.nzp_payer + " ");
                sql.Append(" from " + Points.Pref + "_supg.zakaz  ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
#else
sql.Append(" insert into " + Points.Pref+"_supg:zakaz    (nzp_zk,nzp_zvk, nzp_dest, nzp_supp, norm  , temperature,nedop_s,exec_date,  ");
                sql.Append(" order_date,nzp_user,nzp_res,nzp_atts,is_replicate,repeated,parentno,nzp_status,ds_actual,ds_date,ds_user,last_modified, nzp_payer) ");
                sql.Append("select " + nzp_zk + ", nzp_zvk, nzp_dest, nzp_supp,norm,temperature,nedop_s,exec_date, \'" + finder.order_date + "\'," + local_user + ",5,1,1,0, nzp_zk, 2, ds_actual,ds_date,ds_user,\'" + sToday + "\', " + finder.nzp_payer + " ");
                sql.Append(" from " + Points.Pref+"_supg:zakaz  ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
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
                sql.Append(" update " + Points.Pref + "_supg.zakaz set  repeated =1, ");
                sql.Append(" replno = " + nzp_zk + " ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
#else
sql.Append(" update " + Points.Pref + "_supg:zakaz set  repeated =1, ");
                sql.Append(" replno = " + nzp_zk + " ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
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
                sql.Append(" from " + Points.Pref + "_supg: zakaz z ");
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
        /// Добавить недопоставку(наряд заказ)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool AddNedopJobOrder(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql.Remove(0, sql.Length);



#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
#if PG
                sql.Append(" update " + Points.Pref + "_supg.zakaz set act_s = \'" + finder.act_s + ":00:00" + "\', ");
                sql.Append("  act_po = \'" + finder.act_po + ":00:00" + "\', ");
                sql.Append(" act_num_nedop = " + finder.act_num_nedop + ", ");
                sql.Append(" act_temperature = " + finder.act_temperature + ", ");
                sql.Append(" act_actual = " + finder.act_actual + ", ");
                sql.Append(" last_modified = \'" + sToday + "\' ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "   ");
#else
sql.Append(" update " + Points.Pref + "_supg:zakaz set act_s = \'" + finder.act_s + "\', ");
                sql.Append("  act_po = \'" + finder.act_po + "\', ");
                sql.Append(" act_num_nedop = " + finder.act_num_nedop + ", ");
                sql.Append(" act_temperature = " + finder.act_temperature + ", ");
                sql.Append(" act_actual = " + finder.act_actual + ", ");
                sql.Append(" last_modified = \'" + sToday + "\' ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "   ");
#endif



                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка добавления данных недопоставки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                #region Выполнение процедуры формированиия недопоставок
                IDataReader reader = null;
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNedopJobOrder : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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

        /// <summary>
        /// Проверка закрыт ли наряд заказ для редактирования
        /// </summary>
        /// <param name="finder">pref, nzp_zk</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool IsOrderClose(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            try
            {
                #region Открываем соединение с базами


                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" Select  nzp_res, nzp_atts from " + Points.Pref + "_supg. zakaz where nzp_zk = " + finder.nzp_zk + " ");
#else
                sql.Append(" Select  nzp_res, nzp_atts from " + Points.Pref +"_supg: zakaz where nzp_zk = " + finder.nzp_zk + " ");
#endif


                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                int nzp_res = -1;
                int nzp_atts = -1;
                if (reader != null)
                {
                    reader.Read();
                    nzp_res = -1;
                    nzp_atts = -1;

                    if (reader["nzp_res"] != DBNull.Value) nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["nzp_atts"] != DBNull.Value) nzp_atts = Convert.ToInt32(reader["nzp_atts"]);
                }

                if (nzp_res == 3 && nzp_atts == 3)
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры IsOrderClose : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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

        /// <summary>
        /// Получить список недопоставок по клнкретной услуге
        /// </summary>
        /// <param name="finder">nzp_serv</param>
        /// <param name="ret"></param>
        /// <returns>список недопоставок</returns>
        public List<JobOrder> GetNedopsAll(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            List<JobOrder> retList = new List<JobOrder>();


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
                sql.Append(" select  nzp_kind, name,  is_param ");
                sql.Append(" from    " + Points.Pref + "_data.upg_s_kind_nedop ");
                sql.Append(" where   nzp_parent = " + finder.nzp_serv + "  ");
                sql.Append("  and  kod_kind = 1 ");
#else
                sql.Append(" select  nzp_kind, name,  is_param ");
                sql.Append(" from    " + Points.Pref + "_data:upg_s_kind_nedop ");
                sql.Append(" where   nzp_parent = " + finder.nzp_serv + "  ");
                sql.Append("  and  kod_kind = 1 ");
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
                        JobOrder jo = new JobOrder();

                        if (reader["nzp_kind"] != DBNull.Value) jo.nzp_kind = Convert.ToInt32(reader["nzp_kind"]);
                        if (reader["name"] != DBNull.Value) jo.name = Convert.ToString(reader["name"]);
                        if (reader["is_param"] != DBNull.Value) jo.is_param = Convert.ToInt32(reader["is_param"]);

                        retList.Add(jo);
                    }
                }


                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNedopsAll : " + ex.Message, MonitorLog.typelog.Error, true);
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


        /// <summary>
        /// Копирует одни поля в другие при смене результата
        /// </summary>
        /// <param name="nzp_res">результат</param>
        /// <returns>Успех выполнения</returns>
        public bool CopyFields_WhenResultChanged(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами


                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql.Remove(0, sql.Length);

                //Если результат выполнено
#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "m");
#endif
                if (finder.nzp_res == 3)
                {
#if PG
                    sql.Append(" update " + Points.Pref + "_supg.zakaz set act_s=nedop_s, ");
                    sql.Append(" act_po=fact_date, ");
                    sql.Append(" act_num_nedop= (select d.num_nedop from  ");
                    sql.Append(" " + Points.Pref + "_supg. s_dest d  ");
                    sql.Append(" where " + Points.Pref + "_supg . zakaz" + ".nzp_dest = d.nzp_dest ");
                    sql.Append(" and " + Points.Pref + "_supg . zakaz" + ".nzp_zk = " + finder.nzp_zk + " ");
                    sql.Append("  ),   ");
                    sql.Append(" act_temperature=temperature, ");
                    //sql.Append(" act_actual = 1, ");
                    sql.Append(" last_modified = \'" + sToday + "\' ");
                    sql.Append(" where nzp_zk = " + finder.nzp_zk + "  ");
#else
sql.Append(" update " + Points.Pref + "_supg:zakaz set act_s=nedop_s, ");
                    sql.Append(" act_po=fact_date, ");
                    sql.Append(" act_num_nedop= (select d.num_nedop from  ");                    
                    sql.Append(" " + Points.Pref + "_supg: s_dest d  ");
                    sql.Append(" where " + Points.Pref + "_supg : zakaz" + ".nzp_dest = d.nzp_dest ");
                    sql.Append(" and " + Points.Pref + "_supg : zakaz" + ".nzp_zk = " + finder.nzp_zk + " ");
                    sql.Append("  ),   ");
                    sql.Append(" act_temperature=temperature, ");
                    //sql.Append(" act_actual = 1, ");
                    sql.Append(" last_modified = \'" + sToday + "\' ");
                    sql.Append(" where nzp_zk = " + finder.nzp_zk + "  ");
#endif
                }
                else
                {
#if PG
                    sql.Append(" update " + Points.Pref + "_supg. zakaz set act_s = null,  ");
                    sql.Append(" act_po = null,  ");
                    sql.Append(" act_num_nedop = null,  ");
                    sql.Append(" act_temperature = null, ");
                    sql.Append(" last_modified = \'" + sToday + "\' ");
                    sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
#else
sql.Append(" update " + Points.Pref + "_supg: zakaz set act_s = null,  ");
                    sql.Append(" act_po = null,  ");
                    sql.Append(" act_num_nedop = null,  ");
                    sql.Append(" act_temperature = null, ");
                    sql.Append(" last_modified = \'" + sToday + "\' ");
                    sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
#endif
                }


                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetNedopsAll : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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


        /// <summary>
        /// Обновить статус наряд-заказа
        /// </summary>
        /// <param name="finder">nzp_zk</param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool UpdateStatusJobOrder(JobOrder finder, int status, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами


                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update " + Points.Pref + "_supg . zakaz set nzp_status = " + status + ", mail_date = now() where nzp_zk = " + finder.nzp_zk + " ");
#else
                sql.Append(" update " + Points.Pref + "_supg : zakaz set nzp_status = " + status + ", mail_date = current where nzp_zk = " + finder.nzp_zk + " ");
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateStatusJobOrder : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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

        /// <summary>
        /// Обновить "данные наряд-заказа"
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateJobOrder(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами


                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                #region Получение локального юзера
                DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update " + Points.Pref + "_supg.zakaz set ");
                sql.Append(" nzp_dest = " + finder.nzp_dest + ", ");
                sql.Append(" nedop_s = \'" + finder.nedop_s + ":00:00" + "\', ");
                sql.Append(" temperature = " + finder.temperature + ", ");
                sql.Append(" nzp_supp = " + finder.nzp_supp + ", ");
                sql.Append(" exec_date = \'" + finder.exec_date + ":00:00" + "\', ");
                sql.Append(" nzp_user = " + local_user + " ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
#else
                sql.Append(" update " + Points.Pref + "_supg:zakaz set ");
                sql.Append(" nzp_dest = " + finder.nzp_dest + ", ");
                sql.Append(" nedop_s = \'" + finder.nedop_s + "\', ");
                sql.Append(" temperature = " + finder.temperature + ", ");
                sql.Append(" nzp_supp = " + finder.nzp_supp + ", ");
                sql.Append(" exec_date = \'" + finder.exec_date + "\', ");
                sql.Append(" nzp_user = " + local_user + " ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + " ");
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                #region Выполнение процедуры формированиия недопоставок
                IDataReader reader = null;
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateJobOrder : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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

        /// <summary>
        /// Процедура формирования списка недопоставок по наряду-заказу
        /// </summary>
        /// <param name="job_ord"></param>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<JobOrder> GetSpisNedop(JobOrder job_ord, out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Данные о недопоставках недоступны, т.к. установлен режим работы с центральным банком данных", -1);
                return new List<JobOrder>();
            }

            List<JobOrder> resList = new List<JobOrder>();
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            IDbConnection conn_db = null;
            ret = Utils.InitReturns();

            if (job_ord.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                //проверка на доступность локального банка
#if PG
                if (!TempTableInWebCashe(conn_db, job_ord.pref + "_data.nedop_kvar"))
                {
                    MonitorLog.WriteLog("Получение списка недопоставок по ЛС Банк данных. " + job_ord.pref + " недоступен", MonitorLog.typelog.Warn, true);
                    return null;
                }
                sql.Append(" select " +
                            " s.service, " +
                            " k.name, " +
                            " nk.tn, " +
                            " nk.dat_s, nk.dat_po, " +
                            " nk.dat_when, nk.month_calc  " +
                            " from " + job_ord.pref + "_data. nedop_kvar nk, " +
                              Points.Pref + "_data.upg_s_kind_nedop k, " +
                              new DbTables(conn_db).services + " s " +
                            " where nk.nzp_kvar = " + job_ord.nzp_kvar + " and nk.act_no = " + job_ord.norm +
                            " and nk.is_actual = 12  " +
                            " and nk.nzp_kind = k.nzp_kind " +
                            " and k.kod_kind = 1 " +
                            " and nk.nzp_serv = s.nzp_serv");
#else
if (!TempTableInWebCashe(conn_db, job_ord.pref + "_data:nedop_kvar"))
                {
                    MonitorLog.WriteLog("Получение списка недопоставок по ЛС Банк данных: " + job_ord.pref + " недоступен", MonitorLog.typelog.Warn, true);
                    return null;
                }
                sql.Append( " select " +
                            " s.service, " + 
                            " k.name, " +    
                            " nk.tn, " +      
                            " nk.dat_s, nk.dat_po, " +  
                            " nk.dat_when, nk.month_calc  " +
                            " from " + job_ord.pref + "_data: nedop_kvar nk, " +
                              Points.Pref + "_data:upg_s_kind_nedop k, " +
                              new DbTables(conn_db).services + " s " +
                            " where nk.nzp_kvar = " + job_ord.nzp_kvar + " and nk.act_no = " + job_ord.norm +
                            " and nk.is_actual = 12  " +
                            " and nk.nzp_kind = k.nzp_kind " +
                            " and k.kod_kind = 1 " +
                            " and nk.nzp_serv = s.nzp_serv");
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetSpisNedop" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        JobOrder jr = new JobOrder();
                        DateTime dat_s;
                        DateTime dat_po;
                        DateTime dat_when;
                        DateTime month_calc;
                        if (reader["service"] != DBNull.Value) jr.service = Convert.ToString(reader["service"]).Trim();
                        else jr.service = "-";

                        if (reader["name"] != DBNull.Value) jr.name = Convert.ToString(reader["name"]).Trim();
                        else jr.name = "-";

                        if (reader["tn"] != DBNull.Value) jr.temperature_str = Convert.ToString(reader["tn"]).Trim();
                        else jr.temperature_str = "-";

                        if (jr.temperature_str == "0")
                            jr.temperature_str = "-";

                        if (reader["dat_s"] != DBNull.Value) jr.dat_s = Convert.ToString(reader["dat_s"]).Trim();
                        else jr.dat_s = "-";

                        if (DateTime.TryParse(jr.dat_s, out dat_s))
                        {
                            jr.dat_s = dat_s.ToString("dd.MM.yyyy HH");
                        }

                        if (reader["dat_po"] != DBNull.Value) jr.dat_po = Convert.ToString(reader["dat_po"]).Trim();
                        else jr.dat_po = "-";

                        if (DateTime.TryParse(jr.dat_po, out dat_po))
                        {
                            jr.dat_po = dat_po.ToString("dd.MM.yyyy HH");
                        }

                        if (reader["dat_when"] != DBNull.Value) jr.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                        else jr.dat_when = "-";

                        if (DateTime.TryParse(jr.dat_when, out dat_when))
                        {
                            jr.dat_when = dat_when.ToString("dd.MM.yyyy HH");
                        }

                        if (reader["month_calc"] != DBNull.Value) jr.month_calc = Convert.ToString(reader["month_calc"]).Trim();
                        else jr.month_calc = "-";

                        if (DateTime.TryParse(jr.month_calc, out month_calc))
                        {
                            jr.month_calc = month_calc.ToString("MM.yyyy");
                        }

                        resList.Add(jr);
                    }
                }
                return resList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetSpisNedop : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }


        /// <summary>
        /// Сохраняет данные блока Формирование недопоставки
        /// </summary>
        /// <param name="finder">ds_actual, ds</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool SaveMakeNedop(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами


                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                #region Получение локального юзера
                DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetSupgUser(con_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();
                #endregion
#if PG
                sql.Remove(0, sql.Length);
                sql.Append(" update  " + Points.Pref + "_supg. zakaz");
                sql.Append(" set ds_actual = " + finder.ds_actual + ", ");
                sql.Append(" ds_date =  '" + finder.ds_date + "', ");
                sql.Append(" ds_user = " + local_user + " ");
                sql.Append(" where nzp_zk =  " + finder.nzp_zk);
#else
                sql.Remove(0, sql.Length);
                sql.Append(" update  " + Points.Pref + "_supg: zakaz");
                sql.Append(" set ds_actual = " + finder.ds_actual + ", ");
                sql.Append(" ds_date =  '" + finder.ds_date + "', " );
                sql.Append(" ds_user = " + local_user + " ");
                sql.Append(" where nzp_zk =  " + finder.nzp_zk);
#endif

                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                #region Выполнение процедуры формированиия недопоставок
                IDataReader reader = null;
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveMakeNedop : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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


        /// <summary>
        /// формирование недопоставки по номеру наряда-заказа
        /// </summary>
        /// <param name="finder">nzp_user, pref, norm</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool ExecGenZakazNedop(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            return true;

            /*
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();
            DateTime date = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_));

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                #region Получение локального юзера

                DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(con_db, new Finder() { nzp_user = finder.nzp_user}, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();

                #endregion
                
                sql.Remove(0, sql.Length);
                string sToday = Utils.GetSupgCurDate("D", "");                
 #if PG
 sql.Append("execute PROCEDURE " + finder.pref + "_data.gen_zakaz_nedop(" + finder.norm + ", " + finder.nzp_serv + ", " + 
                            finder.act_num_nedop + ", mdy(" + date.Month + "," + date.Day + "," + date.Year + "), " + local_user +", "+
                            "\'" + sToday + "\')");
#else
 sql.Append("execute PROCEDURE " + finder.pref + "_data:gen_zakaz_nedop(" + finder.norm + ", " + finder.nzp_serv + ", " + 
                            finder.act_num_nedop + ", mdy(" + date.Month + "," + date.Day + "," + date.Year + "), " + local_user +", "+
                            "\'" + sToday + "\')");
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры gen_zakaz_nedop : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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
            } */
        }

        /// <summary>
        /// Процедура получения справочника "Дополнительные отметки"
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>словарь отметок</returns>
        public Dictionary<int, string> GetAnswers(out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            Dictionary<int, string> retDic = new Dictionary<int, string>();
            retDic.Add(-1, "<Не выбрано>");

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetAnswers : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select  nzp_answer, name_answer from " + Points.Pref + "_supg .  s_answer ");
#else
 sql.Append(" select  nzp_answer, name_answer from " + Points.Pref + "_supg :  s_answer ");
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

                        int nzp_answer = -1;
                        string name_answer = "";

                        if (reader["nzp_answer"] != DBNull.Value) nzp_answer = Convert.ToInt32(reader["nzp_answer"]);
                        if (reader["name_answer"] != DBNull.Value) name_answer = Convert.ToString(reader["name_answer"]);

                        retDic.Add(nzp_answer, name_answer);

                    }
                }

                return retDic;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAnswers : " + ex.Message, MonitorLog.typelog.Error, true);
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

        public List<SupgAct> GetActs(SupgActFinder finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_supg_acts = "t" + Convert.ToString(finder.nzp_user) + "_supg_acts";
#if PG
            string sql = "select count(*) from " + tXX_supg_acts;
#else
string sql = "select count(*) from " + tXX_supg_acts;
#endif
            object obj = ExecScalar(conn_web, sql, out ret, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            int quantity;
            try { quantity = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetActs:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
                conn_web.Close();
                return null;
            }
#if PG
            sql = "select " + (finder.skip > 0 ? " offset " + finder.skip : "") + " * from " + tXX_supg_acts;
#else
            sql = "select "+ (finder.skip > 0 ? " skip " + finder.skip : "") + " * from " + tXX_supg_acts;
#endif
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            SupgAct act;
            List<SupgAct> list = new List<SupgAct>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    act = new SupgAct();
                    if (reader["nzp_act"] != DBNull.Value) act.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                    if (reader["number"] != DBNull.Value) act.number = reader["number"].ToString().Trim();
                    if (reader["_date"] != DBNull.Value) act._date = Convert.ToDateTime(reader["_date"]).ToShortDateString();
                    if (reader["plan_number"] != DBNull.Value) act.plan_number = reader["plan_number"].ToString().Trim();
                    if (reader["plan_date"] != DBNull.Value) act.plan_date = Convert.ToDateTime(reader["plan_date"]).ToShortDateString();
                    if (reader["nzp_supp"] != DBNull.Value) act.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["supplier"] != DBNull.Value) act.supplier = reader["supplier"].ToString().Trim();
                    if (reader["nzp_serv"] != DBNull.Value) act.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) act.service = reader["service"].ToString().Trim();
                    if (reader["dat_s"] != DBNull.Value) act.dat_s = Convert.ToDateTime(reader["dat_s"]).ToString("dd.MM.yyyy HH:mm");
                    if (reader["dat_po"] != DBNull.Value) act.dat_po = Convert.ToDateTime(reader["dat_po"]).ToString("dd.MM.yyyy HH:mm");
                    if (reader["comment"] != DBNull.Value) act.comment = reader["comment"].ToString().Trim();
                    list.Add(act);

                    if (i >= finder.rows) break;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetActs:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
            }
            finally
            {
                CloseReader(ref reader);
                conn_web.Close();
            }
            if (ret.result) ret.tag = quantity;
            return list;
        }

        /// <summary>
        /// Получить информацию о проводимых работах по данному ЛС(дому)
        /// </summary>        
        /// <returns>список работ</returns>
        public List<SupgAct> GetPlannedWorks(SupgAct finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            List<SupgAct> retList = new List<SupgAct>();

            if (finder.nzp_kvar == Convert.ToInt32(Constants.NzpEmptyAddress)) return retList;

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetPlannedWorks : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

#if PG
                string sToday = Utils.GetSupgCurDate("D", "s");
#else
                string sToday = Utils.GetSupgCurDate("T", "H");
#endif

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select s.nzp_serv, s.service , a.nzp_act , a.plan_number ,a.dat_s, a.dat_po, sp.name_supp, a.comment, w.name_work_type ");
                sql.Append(" from " + new DbTables(con_db).services + " s, ");
                sql.Append( Points.Pref + "_supg. act a ");

                sql.Append(" left outer join " + new DbTables(con_db).supplier + " sp on   sp.nzp_supp = a.nzp_supp , ");
                sql.Append(" " + Points.Pref + "_supg. act_obj ob, ");
                sql.Append(" " + Points.Pref + "_supg. s_work_type w, ");
                sql.Append(" " + new DbTables(con_db).dom + " d ");
                sql.Append(" where a.nzp_serv = s.nzp_serv  ");
                sql.Append(" and w.nzp_work_type = a.nzp_work_type ");
                sql.Append(" and a.nzp_act = ob.nzp_act ");
                sql.Append(" and  d.nzp_dom = " + finder.nzp_dom);
                sql.Append(" and (ob.nzp_ul = d.nzp_ul ");
                sql.Append(" and  (ob.nzp_dom = " + finder.nzp_dom + " or ob.nzp_dom=0 )");
                sql.Append(" and  (ob.nzp_kvar = " + finder.nzp_kvar + " or ob.nzp_kvar = 0))");
                
                if (finder.dat_s != "")
                {
                    sql.Append(" and a.dat_s<= \'" + Regex.Replace(finder.dat_s, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\' ");
                    sql.Append(" and (a.dat_po>=\'" + Regex.Replace(finder.dat_s, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\' or a.dat_po is null) ");
                }

                else
                {
                    sql.Append(" and a.dat_s<= \'" + Regex.Replace(sToday, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\' ");
                    sql.Append(" and (a.dat_po>=\'" + Regex.Replace(sToday, "(?<=\\d{4}.\\d{2}.\\d{2} \\d{2}$)", ":00:00") + "\' or a.dat_po is null) ");
                }
                sql.Append(" order by a.dat_po desc ");
#else
sql.Append(" select s.nzp_serv, s.service , a.nzp_act , a.plan_number ,a.dat_s, a.dat_po, sp.name_supp, a.comment, w.name_work_type ");
                sql.Append(" from " + Points.Pref + "_supg: act a, ");
                sql.Append(" " + new DbTables(con_db).services + " s, ");
                sql.Append(" outer(" + new DbTables(con_db).supplier + " sp), ");
                sql.Append(" " + Points.Pref + "_supg: act_obj ob, ");
                sql.Append(" " + Points.Pref + "_supg: s_work_type w, ");
                sql.Append(" " + new DbTables(con_db).dom + " d ");
                sql.Append(" where a.nzp_serv = s.nzp_serv ");
                sql.Append(" and sp.nzp_supp = a.nzp_supp ");
                sql.Append(" and w.nzp_work_type = a.nzp_work_type ");
                sql.Append(" and a.nzp_act = ob.nzp_act ");
                sql.Append(" and  d.nzp_dom = " + finder.nzp_dom );
                sql.Append(" and (ob.nzp_ul = d.nzp_ul ");
                sql.Append(" and  (ob.nzp_dom = " + finder.nzp_dom + " or ob.nzp_dom=0 )");
                sql.Append(" and  (ob.nzp_kvar = " + finder.nzp_kvar + " or ob.nzp_kvar = 0))");
                if (finder.dat_s != "")
                {
                    sql.Append(" and a.dat_s<= \'" + finder.dat_s + "\' ");
                    sql.Append(" and (a.dat_po>=\'" + finder.dat_s + "\' or a.dat_po is null) ");
                }
                else
                {
                    sql.Append(" and a.dat_s<= \'" + sToday + "\' ");
                    sql.Append(" and (a.dat_po>=\'" + sToday + "\' or a.dat_po is null) ");
                }
                sql.Append(" order by a.dat_po desc ");
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
                        SupgAct sact = new SupgAct();
                        if (reader["nzp_serv"] != DBNull.Value) sact.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["nzp_act"] != DBNull.Value) sact.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                        if (reader["plan_number"] != DBNull.Value) sact.plan_number = Convert.ToString(reader["plan_number"]);
                        if (reader["service"] != DBNull.Value) sact.service = Convert.ToString(reader["service"]);
                        if (reader["dat_s"] != DBNull.Value) sact.dat_s = Convert.ToString(reader["dat_s"]);
                        if (reader["dat_po"] != DBNull.Value) sact.dat_po = Convert.ToString(reader["dat_po"]);
                        if (reader["name_supp"] != DBNull.Value) sact.supplier = Convert.ToString(reader["name_supp"]);
                        if (reader["comment"] != DBNull.Value) sact.comment = sact.comment = Convert.ToString(reader["comment"]);
                        if (reader["name_work_type"] != DBNull.Value) sact.name_work_type = Convert.ToString(reader["name_work_type"]);

                        retList.Add(sact);
                    }
                }

                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPlannedWorks : " + ex.Message + ", Trace:" + ex.StackTrace + " ,  Source:" + ex.Source, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }
                CloseReader(ref reader);
                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Проверка возмождно ли открыть заказ или
        /// н-з на редактирование (блокировка пользователем)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ord"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CheckToClose(JobOrder finder, string ord, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;


            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("CheckToClose : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

#if PG
                switch (ord)
                {
                    case "zvk":
                        {
                            sql.Append(" select * from " + Points.Pref + "_supg.lock_zvk (" + finder.nzp_user + ", " + finder.nzp_zvk + ");");
                            break;
                        }
                    case "zk":
                        {
                            sql.Append(" select * from " + Points.Pref + "_supg.lock_zk (" + finder.nzp_user + ", " + finder.nzp_zk + ");");
                            break;
                        }
                }
#else
                switch (ord)
                {
                    case "zvk":
                        {
                            sql.Append("execute procedure " + Points.Pref + "_supg: lock_zvk (" + finder.nzp_user +", " + finder.nzp_zvk +  ");");
                            break;
                        }
                    case "zk":
                        {
                            sql.Append("execute procedure " + Points.Pref + "_supg: lock_zk (" + finder.nzp_user + ", " + finder.nzp_zk + ");");
                            break;
                        }
                }
#endif



                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }


                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader[0] != DBNull.Value)
                        {
                            return Convert.ToInt16(reader[0]) > 0 ? true : false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CheckToClose : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }
                CloseReader(ref reader);
                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Сохранить дату согласования сроков(документ) н-з
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateDocumentPeriod(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            //IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            #if PG
            //проверка на входные данные
            if (String.IsNullOrEmpty(finder.document_date) || String.IsNullOrEmpty(finder.document_date))
            {
                //todo возможно следует обработать ситуацию как особую
                return true;
            }
            #endif

            try
            {
                #region Открываем соединение с базами

                //con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                //ret = OpenDb(con_web, true);
                //if (ret.result)
                //{
                ret = OpenDb(con_db, true);
                //}
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                //#region Получение локального юзера
                //DbWorkUser dbU = new DbWorkUser();
                //int local_user = dbU.GetLocalUser(con_db, new Finder() { nzp_user = finder.nzp_user}, out ret);
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                //    return false;
                //}
                //dbU.Close();
                //#endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update " + Points.Pref + "_supg.zakaz set  ");
                sql.Append(" plan_date = '" + finder.document_date + "' ");
                sql.Append(" ,control_date = '" + finder.document_controlDate + "' ");
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
#else
sql.Append(" update " + Points.Pref + "_supg:zakaz set  ");                
                sql.Append(" plan_date = '" + finder.document_date + "' ");                               
                sql.Append(" ,control_date = '" + finder.document_controlDate + "' ");                
                sql.Append(" where nzp_zk = " + finder.nzp_zk + "; ");
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления контрольного срока в документе " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }


                //#region Выполнение процедуры формированиия недопоставок
                //int norm = 0;
                //int nzp_serv = 0;
                //int num_nedop = 0;

#if PG
                //string sqlStr = "select z.norm, d.nzp_serv, d.num_nedop ";
                //sqlStr += " from " + pref + "_data.zakaz z, ";
                //sqlStr += " " + Points.Pref + "_data. s_dest d ";
                //sqlStr += " where z.nzp_dest = d.nzp_dest ";
                //sqlStr += " and z.nzp_zk = " + finder.nzp_zk + " ";
#else
                //string sqlStr = "select z.norm, d.nzp_serv, d.num_nedop ";
                //sqlStr += " from " + pref + "_data:zakaz z, ";
                //sqlStr += " " + Points.Pref + "_data: s_dest d ";
                //sqlStr += " where z.nzp_dest = d.nzp_dest ";
                //sqlStr += " and z.nzp_zk = " + finder.nzp_zk + " ";
#endif

                //if (!ExecRead(con_db, out reader, sqlStr, true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки norm " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}
                //if (reader.Read())
                //{
                //    if (reader["norm"] != DBNull.Value) norm = Convert.ToInt32(reader["norm"]);
                //    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                //    if (reader["num_nedop"] != DBNull.Value) num_nedop = Convert.ToInt32(reader["num_nedop"]);
                //}
                ////процедура
                //JobOrder fproc = new JobOrder();
                //fproc.nzp_user = finder.nzp_user;
                //fproc.norm = norm;
                //fproc.nzp_serv = nzp_serv;
                //fproc.act_num_nedop = num_nedop;

                //bool resProc = this.ExecGenZakazNedop(fproc, out ret);
                //if (!resProc)
                //{
                //    MonitorLog.WriteLog("Ошибка выполнения процедуры gen_zakaz_nedop : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}

                //#endregion


                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateDocumentPeriod : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                //if (con_web != null)
                //{
                //    con_web.Close();
                //}

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Процедура получения справочника s_work_type
        /// </summary>
        /// <returns></returns>
        public List<SupgAct> GetWorksTypes(out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<SupgAct> retList = new List<SupgAct>() 
            { 
                new SupgAct() { nzp_work_type = -1, name_work_type = "<не выбрано>" } 
            };

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
                sql.Append(" select  nzp_work_type, name_work_type from " + Points.Pref + "_supg. s_work_type ");
#else
sql.Append(" select  nzp_work_type, name_work_type from " + Points.Pref + "_supg: s_work_type ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения справочника плановых работ " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        SupgAct sa = new SupgAct();

                        if (reader["nzp_work_type"] != DBNull.Value) sa.nzp_work_type = Convert.ToInt32(reader["nzp_work_type"]);
                        if (reader["name_work_type"] != DBNull.Value) sa.name_work_type = Convert.ToString(reader["name_work_type"]);

                        retList.Add(sa);
                    }
                }

                return retList; ;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetWorksTypes : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                //if (con_web != null)
                //{
                //    con_web.Close();
                //}

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Добавление плановой работы
        /// </summary>
        /// <param name="finder">параметры добавления</param>
        /// <param name="ret">результат</param>
        /// <returns>результат</returns>
        public bool AddPlannedWork(ref SupgAct finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                //дата
#if PG
                string sToday = Utils.GetSupgCurDate("D", "m");
#else
                string sToday = Utils.GetSupgCurDate("D", "");
#endif



                //назначение номера документа, если номер документа пустой                
                if (finder.plan_number == "")
                {
                    DbEditInterData dbEID = new DbEditInterData();
                    int plan_number = dbEID.GetSupgSeriesProc(16, out ret);
                    dbEID.Close();
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка назначения номера документа) : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }
                    if (plan_number != 0) finder.plan_number = plan_number.ToString();
                }
                // Назначить текущую дату, если дата документа пустая
                if (finder.plan_date == "") finder.plan_date = sToday;
                // назначить номер акта, если номер акта пустой
                if (finder.is_actual != 100)
                {

                    if (finder.number.Trim() == "")
                    {
                        DbEditInterData dbEID = new DbEditInterData();
                        int number = dbEID.GetSupgSeriesProc(17, out ret);
                        dbEID.Close();
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка назначения номера акта) : " + ret.text, MonitorLog.typelog.Error, true);
                            return false;
                        }
                        if (number != 0) finder.number = number.ToString();
                    }
                    // Назначить текущую дату, если дата акта пустая
                    if (finder._date == "") finder._date = sToday;

                }


                //Экранирование символов
                finder.comment = this.SymbolsScreening(finder.comment);
                finder.reply_comment = this.SymbolsScreening(finder.reply_comment);


                //добавление плановой работы
                sql.Remove(0, sql.Length);

                
#if PG
                sql.Append(" insert into  " + Points.Pref + "_supg . act ");
                sql.Append(" (plan_number , plan_date,nzp_supp_plant,nzp_work_type,nzp_serv,nzp_kind,dat_s,dat_po,nzp_supp,comment, ");
                sql.Append(" is_actual, _date , number ,  reply_date ,reply_comment, tn, registration_date, last_modified )");
                sql.Append(" values('" + finder.plan_number + "','" + finder.plan_date + "'," + finder.nzp_supp_plant + ", ");
                sql.Append(finder.nzp_work_type + "," + finder.nzp_serv + "," + finder.nzp_kind + ",'" + finder.dat_s + ":00:00");
                sql.Append("','" + finder.dat_po + ":00:00" + "'," + finder.nzp_supp + ",'" + finder.comment + "', ");

                //TODO: ошибка в синтаксисе даты. В finder.reply_date что надо положить? null, дату какую-либо.
                sql.Append(finder.is_actual + ", " + Utils.EDateNull(finder._date) + ", '" + finder.number + "', " + Utils.EDateNull(finder.reply_date) + ", '" + finder.reply_comment + "', " + finder.tn + ", now(), now());");


#else
 sql.Append(" insert into  " + Points.Pref + "_supg : act ");
                sql.Append(" (plan_number , plan_date,nzp_supp_plant,nzp_work_type,nzp_serv,nzp_kind,dat_s,dat_po,nzp_supp,comment, ");
                sql.Append(" is_actual, _date , number ,  reply_date ,reply_comment, tn )");
                sql.Append(" values('" + finder.plan_number + "','" + finder.plan_date + "'," + finder.nzp_supp_plant +", ");
                sql.Append(finder.nzp_work_type + "," + finder.nzp_serv + "," + finder.nzp_kind + ",'" + finder.dat_s );
                sql.Append("','" + finder.dat_po + "'," + finder.nzp_supp + ",'" + finder.comment + "', ");
                sql.Append(finder.is_actual + ", '" + finder._date + "', '" + finder.number + "', '" + finder.reply_date + "', '" + finder.reply_comment + "', " + finder.tn +");");
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка добавления плановых работ " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                //nzp_act
                int nzp_act = GetSerialValue(con_db);
                //вернуть nzp_act
                ret.tag = nzp_act;
                //ret.text = finder.plan_number.Trim() + ";" + finder.plan_date.Trim() + ";" + finder.number.Trim() + ";" + finder._date.Trim() + ";";

                //цикл по адресам
                foreach (Ls ls in finder.adrList)
                {

                    //добавление плановой работы по адресу
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into " + Points.Pref + "_supg .  act_obj ( nzp_act, nzp_ul, nzp_dom, nzp_kvar) ");
                    sql.Append(" values (" + nzp_act + ",");
                    sql.Append(ls.nzp_ul + "," + ls.nzp_dom + "," + ls.nzp_kvar + "); ");
#else
sql.Append(" insert into " + Points.Pref + "_supg :  act_obj ( nzp_act, nzp_ul, nzp_dom, nzp_kvar) ");
                    sql.Append(" values (" + nzp_act + ",");
                    sql.Append(ls.nzp_ul + "," + ls.nzp_dom + "," + ls.nzp_kvar + "); ");
#endif

                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления адресов плановых работ " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }

                    //#region Получение локального юзера
                    //DbWorkUser dbU = new DbWorkUser();
                    //int nzp_User = dbU.GetLocalUser(con_db, new Finder() { nzp_user = finder.nzp_user, pref =  }, out ret);
                    //if (!ret.result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    //    ret.result = false;
                    //    return false;
                    //}
                    //dbU.Close();
                    //#endregion
                    //string sToday = Utils.GetSupgCurDate("D", "");
                    ////добавление недопоставки по адресу из списка                    
#if PG
                    //sql.Remove(0, sql.Length);
                    //sql.Append(" execute procedure " + Points.Pref + "_data .  gen_act_nedop ( ");
                    //sql.Append( nzp_act.ToString()+ ","+ ls.nzp_dom + "," + ls.nzp_kvar +","+finder.is_actual );
                    //sql.Append( ","+ finder.nzp_serv + "," + finder.nzp_kind + "," + finder.tn.ToString() + ",\'" + finder.dat_s + "\'");
                    //sql.Append(", \'" + finder.dat_po + "\', mdy(" + Points.CalcMonth.month_.ToString() + "," + 
                    //                                               DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_).ToString() + "," +
                    //                                               Points.CalcMonth.year_.ToString() + "), " + nzp_User);
                    //sql.Append(", \'" + sToday + "\' )");
#else
 //sql.Remove(0, sql.Length);
                    //sql.Append(" execute procedure " + Points.Pref + "_data :  gen_act_nedop ( ");
                    //sql.Append( nzp_act.ToString()+ ","+ ls.nzp_dom + "," + ls.nzp_kvar +","+finder.is_actual );
                    //sql.Append( ","+ finder.nzp_serv + "," + finder.nzp_kind + "," + finder.tn.ToString() + ",\'" + finder.dat_s + "\'");
                    //sql.Append(", \'" + finder.dat_po + "\', mdy(" + Points.CalcMonth.month_.ToString() + "," + 
                    //                                               DateTime.DaysInMonth(Points.CalcMonth.year_, Points.CalcMonth.month_).ToString() + "," +
                    //                                               Points.CalcMonth.year_.ToString() + "), " + nzp_User);
                    //sql.Append(", \'" + sToday + "\' )");
#endif

                    //if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    //{
                    //    MonitorLog.WriteLog("Ошибка формирования недопоставок по плановым работам " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //    ret.result = false;
                    //    return false;
                    //}
                }
                // очистить список 
                finder.adrList = new List<Ls>();

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddPlannedWork : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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
    }
}
