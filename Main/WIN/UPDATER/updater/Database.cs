using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using IBM.Data.Informix;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Windows.Forms;
using System.Diagnostics;

namespace updater
{
    class Database : DataBaseHead
    {
        public ArrayList Get_rajon_info(string connectionStr, string SqlStr, int check)
        {
            System.Collections.ArrayList RajList = new System.Collections.ArrayList();
            
            Returns ret = Utils.InitReturns();
            IfxConnection conn_db = GetConnection(connectionStr);// new IfxConnection(connectionStr);
            IfxDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            StringBuilder sql = new StringBuilder();

            sql.Append(SqlStr);

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                reader.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Info info = new Info();
                    switch(check)
                    {
                       case 0:
                            {
                                if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                                if (reader["update_status"] != DBNull.Value) info.update_status = Convert.ToString(reader["update_status"]).Trim();
                                if (reader["update_type"] != DBNull.Value) info.update_type = Convert.ToString(reader["update_type"]).Trim();
                                if (reader["update_version"] != DBNull.Value) info.update_version = Convert.ToString(reader["update_version"]).Trim();
                                if (reader["update_date"] != DBNull.Value) info.update_date = Convert.ToString(reader["update_date"]).Trim();
                                RajList.Add(info);
                                break;
                            }
                       case 1:
                            {
                                if (reader["rajon_number"] != DBNull.Value) info.rajon_number = Convert.ToString(reader["rajon_number"]).Trim();
                                if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                                if (reader["rajon_ip"] != DBNull.Value) info.rajon_ip = Convert.ToString(reader["rajon_ip"]).Trim();
                                RajList.Add(info);
                                break;
                            }
                       case 2:
                          {
                                if (reader["update_report"] != DBNull.Value) info.update_report = Convert.ToString(reader["update_report"]).Trim();
                                RajList.Add(info);
                                break;
                          }
                       case 3:
                          {
                              if (reader["rajon_basename"] != DBNull.Value) info.rajon_basename = Convert.ToString(reader["rajon_basename"]).Trim();
                              RajList.Add(info);
                              break;
                          }
                       case 4:
                          {
                              if (reader["rajon_number"] != DBNull.Value) info.rajon_number = Convert.ToString(reader["rajon_number"]).Trim();
                              if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                              if (reader["rajon_ip"] != DBNull.Value) info.rajon_ip = Convert.ToString(reader["rajon_ip"]).Trim();
                              if (reader["rajon_login"] != DBNull.Value) info.rajon_login = Convert.ToString(reader["rajon_login"]).Trim();
                              if (reader["rajon_password"] != DBNull.Value) info.rajon_password = Convert.ToString(reader["rajon_password"]).Trim();
                              if (reader["rajon_basename"] != DBNull.Value) info.rajon_basename = Convert.ToString(reader["rajon_basename"]).Trim();
                              if (reader["rajon_webpath"] != DBNull.Value) info.rajon_webpath = Convert.ToString(reader["rajon_webpath"]).Trim();
                              RajList.Add(info);
                              break;
                          }
                       case 5:
                          {
                              if (reader["rajon_number"] != DBNull.Value) info.rajon_number = Convert.ToString(reader["rajon_number"]).Trim();
                              if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                              if (reader["rajon_ip"] != DBNull.Value) info.rajon_ip = Convert.ToString(reader["rajon_ip"]).Trim();
                              RajList.Add(info);
                              break;
                          }
                        default:
                          {
                                if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                                if (reader["update_version"] != DBNull.Value) info.update_version = Convert.ToString(reader["update_version"]).Trim();
                                RajList.Add(info);
                                break;
                          }
                    }

                }
                reader.Close();
                sql.Remove(0, sql.Length);

