using System.Linq;
using System.Reflection;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using STCLINE.KP50.DataBase;

    /// <summary>Базовый класс переноса домов</summary>
    public abstract class Transfer
    {
        protected Transfer() { }

        protected virtual IDbConnection Connection { get; set; }

        private readonly string connectionString = Constants.cons_Kernel;
        /// <summary>Парметры отчета</summary>
        public virtual TransferParams HouseParams { get; set; }

        /// <summary>
        /// Запуск переноса данных
        /// </summary>
        public void StartTransfer()
        {
            try
            {
                OpenConnection();
                TransferringDataFromOldBank();
            }
            catch (MyExeption ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                TransferProgress.InsertIntoHouseLog(HouseParams.transfer_id, HouseParams.current_house.nzp_dom, 0,
                    "Произошла ошибка при переносе данных из одной таблицы в другую");
                MonitorLog.WriteException("Произошла ошибка при переносе данных из одной таблицы в другую ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при переносе данных из одной таблицы в другую ", ex);
                }
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Запуск удаления данных 
        /// </summary>
        public void StartDelete()
        {
            try
            {
                OpenConnection();
                DeleteDataFromOldBank();
            }
            catch (Exception ex)
            {
                TransferProgress.InsertIntoHouseLog(HouseParams.transfer_id, HouseParams.current_house.nzp_dom, 0, "Произошла ошибка при удалении данных из таблицы");
                MonitorLog.WriteException("Произошла ошибка при удалении данных из таблицы ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при удалении данных из таблицы ", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Запуск отката переноса данных
        /// </summary>
        public void StartAddedRollback()
        {
            try
            {
                OpenConnection();
                RollbackAddedData();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при откате изменений добавления данных в новую таблицу", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при откате изменений добавления данных в новую таблицу", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Запуск отката удаления данных
        /// </summary>
        public void StartDeletedRollback()
        {
            try
            {
                OpenConnection();
                RollbackAddedData();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при откате изменений удаления данных из таблицы", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при откате изменений удаления данных из таблицы", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Запуск сравнения структуры таблиц
        /// </summary>
        public void StartCompare()
        {
            try
            {
                OpenConnection();
                if (!Compare())
                {
                    var ex = new MyExeption("Ошибка, перенос не возможен, разные структуры таблиц");
                    MonitorLog.WriteException("Ошибка, перенос не возможен, разные структуры таблиц", ex);
                    throw ex;
                }
            }
            catch (MyExeption ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при сравнении структуры таблиц", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при сравнении структуры таблиц", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>Перенести данные из таблицы</summary>
        protected abstract void TransferringDataFromOldBank();

        /// <summary>Удалить данные из таблицы</summary>
        protected abstract void DeleteDataFromOldBank();

        /// <summary>Откатить изменения, которые возникли при переносе данных в новый банк </summary>
        protected abstract void RollbackAddedData();

        /// <summary>Откатить изменения, которые возникли при удалении данных в старом банке</summary>
        protected abstract void RollbackDeletedData();

        /// <summary>Сравнение данных в старом и новом банке</summary>
        protected abstract bool CompareDataInTables();

        /// <summary>Сравнение полей таблицы в старом и новом банке</summary>
        protected abstract bool Compare();


        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        private IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(connectionString);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.text);
                }
            }

            return Connection;
        }


        /// <summary>Закрыть соединение с БД</summary>
        private void CloseConnection()
        {
            try
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection = null;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Не удалось закрыть соединение", exc);
            }
        }

        /// <summary>Закрыть IDataReader</summary>
        /// <param name="reader">Экземпляр IDataReader</param>
        private void CloseReader(ref MyDataReader reader)
        {
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }
        }

        /// <summary>Открыть соединение с БД</summary>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void OpenDb(bool inlog = true)
        {
            var result = DBManager.OpenDb(Connection, inlog);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual void ExecSQL(string sql)
        {
            ExecSQL(sql, true, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecSQL(string sql, bool inlog)
        {
            ExecSQL(sql, inlog, 6000);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecSQL(string sql, bool inlog, int timeout)
        {
            var result = DBManager.ExecSQL(Connection, sql, inlog, timeout);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        /// <summary>Проверка на существование таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual bool TempTableInWebCashe(string tableName)
        {
            return DBManager.TempTableInWebCashe(Connection, tableName);
        }

        /// <summary>Проверка на существование колонки в таблице</summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="columnName">Название колонки</param>
        /// <returns>Колонка</returns>
        protected virtual bool TempColumnInWebCashe(string tableName, string columnName)
        {
            return DBManager.TempColumnInWebCashe(Connection, tableName, columnName);
        }


        /// <summary>Получить результат sql запроса в виде таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <returns>Таблица</returns>
        protected virtual DataTable ExecSQLToTable(string sql)
        {
            return DBManager.ExecSQLToTable(Connection, sql);
        }
        /// <summary>Получить результат sql запроса в виде List<T></summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual List<T> Query<T>(string sql) where T : class
        {
            return Query<T>(sql, true, 300);
        }


        /// <summary>Получить результат sql запроса в виде значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual object ExecScalar(string sql)
        {
            Returns ret;
            return DBManager.ExecScalar(Connection, sql, out ret, true);
        }

        /// <summary>Получить результат sql запроса в виде типизированного значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual T ExecScalar<T>(string sql) where T : struct
        {
            Returns ret;
            var obj = DBManager.ExecScalar(Connection, sql, out ret, true);
            return obj == null || obj == DBNull.Value ?
                default(T) : (T)Convert.ChangeType(obj, typeof(T));
        }

        /// <summary>Получить результат sql запроса в виде List<T></summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual List<T> Query<T>(string sql, bool inlog) where T : class
        {
            return Query<T>(sql, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде List<T></summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual List<T> Query<T>(string sql, bool inlog, int timeout) where T : class
        {
            MyDataReader reader;
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog, 6000);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
            var list = mapList<T>(reader);
            CloseReader(ref reader);
            return list;
        }
        /// <summary>
        /// Метод получения данных
        /// </summary>
        /// <param name="sql">Текст запроса</param>
        /// <returns>DataTable</returns>
        protected virtual DataTable OpenSql(string sql)
        {
            return ClassDBUtils.OpenSQL(sql, Connection).resultData;
        }

        /// <summary>
        /// Метод получения данных
        /// </summary>
        /// <param name="sql">Текст запроса</param>
        /// <param name="transaction">Транзакция</param>
        /// <returns>DataTable</returns>
        protected virtual DataTable OpenSql(string sql, IDbTransaction transaction)
        {
            return ClassDBUtils.OpenSQL(sql, Connection, transaction).resultData;
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет (по умолчанию да)</param>
        protected virtual void ExecRead(out MyDataReader reader, string sql, bool inlog = true)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog, 6000);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        private static List<T> mapList<T>(MyDataReader dr)
        {
            var list = new List<T>();

            PropertyInfo[] properties = typeof(T).GetProperties();
            while (dr.Read())
            {
                T t = Activator.CreateInstance<T>();
                foreach (PropertyInfo pi in properties)
                    try
                    {
                        if (dr[pi.Name] != DBNull.Value)
                            pi.SetValue(t, dr[pi.Name], null);
                    }
                    catch (Exception)
                    {
                    }

                list.Add(t);
            }
            return list;
        }

        /// <summary>
        /// Разделитель наименования базы данных (схемы) и таблицы
        /// </summary>
        protected static string tableDelimiter
        {
            get
            {
                return DBManager.tableDelimiter;
            }
        }

        /// <summary>
        /// ключевые слова для перевода Informix в PostgreSQL
        /// </summary>
        protected static string sKernelAliasRest = DBManager.sKernelAliasRest;
        protected static string sDataAliasRest = DBManager.sDataAliasRest;
        protected static string sUploadAliasRest = DBManager.sUploadAliasRest;
        protected static string tbluser = DBManager.tbluser;
        protected static string sDecimalType = DBManager.sDecimalType;
        protected static string sCharType = DBManager.sCharType;
        protected static string sUniqueWord = DBManager.sUniqueWord;
        protected static string sNvlWord = DBManager.sNvlWord;
        protected static string sConvToNum = DBManager.sConvToNum;
        protected static string sConvToInt = DBManager.sConvToInt;
        protected static string sConvToChar = DBManager.sConvToChar;
        protected static string sConvToVarChar = DBManager.sConvToVarChar;
        protected static string sConvToDate = DBManager.sConvToDate;
        protected static string sDefaultSchema = DBManager.sDefaultSchema;
        protected static string s0hour = DBManager.s0hour;
        protected static string sUpdStat = DBManager.sUpdStat;
        protected static string sCrtTempTable = DBManager.sCrtTempTable;
        protected static string sUnlogTempTable = DBManager.sUnlogTempTable;
        protected static string sCurDate = DBManager.sCurDate;
        protected static string sCurDateTime = DBManager.sCurDateTime;
        protected static string DateNullString = DBManager.DateNullString;
        protected static string sFirstWord = DBManager.sFirstWord;
        protected static string sDateTimeType = DBManager.sDateTimeType;
        protected static string sLockMode = DBManager.sLockMode;

        
        private void GetTableFieldReader(out MyDataReader reader, string tableName, string bank)
        {
            var sql = string.Format("SELECT column_name AS name, data_type AS type " +
                                    "FROM information_schema.columns " +
                                    "WHERE table_name = '{0}' " +
                                    "AND table_schema ='{1}'" +
                                    "AND (column_default NOT LIKE '%nextval%' OR column_default IS NULL)", tableName, bank);
            ExecRead(out reader, sql);
        }

        /// <summary>
        /// получить список полей таблицы в виде Dictionary<key,value>
        /// </summary>
        /// <param name="tableName">Наименование таблицы</param>
        /// <param name="bank">Банк данных</param>
        /// <returns></returns>
        protected Dictionary<string, string> GetTableFields(string tableName, string bank)
        {
            bank = bank.Replace('.', ' ').Trim();
            var dict = new Dictionary<string, string>();
            MyDataReader reader;
            GetTableFieldReader(out reader, tableName, bank);
            while (reader.Read())
            {
                dict.Add(reader["name"].ToString(), reader["type"].ToString());
            }
            CloseReader(ref reader);
            return dict;
        }

        /// <summary>
        /// получить список полей таблицы  
        /// </summary>
        /// <param name="tableName">Наименование таблицы</param>
        /// <param name="bank">банк данных</param>
        /// <returns></returns>
        protected List<string> GetTableFieldsList(string tableName, string bank)
        {
            bank = bank.Replace('.', ' ').Trim();
            var list = new List<string>();
            MyDataReader reader;
            GetTableFieldReader(out reader, tableName, bank);
            while (reader.Read())
            {
                list.Add(reader["name"].ToString());
            }
            CloseReader(ref reader);
            return list;
        }

        /// <summary>
        /// получить список полей таблицы  
        /// </summary>
        /// <param name="tableName">Наименование таблицы</param>
        /// <param name="bank">банк данных</param>
        /// <returns></returns>
        protected List<TableContains> GetTableFieldsNameValueList(string tableName, string bank)
        {
            bank = bank.Replace('.', ' ').Trim();
            var list = new List<TableContains>();
            MyDataReader reader;
            GetTableFieldReader(out reader, tableName, bank);
            while (reader.Read())
            {
                list.Add(new TableContains { name = reader["name"].ToString(), type = reader["type"].ToString() });
            }
            CloseReader(ref reader);
            return list;
        }


        private class MyExeption : Exception
        {
            public MyExeption(string error)
                : base(error)
            {
            }
        }

        /// <summary>
        /// список nzp_kvar
        /// </summary>
        /// <returns>список ключей</returns>
        public List<int> GetKvars()
        {
            MyDataReader reader = null;
            var list = new List<int>();
            ExecRead(out reader, string.Format("select nzp_kvar from {0}kvar where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest, HouseParams.current_house.nzp_dom));
            while (reader.Read())
            {
                list.Add(reader["nzp_kvar"] != DBNull.Value ? Convert.ToInt32(reader["nzp_kvar"]) : 0);
            }

            if (list.Any())
            {
                ExecRead(out reader, string.Format("select nzp_kvar from {0}kvar where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest, HouseParams.current_house.nzp_dom));
                while (reader.Read())
                {
                    list.Add(reader["nzp_kvar"] != DBNull.Value ? Convert.ToInt32(reader["nzp_kvar"]) : 0);
                }
                
            }
            CloseReader(ref reader);
            return list;
        }

        /// <summary>
        /// список полей таблицы в строчку, без ключа 
        /// </summary>
        /// <param name="tableName">Наименование таблицы</param>
        /// <param name="bank">банк данных</param>
        /// <returns>строка</returns>
        public virtual string GetFields(string tableName, string bank)
        {
            var list = GetTableFieldsList(tableName, bank);
            //list.RemoveAt(0);
            return string.Join(",", list.ToArray());
        }
    }
}