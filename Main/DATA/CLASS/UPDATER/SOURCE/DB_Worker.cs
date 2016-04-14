using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

using System.Data;

namespace STCLINE.KP50.Updater
{
    public class DB_Worker : DataBaseHead
    {
        private string cons_Webdata;        
        
        private string cur_pref;
        //статическое поле для статической процедуры
        public static string TypeUpdate;
        //не стат поле
        public string typeUpdate;

        public string UpdInd;

        #region Методы доступа
        public string Cons_Webdata
        {
            set { this.cons_Webdata = value;}
            get {return this.cons_Webdata;}
        }

        public string Cur_pref
        {
            set { this.cur_pref = value; }
            get { return this.cur_pref; }
        }
        #endregion
        public DB_Worker(string ConnectionString, string UpdateIndex) : base()
        {
            this.cons_Webdata = ConnectionString;
            this.UpdInd = UpdateIndex;
        }
        public DB_Worker() : base()
        {

        }

        //Процедура записи в банку
        public Returns WriteReportUpdate(UpData upd, string sqlString, int status , ref string LOGSTR)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection i_connect;
            string LogString = "";
            

            #region Подключение к БД
            try
            {
                LogString += " \r\n\r\nПопытка подключени к БД...";
               i_connect = GetConnection(this.cons_Webdata);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }

