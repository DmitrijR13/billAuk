using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;

namespace Unloader
{
    public class DB
    {
        public string schema { get; set; }
        public string db { get; set; }
        protected virtual IDbConnection Connection { get; set; }

        public string connectionString { get; set; }
        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        public IDbConnection OpenConnection(string conn = null)
        {
            if (conn != null) connectionString = conn;
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(connectionString);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.resultMessage);
                }
            }
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            return Connection;
        }

        public void Execute(string sql)
        {
            Connection.Execute(sql);
        }

        public IEnumerable<T> Query<T>(string sql)
        {
            return Connection.Query<T>(sql);
        }

        public T First<T>(string sql)
        {
            return Connection.Query<T>(sql).First();
        }

        public T ExecuteScalar<T>(string sql)
        {
            return Connection.ExecuteScalar<T>(sql);
        }

        public List<T> Fetch<T>(string sql)
        {
            return Connection.Query<T>(sql).ToList();
        }

        /// <summary>Закрыть соединение с БД</summary>
        public void CloseConnection()
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
                throw new Exception(result.resultMessage);
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
                throw new Exception(result.resultMessage);
            }
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
                throw new Exception(result.resultMessage);
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
        ///// <summary>Получить результат sql запроса в виде List<T></summary>
        ///// <param name="sql">Sql запрос</param>
        //protected virtual List<T> Query<T>(string sql) where T : class
        //{
        //    return Query<T>(sql, true, 300);
        //}
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
                throw new Exception(result.resultMessage);
            }
            var list = mapList<T>(reader);
            CloseReader(ref reader);
            return list;
        }
        private static List<T> mapList<T>(MyDataReader dr)
        {
            var list = new List<T>();

            var properties = typeof(T).GetProperties();
            while (dr.Read())
            {
                var t = Activator.CreateInstance<T>();
                foreach (var pi in properties)
                    try
                    {
                        if (dr[pi.Name] != DBNull.Value)
                            pi.SetValue(t, dr[pi.Name], null);
                    }
                    catch
                    {
                    }

                list.Add(t);
            }
            return list;
        }

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


        public List<string> GetAllSchema(string conn = null, string db = null)
        {
            if (conn == null) conn = connectionString;
            if (db == null) db = this.db;
            conn = CreateConnectionString(conn, db);
            MyDataReader reader = null;
            var list = new List<string>();
            try
            {
                OpenConnection(conn);
                ExecRead(out reader, "select schema_name from information_schema.schemata where schema_name <> 'information_schema' and schema_name !~ E'^pg_'  ");
                while (reader.Read())
                {
                    list.Add(reader["schema_name"].ToString());
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                CloseConnection();
            }
            return list;
        }

        public List<string> GetAllDb(string conn)
        {
            MyDataReader reader = null;
            var list = new List<string>();
            try
            {
                OpenConnection(conn);
                ExecRead(out reader, "SELECT datname FROM pg_database WHERE datistemplate = false and upper(datname) not like upper('%postgres%');");
                while (reader.Read())
                {
                    list.Add(reader["datname"].ToString());
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                CloseConnection();
            }
            return list;
        }

        public string CreateConnectionString(string conn, string db_name)
        {
            connectionString = conn;
            return connectionString.Replace("Database=" + conn.Split(';')[4].Split('=')[1], "Database=" + db_name);
        }

        public Points LoadPoints(string conn = null, string db = null, string schema_name = "public")
        {
            if (conn == null) conn = connectionString;
            if (schema_name == null) schema_name = schema;
            if (db == null) db = this.db;
            MyDataReader reader = null;
            conn = CreateConnectionString(conn, db);
            Points points = null;
            try
            {
                OpenConnection(conn);

                ExecRead(out reader, "select * from " + schema_name + ".s_point where nzp_graj = 0");
                while (reader.Read())
                {
                    points = new Points
                    {
                        nzp_wp = DBManager.CastValue<int>(reader["nzp_wp"]),
                        point = DBManager.CastValue<string>(reader["point"]).Trim(),
                        pref = DBManager.CastValue<string>(reader["bd_kernel"]).Trim()
                    };
                }
                reader.Close();
                if (points != null) points.pointList = new List<Point>();
                ExecRead(out reader, "select * from " + schema_name + ".s_point where nzp_graj <> 0");
                while (reader.Read())
                {
                    points.pointList.Add(new Point
                    {
                        nzp_wp = DBManager.CastValue<int>(reader["nzp_wp"]),
                        point = DBManager.CastValue<string>(reader["point"]).Trim(),
                        pref = DBManager.CastValue<string>(reader["bd_kernel"]).Trim()
                    });
                }
            }
            catch
            {
            }
            finally
            {
                if (reader != null) reader.Close();
                CloseConnection();
            }
            return points;
        }


        /// <summary>
        /// Путь до директории с файлами
        /// </summary>
        /// <returns>Директория с файлами</returns>
        public string GetPath()
        {
            var parentDir = (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory;
            var directory = Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}",
                parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            return directory.FullName;
        }
    }
}