                return RajList;
            }
            finally
            {
                conn_db.Close();
            }
        }


        //процедура записи в БД
        public void  WriteUpdate(string connectionStr, string SqlStr, string report)
        {
            Returns ret = Utils.InitReturns();     
            ret = Utils.InitReturns();
            IfxConnection i_connect = GetConnection(connectionStr);

            #region Подключение к БД

            Database DB = new Database();
            ret = DB.OpenDb(i_connect, true);
            if (!ret.result)
            {
                MessageBox.Show("Ошибка открытия базы данных!");
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            sql.Append(SqlStr);

            #region Выполнение запроса на обновление
            //Выполнение запроса            
            ret = Utils.InitReturns();
            try
            {
                IfxCommand cmd = new IfxCommand(sql.ToString(), i_connect);
                cmd.Parameters.Add("@binaryValue", IfxType.Text).Value = report;
                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса!" + sql.ToString() + ex.Message);
            }
            finally
            {
                i_connect.Close();
            }
            #endregion
        }

        public List<PHPInfo> Get_PHP_Info(string connectionStr, string SqlStr)
        {
            List<PHPInfo> retList = new List<PHPInfo>();
            Returns ret = Utils.InitReturns();
            IfxConnection conn_db = GetConnection(connectionStr);
            IfxDataReader reader;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            StringBuilder sql = new StringBuilder();

            sql.Append(SqlStr);

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                reader.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    PHPInfo info = new PHPInfo();

                    if (reader["rajon_number"] != DBNull.Value) info.rajon_number = Convert.ToInt32(reader["rajon_number"]);
                    if (reader["rajon_name"] != DBNull.Value) info.rajon_name = Convert.ToString(reader["rajon_name"]).Trim();
                    if (reader["dat_when"] != DBNull.Value) info.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                    if (reader["dat_pgu"] != DBNull.Value) info.dat_pgu = Convert.ToString(reader["dat_pgu"]).Trim();
                    if (reader["cnt_access"] != DBNull.Value) info.cnt_access = Convert.ToString(reader["cnt_access"]).Trim();
                    if (reader["cnt_ls"] != DBNull.Value) info.cnt_ls = Convert.ToString(reader["cnt_ls"]).Trim();
                    if (reader["cnt_device"] != DBNull.Value) info.cnt_device = Convert.ToString(reader["cnt_device"]).Trim();
                    if (reader["cnt_faktura"] != DBNull.Value) info.cnt_faktura = Convert.ToString(reader["cnt_faktura"]).Trim();
                    if (reader["cnt_pay"] != DBNull.Value) info.cnt_pay = Convert.ToString(reader["cnt_pay"]).Trim();
                    if (reader["cnt_access_day"] != DBNull.Value) info.cnt_access_day = Convert.ToString(reader["cnt_access_day"]).Trim();
                    if (reader["cnt_ls_day"] != DBNull.Value) info.cnt_ls_day = Convert.ToString(reader["cnt_ls_day"]).Trim();
                    if (reader["cnt_device_day"] != DBNull.Value) info.cnt_device_day = Convert.ToString(reader["cnt_device_day"]).Trim();
                    if (reader["cnt_faktura_day"] != DBNull.Value) info.cnt_faktura_day = Convert.ToString(reader["cnt_faktura_day"]).Trim();
                    if (reader["cnt_pay_day"] != DBNull.Value) info.cnt_pay_day = Convert.ToString(reader["cnt_pay_day"]).Trim();
                    retList.Add(info);

                }
                reader.Close();
                sql.Remove(0, sql.Length);

                return retList;
            }
            finally
            {
                conn_db.Close();
            }
        }

        public void Insert_PHP_info(string connectionStr, PHPInfo info)
        {
            Returns ret = Utils.InitReturns();
            ret = Utils.InitReturns();
            IfxConnection i_connect = GetConnection(connectionStr);

            Database DB = new Database();
            ret = DB.OpenDb(i_connect, true);
            if (!ret.result)
            {
                MessageBox.Show("Ошибка открытия базы данных!");
                i_connect.Close();
                return;
            }

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO rajon_execphp VALUES (0," +
                info.rajon_number + ", \'" +
                info.rajon_name + "\', \'" +
                info.dat_when + "\', \'" +
                info.dat_pgu + "\', " +
                info.cnt_access + ", " +
                info.cnt_ls + ", " +
                info.cnt_device + ", " +
                info.cnt_faktura + ", " +
                info.cnt_pay + ", " +
                info.cnt_access_day + ", " +
                info.cnt_ls_day + ", " +
                info.cnt_device_day + ", " +
                info.cnt_faktura_day + ", " +
                info.cnt_pay_day + ")");

            ret = Utils.InitReturns();
            try
            {
                IfxCommand cmd = new IfxCommand(sql.ToString(), i_connect);
                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении информации: " + sql.ToString() + ex.Message);
            }
            finally
            {
                i_connect.Close();
            }
        }
    }
}