            ret = OpenDb(i_connect, true);
            if (!ret.result)
            {
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            #endregion
            LogString += "OK";
            StringBuilder sql = new StringBuilder();
            if (sqlString == "")
            {
                sql.Append(" UPDATE  updater SET status = " + status + " where nzp_up = " + "\'" + upd.nzp + "\' and status = 0 and typeup = \'" + this.typeUpdate +"\';");// 'TestVersion'; );  
            }
            else
            {
                sql.Append(sqlString);
            }
            
            #region Выполнение запроса на обнавление
            IDbTransaction trans;
            try
            {
                trans = i_connect.BeginTransaction();
            }
            catch
            {
                trans = null;
            }
            //Выполнение запроса            
            ret = Utils.InitReturns();   
            try
            {
                LogString += "\r\n\r\nПопытка выполнить запрос...";               
                
                IDbCommand cmd = null;
                if (trans != null)
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect, trans);
                }
                else
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect);
                }

                int res = cmd.ExecuteNonQuery();
                if (trans != null)
                {
                    trans.Commit();
                }
                i_connect.Close();
                sql.Remove(0, sql.Length);                
                ret.text = "Обновлено строк: " + res;
                LogString += "OK";
                try
                {
                    Loger.WriteInfo(LogString, ref LOGSTR);
                }
                catch (Exception exp)
                {
                    MonitorLog.WriteLog("19" + exp.Message + "/r/n/r/n" + exp.StackTrace, MonitorLog.typelog.Error, true);
                }
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\r\n\r\nОшибка зписи в базу данных ";
                ret.sql_error = " БД " + i_connect.Database + " \n '" + sql.ToString() + "' \n " + ex.Message;
                string err = ret.text + " \n " + ret.sql_error;
                LogString += err;
                Loger.WriteInfo(LogString, ref LOGSTR);
                MonitorLog.WriteLog("15"+err, MonitorLog.typelog.Error, 1, 3, true);

                if (Constants.Viewerror)
                {
                   ret.text = err;
                }

                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Rollback();
                }
                i_connect.Close();
              
                return ret;
            }
            #endregion
            
            #region Код обновления для отладки
            //try
            //{
            //    IDbCommand cmd = DBManager.newDbCommand(sql.ToString(), i_connect);                
            //    int res = cmd.ExecuteNonQuery();
            //    i_connect.Close();
            //    return res;
            //}
            //catch (Exception ex)
            //{
            //    //Ошибка выполнения транзакции
            //    i_connect.Close();
            //    return -1;
            //}
            #endregion
        }

        //Данные для скачивания обновления
        public ArrayList GetPathUpdate(out Returns ret, string connStr)
        {
            DbPatch db = new DbPatch();
            ArrayList res = null;
            if (TypeUpdate != "")
            {
                res = db.CheckForUpdates(out ret, connStr, TypeUpdate);
            }
            else
            {
                res = db.CheckForUpdates(out ret, connStr, "host");
            }
            return res;
        }
       
        //Процедура отметки следующих обновлений как недостижимых,
        //если текущее обновление завершилдось неудачей
        public Returns CheckNextUpdates(UpData upd , ref string LOGSTR)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection i_connect;
            string LogString = "";


            #region Подключение к БД
            try
            {
                LogString += " \r\n\r\nПопытка подключени к БД...";
                i_connect = GetConnection(this.cons_Webdata);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }

            ret = OpenDb(i_connect, true);
            if (!ret.result)
            {
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            #endregion
            LogString += "OK";
            StringBuilder sql = new StringBuilder();           
           
            sql.Append(" UPDATE  updater SET status = 4 where nzp_up = " + upd.nzp + " and status = 0;");
            
            #region Выполнение запроса на обнавление
            //Выполнение запроса    
            IDbTransaction trans;
            try
            {
                trans = i_connect.BeginTransaction();
            }
            catch(Exception)
            {
                trans = null;
            }
            ret = Utils.InitReturns();
            try
            {
                LogString += "\r\n\r\nПопытка выполнить запрос...";
                IDbCommand cmd = null;
                if (trans != null)
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect, trans);
                }
                else
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect);
                }
                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Commit();
                }              
                i_connect.Close();
                ret.text = "Обновлено строк: " + res;
                LogString += "OK";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\r\n\r\nОшибка зписи в базу данных ";
                ret.sql_error = " БД " + i_connect.Database + " \n '" + sql.ToString() + "' \n " + ex.Message;
                string err = ret.text + " \n " + ret.sql_error;
                LogString += err;
                Loger.WriteInfo(LogString, ref LOGSTR);
                MonitorLog.WriteLog("16"+err, MonitorLog.typelog.Error, 1, 3, true);

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Rollback();
                }
                i_connect.Close();

                return ret;
            }
            #endregion
        }

        //Процедура добавления отчета в базу
        public Returns WriteBlob(UpData upd, string PathFile , ref string LOGSTR)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection i_connect;
            string LogString = "";


            #region Подключение к БД
            try
            {
                LogString += " \r\n\r\nПопытка подключени к БД...";
                i_connect = GetConnection(this.cons_Webdata);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString , ref LOGSTR);
                return ret;
            }

            ret = OpenDb(i_connect, true);
            if (!ret.result)
            {
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            #endregion
            LogString += "OK";
            StringBuilder sql = new StringBuilder();

            //Загрузка файла отчета в строку байтов
            #region Загрузка файла отчета в  байты            
            //string data2 = File.ReadAllText(PathFile,Encoding.GetEncoding("windows-1251"));       
            #endregion

            sql.Append(" UPDATE  updater SET report = ?  where nzp_up = " + "\'" + upd.nzp + "\';");
            

            #region Выполнение запроса на обнавление
            IDbTransaction trans;
            try
            {
                trans = i_connect.BeginTransaction();
            }
            catch (Exception)
            {
                trans = null;
            }
            //Выполнение запроса            
            ret = Utils.InitReturns();
            try
            {
                LogString += "\r\n\r\nПопытка выполнить запрос...";
                IDbCommand cmd = null;
                if (trans != null)
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect, trans);
                }
                else
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect);
                }
                //Параметры для запроса
                DBManager.addDbCommandParameter(cmd, "@binaryValue", DbType.String, LOGSTR);//data2;
                

                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Commit();
                }
                i_connect.Close();
                ret.text = "Обновлено строк: " + res;
                LogString += "OK";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\r\n\r\nОшибка зписи в базу данных ";
                ret.sql_error = " БД " + i_connect.Database + " \n '" + sql.ToString() + "' \n " + ex.Message;
                string err = ret.text + " \n " + ret.sql_error;
                LogString += err;
                Loger.WriteInfo(LogString, ref LOGSTR);
                MonitorLog.WriteLog("17"+err, MonitorLog.typelog.Error, 1, 3, true);

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Rollback();
                }
                i_connect.Close();

                return ret;
            }
            #endregion
        }

        //Процедура отмечающая обновления WEB как недостижимые
        public Returns WriteWEB_NextApdateStatus(ref string LOGSTR)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection i_connect;
            string LogString = "";


            #region Подключение к БД
            try
            {
                LogString += " \r\n\r\nПопытка подключени к БД...";
                i_connect = GetConnection(this.cons_Webdata);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString , ref LOGSTR);
                return ret;
            }

            ret = OpenDb(i_connect, true);
            if (!ret.result)
            {
                LogString += "\r\n\r\nОшибка подключения к БД.";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            #endregion
            LogString += "OK";
            StringBuilder sql = new StringBuilder();                      
            
            //Получение последней версии хоста(потом в отдельную процедуру)
            //float HostLastVersion = -1;
            sql.Append(" UPDATE updater SET  status = 5 where  ABS(version - (Select  MAX(floor(version)) from updater where  status = 1 and  typeup = 'host')) > 1 and  typeup = 'web' and version <> 0 ");                         

            #region Выполнение запроса на обнавление
            IDbTransaction trans;
            try
            {
                trans = i_connect.BeginTransaction();
            }
            catch (Exception)
            {
                trans = null;
            }
            //Выполнение запроса            
            ret = Utils.InitReturns();
            try
            {
                LogString += "\r\n\r\nПопытка выполнить запрос...";
                IDbCommand cmd = null;
                if (trans != null)
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect, trans);
                }
                else
                {
                    cmd = DBManager.newDbCommand(sql.ToString(), i_connect);
                }
                int res = cmd.ExecuteNonQuery();
                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Commit();
                }
                i_connect.Close();
                ret.text = "Обновлено строк: " + res;
                LogString += "OK";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\r\n\r\nОшибка зписи в базу данных ";
                ret.sql_error = " БД " + i_connect.Database + " \n '" + sql.ToString() + "' \n " + ex.Message;
                string err = ret.text + " \n " + ret.sql_error;
                LogString += err;
                Loger.WriteInfo(LogString, ref LOGSTR);
                MonitorLog.WriteLog("18"+err, MonitorLog.typelog.Error, 1, 3, true);

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

                sql.Remove(0, sql.Length);
                if (trans != null)
                {
                    trans.Rollback();
                }
                i_connect.Close();

                return ret;
            }
            #endregion
        }

        public Returns AddUpdate(string UpdatePath, string WebPath, string UpdateIndex, int status, ref string LOGSTR)
        {
            string typeup = "";
            switch (UpdateIndex)
            {
                case "0":
                    {
                        typeup = "host";
                        break;
                    }
                case "1":
                    {
                        typeup = "web";
                        break;
                    }
                case "2":
                    {
                        typeup = "broker";
                        break;
                    }
                case "3":
                    {
                        typeup = "script";
                        break;
                    }
                case "4":
                    {
                        typeup = "updater";
                        break;
                    }
            }

            Returns ret = Utils.InitReturns();
            IDbConnection conn = DBManager.newDbConnection(this.cons_Webdata);

            ret = OpenDb(conn, true);

            if (!ret.result)
            {
                return ret;
            }

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO updater (dateup, typeup, status, path, web_path) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + typeup + "'," + status.ToString() + ", '" + UpdatePath + "', '" + WebPath + "');");

            try
            {
                ret = ExecSQL(conn, sql.ToString(), true);
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения добавления данных в базу " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }

            DbPatch db = new DbPatch();
            UpData up = db.GetHistoryLast(int.Parse(UpdateIndex));

            try
            {
                ret = WriteBlob(up, null, ref LOGSTR);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления отчета в базу " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }

            return ret;
        }
        
      
        ////Процедура получения отчета
        //public ArrayList GetReportByVersion(out Returns ret, string versionL , string versionR, string typeUp)
        //{
        //    ArrayList UpdLis = new ArrayList();

        //    //Returns ret = Utils.InitReturns();     
        //    ret = Utils.InitReturns();
        //    //заполнить webdata:tXX_cnt
        //    IDbConnection conn_db = GetConnection(this.cons_Webdata);
        //    IfxDataReader reader;
        //    //IfxDataReader reader_2;


        //    ret = OpenDb(conn_db, true);
        //    if (!ret.result)
        //    {
        //        return null;
        //    }

        //    StringBuilder sql = new StringBuilder();

        //    sql.Append(" Select  version, report from  updater where  version   between " +  
        //                versionL +  " and " + versionR + " and " + " typeup = " + "\'" + typeUp + "\'");                                              

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
        //        while (reader.Read())
        //        {
        //            UpData updata = new UpData();
                    
        //            if (reader["version"] != DBNull.Value) updata.Version = (double)(reader["version"]);
        //            if (reader["report"] != DBNull.Value) updata.report = Convert.ToString(reader["report"]).Trim();


        //            UpdLis.Add(updata);
        //        }
        //        sql.Remove(0, sql.Length);
        //        reader.Close();
        //        conn_db.Close(); //закрыть соединение с основной базой                                


        //        return UpdLis;
        //    }
        //    finally
        //    {
        //        conn_db.Close();
        //    }   
        //}

        ////процедура выполнения SQL строки
        //public Dictionary<string, object> GoPatch_DB(out Returns ret,string SQL_String, string dataBaseType)
        //{
        //    ret = Utils.InitReturns();

        //    //if (pref == "")
        //    //{
        //    //    ret.text = "Префикс базы данных не задан";
        //    //    return null;
        //    //}

        //    //Подключения к базам
        //    IDbConnection conn_db;
        //    if (dataBaseType == "cache")
        //    {
        //        conn_db = GetConnection(Constants.cons_Webdata);
        //    }
        //    else
        //    {
        //        conn_db = GetConnection(Constants.cons_Kernel);
        //    }

        //    IfxDataReader reader;

        //    ret = OpenDb(conn_db, true);
        //    if (!ret.result)
        //    {
        //        return null;
        //    }

            
        //    StringBuilder sql = new StringBuilder();
        //    sql.Append(SQL_String);

        //    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
        //    {
        //        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
        //        conn_db.Close();
        //        ret.result = false;
        //        return null;
        //    }

        //    try
        //    {
        //        Dictionary<string, object> retDic = new Dictionary<string, object>();

        //        if (reader.HasRows)
        //        {                    
                    
        //            int ind = 0;
        //            while (reader.Read())
        //            {
        //                for (int i = 0; i < reader.FieldCount; i++ )
        //                {
        //                    if (reader[i] != DBNull.Value)
        //                    {
        //                        retDic.Add(reader.GetName(i) + ind, reader[i].ToString());
        //                    }
        //                }
        //                ind++;
        //            }
        //        }
        //        if (reader != null)
        //        {
        //            reader.Close();
        //        }
        //        conn_db.Close(); //закрыть соединение с основной базой  
        //        sql.Remove(0, sql.Length);

        //        return retDic;
        //    }
        //    catch(Exception ex)
        //    {
        //        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
        //        conn_db.Close(); //закрыть соединение с основной базой                
        //        return null;
        //    }        
        //}

    }
}
