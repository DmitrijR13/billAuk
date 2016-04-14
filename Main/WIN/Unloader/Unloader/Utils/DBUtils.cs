using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Unloader
{
    public class MyDataReader
    {
        protected IDataReader _reader = null;

        protected IDbCommand _command = null;

        public void setReaderAndCommand(IDataReader reader, IDbCommand command)
        {
            this.Close();
            this._reader = reader;
            this._command = command;
        }

        /// <summary>
        /// Выполняет чтение следующей записи
        /// </summary>
        /// <returns>Результат чтения</returns>
        public bool Read()
        {
            if (this._reader != null) return this._reader.Read();
            else return false;
        }

        /// <summary>
        /// Возвращает значение поля по индексу в запросе
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Значение поля</returns>
        public object this[int i]
        {
            get
            {
                return (_reader != null) ? this._reader[i] : DBNull.Value;
            }
        }

        /// <summary>
        /// Возвращает значение поля по наименованию
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Значение поля</returns>
        public object this[string name]
        {
            get
            {
                return (_reader != null) ? this._reader[name] : DBNull.Value;
            }
        }

        /// <summary>
        /// Выполняет освобождение ресурсов
        /// </summary>
        public void Close()
        {
            if (this._reader != null)
            {
                if (!this._reader.IsClosed)
                {
                    this._reader.Close();
                    this._reader.Dispose();
                }
                this._reader = null;
            }

            if (this._command != null)
            {
                this._command.Dispose();
                this._command = null;
            }
        }
    }



    public static class DBManager
    {
        public static Returns OpenDb(IDbConnection connection, bool inlog)
        {
            Returns ret;
            ret = new Returns();
            try
            {
                if (connection.State == ConnectionState.Closed) connection.Open();
                else if (connection.State == ConnectionState.Broken)
                {
                    connection.Close();
                    connection.Open();
                }
                return ret;
            }
            catch (Exception ex)
            {
                if (connection != null) connection.Close();
                ret.resultMessage = "Ошибка:" + ex.Message;
                return ret;
            }
        }

        public static IDbConnection GetConnection(string connectionString)
        {
            return newDbConnection(connectionString);
        }

        public static IDbConnection newDbConnection(string connectionString)
        {
            //connectionString = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=websmr;Preload Reader=true;";
            return new NpgsqlConnection(connectionString);
        }

        public static string tableDelimiter
        {
            get
            {
                return ".";
            }
        }
        /// <summary>
        /// [Informix only]
        /// Поключиться к базе данных
        /// </summary>
        [Flags]
        public enum ConnectToDb
        {
            /// <summary>
            /// База данных App сервера
            /// </summary>
            Host = 1,

            /// <summary>
            /// База данных Web сервера
            /// Не используется для PostgreSQL
            /// </summary>
            Web = 2
        }

        /// <summary>
        /// Аргументы создания таблицы
        /// </summary>
        public enum CreateTableArgs
        {
            None = 0,

            /// <summary>
            /// Создать таблицу, если она не существует
            /// </summary>
            CreateIfNotExists = 1,

            /// <summary>
            /// Удалить таблицу, если она уже существует и создать новую
            /// </summary>
            DropIfExists = 2
        }

        /// <summary>
        /// кол-во затронутых строк при выполнении запроса
        /// </summary>
        public static int _affectedRowsCount = -100;

        /// <summary>Создать sql комманду</summary>
        /// <param name="sqlString">sql запрос</param>
        /// <param name="connection">Подключение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="command"></param>
        /// <param name="parameterName"></param>
        /// <returns>Sql комманда</returns>
        // добавление параметра в команду
        public static IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName)
        {
            var param = command.CreateParameter();
            param.ParameterName = parameterName;
            command.Parameters.Add(param);
            return param;
        }

        // добавление параметра в команду
        public static IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, DbType parameterType)
        {
            var param = addDbCommandParameter(command, parameterName);
            if (param != null)
            {
                param.DbType = parameterType;
            }

            return param;
        }

        // добавление параметра в команду
        public static IDbDataParameter addDbCommandFixedLengthParameter(IDbCommand command, string parameterName, DbType parameterType, int size)
        {
            var param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Size = size;
            }

            return param;
        }

        // добавление параметра в команду
        public static IDbDataParameter addDbCommandDecimalParameter(IDbCommand command, string parameterName, DbType parameterType, byte precision, byte scale)
        {
            var param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Precision = precision;
                param.Scale = scale;
            }

            return param;
        }

        // добавление параметра в команду
        public static IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, object value)
        {
            var param = addDbCommandParameter(command, parameterName);
            if (param != null)
            {
                param.Value = value;
            }

            return param;
        }

        // добавление параметра в команду
        public static IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, DbType parameterType, object value)
        {
            var param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Value = value;
            }

            return param;
        }

        public const string sKernelAliasRest = "_kernel.";
        public const string sDataAliasRest = "_data.";
        public const string sUploadAliasRest = "_upload.";
        public const string sSupgAliasRest = "_supg.";
        public const string tbluser = "";
        public const string sDecimalType = "numeric";
        public const string sCharType = "character";
        public const string sUniqueWord = "distinct";
        public const string sNvlWord = "coalesce";
        public const string sConvToNum = "::numeric";
        public const string sConvToInt = "::int";
        public const string sConvToChar = "::character";
        public const string sConvToVarChar = "::varchar";
        public const string sConvToDate = "::date";
        public const string sDefaultSchema = "public.";
        public const string s0hour = "interval '0 hour'";
        public const string sUpdStat = "analyze";
        public const string sCrtTempTable = "temp";
        public const string sUnlogTempTable = "";
        public const string sCurDate = "current_date";
        public const string sCurDateTime = "now()";
        public const string DateNullString = "Null::date";
        public const string sFirstWord = "limit";
        public const string sSerialDefault = "default";
        public const string sYearFromDate = "Extract(year from ";
        public const string sMonthFromDate = "Extract(month from ";
        public const string sDateTimeType = "timestamp";
        public const string sLockMode = "";
        public const string sMatchesWord = "similar to";
        public const string sRegularExpressionAnySymbol = "%";


        public static string SetLimitOffset(string sql, int limit, int offset)
        {
            sql = sql.Insert(sql.Length, limit != 0 ? " limit " + limit : "");
            sql = sql.Insert(sql.Length, offset != 0 ? " offset " + offset : "");

            return sql;
        }

        public static string SetSubString(string str, string start, string end)
        {
            return " substring(" + str + " from " + start + " +1 for " + end + ") ";
        }

        public static string SetInterval(string interval, string kind)
        {
            string result = String.Empty;
            switch (kind)
            {
                case "minute":
                    result = "extract (epoch from " + interval + ")::int/60";
                    break;
                case "hour":
                    result = "extract (epoch from " + interval + ")::int/3600";
                    break;
                case "day":
                    result = "extract (epoch from " + interval + ")::int/86400";
                    break;
            }
            return result;
        }

        public static void CloseDb(IDbConnection connection)
        {
            if (connection != null) connection.Close();
        }

        public static Returns ExecSQL(IDbConnection connection, string sql, bool inlog)
        {
            return ExecSQL(connection, null, sql, inlog, 6000);
        }

        public static Returns ExecSQL(IDbConnection connection, string sql, bool inlog, int time)
        {
            return ExecSQL(connection, null, sql, inlog, time);
        }

        public static Returns ExecSQL(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog)
        {
            return ExecSQL(connection, transaction, sql, inlog, 300);
        }

        /// <summary>
        /// Выполнение запроса с возвратом кол-ва затронутых строк
        /// </summary>
        /// <param name="connection">соединение с БД</param>
        /// <param name="transaction">транзакция</param>
        /// <param name="sql">текст запроса для выполнения</param>
        /// <param name="inlog">писать в лог при ошибке выполнения или нет</param>
        /// <param name="affectedRowsCount">кол-во затронутых строк</param>
        /// <param name="timeout">timeout для выполнения запроса</param>
        /// <returns></returns>
        public static Returns ExecSQL(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog,
             out int affectedRowsCount, int timeout = 300)
        {
            Returns ret = ExecSQL(connection, transaction, sql, inlog, timeout);
            affectedRowsCount = _affectedRowsCount;
            return ret;
        }

        /// <summary>Создать sql комманду</summary>
        /// <param name="sqlString">sql запрос</param>
        /// <param name="connection">Подключение</param>
        /// <param name="transaction">Транзакция</param>
        /// <returns>Sql комманда</returns>
        public static IDbCommand newDbCommand(string sqlString, IDbConnection connection, IDbTransaction transaction = null)
        {
            return connection is NpgsqlConnection ? new NpgsqlCommand(sqlString, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction) : null;
        }


        public static Returns ExecSQL(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog, int time)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");


            var ret = new Returns();
            IDbCommand cmd = null;

            try
            {
                cmd = newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = time;
                _affectedRowsCount = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ret.resultMessage = "Ошибка:" + ex.Message;
                ret.result = false;
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

            return ret;
        }

        public static Returns ExecRead(IDbConnection connection, out IDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connection, null, out reader, sql, inlog, 300);
        }

        public static Returns ExecRead(IDbConnection connection, out IDataReader reader, string sql, bool inlog, int timeout)
        {
            return ExecRead(connection, null, out reader, sql, inlog, timeout);
        }

        public static Returns ExecRead(IDbConnection connection, IDbTransaction transaction, out IDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connection, transaction, out reader, sql, inlog, 300);
        }

        public static Returns ExecRead(IDbConnection connection, IDbTransaction transaction, out IDataReader reader, string sql, bool inlog, int timeout)
        {
            var ret = new Returns();

            sql = sql.PgNormalize(connection);
            IDbCommand cmd = null;
            reader = null;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = timeout;
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                ret.resultMessage = "Ошибка:" + ex.Message;
                ret.result = false;
                if (reader != null)
                {
                    reader.Dispose();
                }

                reader = null;
            }
            finally
            {
                //if (cmd != null) cmd.Dispose();
            }

            return ret;
        }

        public static Returns ExecRead(IDbConnection connection, out MyDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connection, null, out reader, sql, inlog, 300);
        }

        public static Returns ExecRead(IDbConnection connection, out MyDataReader reader, string sql, bool inlog, int timeout)
        {
            return ExecRead(connection, null, out reader, sql, inlog, timeout);
        }

        public static Returns ExecRead(IDbConnection connection, IDbTransaction transaction, out MyDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connection, transaction, out reader, sql, inlog, 300);
        }

        public static T CastValue<T>(object value)
        {
            return (value == DBNull.Value || value == null) ? (typeof(T) == typeof(string) ? (T)Convert.ChangeType("", typeof(T)) : default(T)) : (T)Convert.ChangeType(value, typeof(T));
        }

        public static Returns ExecRead(IDbConnection connection, IDbTransaction transaction, out MyDataReader reader, string sql, bool inlog, int timeout)
        {
            var ret = new Returns();

            sql = sql.PgNormalize(connection);

            IDbCommand cmd = null;
            reader = null;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = timeout;
                reader = new MyDataReader();
                reader.setReaderAndCommand(cmd.ExecuteReader(), cmd);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.resultMessage = "Ошибка:" + ex.Message;

                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }

                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
            return ret;
        }

        public static T ExecScalar<T>(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog) where T : struct
        {
            Returns ret;
            var obj = ExecScalar(connection, transaction, sql, out ret, inlog);
            return obj == null || obj == DBNull.Value ?
                default(T) : (T)Convert.ChangeType(obj, typeof(T));
        }

        public static T ExecScalar<T>(IDbConnection connection, IDbTransaction transaction, string sql) where T : struct
        {
            Returns ret;
            var obj = ExecScalar(connection, transaction, sql, out ret, true);
            return obj == null || obj == DBNull.Value ?
                default(T) : (T)Convert.ChangeType(obj, typeof(T));
        }

        public static T ExecScalar<T>(IDbConnection connection, string sql) where T : struct
        {
            Returns ret;
            var obj = ExecScalar(connection, sql, out ret, true);
            return obj == null || obj == DBNull.Value ?
                default(T) : (T)Convert.ChangeType(obj, typeof(T));
        }

        public static object ExecScalar(IDbConnection connection, string sql, out Returns ret, bool inlog)
        {
            return ExecScalar(connection, null, sql, out ret, inlog);
        }

        public static object ExecScalar(IDbConnection connection, IDbTransaction transaction, string sql,
            out Returns ret, bool inlog)
        {
            return ExecScalar(connection, transaction, sql, out ret, inlog, 300);
        }

        public static object ExecScalar(IDbConnection connection, IDbTransaction transaction, string sql, out Returns ret, bool inlog, int timeout)
        {
            ret = new Returns();
            sql = sql.PgNormalize(connection);
            IDbCommand cmd = null;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = timeout;
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.resultMessage = "Ошибка:" + ex.Message;
                return null;
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }

        public static int GetSerialValue(IDbConnection conn)
        {
            return GetSerialValue(conn, 1, null);
        }

        public static int GetSerialValue(IDbConnection conn, IDbTransaction transaction) //
        {
            return GetSerialValue(conn, 1, transaction);
        }

        public static int GetSerialValue2(IDbConnection conn)
        {
            return GetSerialValue(conn, 2, null);
        }

        public static int GetSerialValue2(IDbConnection conn, IDbTransaction transaction)
        {
            return GetSerialValue(conn, 2, transaction);
        }

        public static int GetSerialValue(IDbConnection conn, int tip, IDbTransaction transaction)
        {
            int res = 0;
            if (tip == 1) //получить код добавленной записи
            {
                IDataReader reader;
                if (!ExecRead(conn, transaction, out reader, "select lastval() as key", false).result)
                {
                    return res;
                }
                try
                {
                    if (reader.Read())
                    {
                        res = Convert.ToInt32(reader["key"]);
                    }
                    ;
                }
                finally
                {
                    if (!reader.IsClosed) reader.Close();
                    reader.Dispose();
                }
            }
            return res;
        }

        public static bool DatabaseOnServer(IDbConnection conn_db, string db)
        {
            Returns ret;
            return DatabaseOnServer(conn_db, db, out ret);
        }

        public static bool DatabaseOnServer(IDbConnection conn_db, string db, out Returns ret)
        {
            IDataReader reader;
            ret = ExecRead(
                           conn_db,
                out reader,
 " Select * From information_schema.schemata Where schema_name = '" + db.Trim().ToLower() + "'",
                false);

            if (!ret.result)
            {
                return false;
            }
            bool b;
            try
            {
                b = reader.Read();
            }
            catch (Exception ex)
            {
                b = false;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }

            return b;
        }

        public static bool ProcedureInWebCashe(IDbConnection conn_web, string proc)
        {
            return ProcedureInWebCashe(conn_web, proc, "");
        }

        public static bool ProcedureInWebCashe(IDbConnection conn_web, string proc, string db)
        {
            return ProcedureExists(conn_web, proc, db);
        }

        private static bool ProcedureExists(IDbConnection connWeb, string proc, string db)
        {
            db = string.IsNullOrEmpty(db) ? "public" : db;

            var query =
                new StringBuilder().Append("SELECT  count(*) ")
                    .Append("FROM    pg_catalog.pg_namespace n ")
                    .Append("JOIN    pg_catalog.pg_proc p ")
                    .Append("ON      pronamespace = n.oid ")
                    .AppendFormat("where proname = '{0}' and nspname = '{1}'", proc, db);

            Returns ret;

            ExecScalar(connWeb, query.ToString(), out ret, true);

            if (!ret.result)
            {
                return false;
            }

            int procCount;
            int.TryParse(ret.resultMessage, out procCount);

            return procCount > 0;
        }

        public static int ProcedureInWebCasheID(IDbConnection conn_web, string proc, string db)
        {
            if (!string.IsNullOrEmpty(db)) db += ":";

            IDataReader reader;
            int procid = 0;
            if (
                ExecRead(
                         conn_web,
                    out reader,
                    " Select procid From " + db + "sysprocedures Where lower(procname) = '" + proc.Trim().ToLower() + "'",
                    false).result)
            {
                try
                {
                    if (reader.Read())
                    {
                        procid = Convert.ToInt32(reader["procid"]);
                    }
                }
                finally
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return procid;
        }

        public static bool TableInWebCashe(IDbConnection conn_web, string tab)
        {
            return (TableInWebCasheID(conn_web, tab) > 0);
        }

        public static uint TableInWebCasheID(IDbConnection conn_web, string tab)
        {
            MyDataReader reader;
            uint tabid = 0;
            if (ExecRead(
                         conn_web,
                out reader,
 " select pg_class.oid as tabid from pg_class inner join pg_namespace on pg_class.relnamespace = pg_namespace.oid Where lower(relname) = '"
                + tab.Trim().ToLower() + "' and pg_namespace.nspname = CURRENT_SCHEMA()",
                false).result)
            {

                try
                {
                    if (reader.Read())
                    {
                        tabid = Convert.ToUInt32(reader["tabid"]);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            return tabid;
        }


        public static bool TableInBase(IDbConnection conn_web, IDbTransaction transaction, string base_name, string tab)
        {
            return (TableInBaseID(conn_web, transaction, base_name, tab) > 0);
        }

        public static int TableInBaseID(IDbConnection conn_web, IDbTransaction transaction, string base_name, string tab)
        {
            IDataReader reader;
            int tabid = 0;
            base_name = base_name.Trim();
            Returns ret;
            string sql = "select COUNT(*) as tabid from pg_tables where lower(tablename)='" + tab.Trim().ToLower() + "' and lower(schemaname)='" + base_name + "'";


            object obj = ExecScalar(conn_web, transaction, sql, out ret, true);
            if (obj != null)
            {
                tabid = Convert.ToInt32(obj);
            }
            if (ExecRead(conn_web, out reader, sql, false).result)
            {
                try
                {
                    if (reader.Read())
                    {
                        tabid = Convert.ToInt32(reader["tabid"]);
                    }
                }
                finally
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return tabid;
        }

        /// <summary>
        /// Проверка наличия доступа к таблице
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tab"></param>
        /// <returns></returns>
        public static bool TempTableInWebCashe(IDbConnection connection, string tab)
        {
            return TempTableInWebCashe(connection, null, tab);
        }

        public static bool TempTableInWebCashe(IDbConnection connection, IDbTransaction transaction, string tab)
        {
            MyDataReader reader;

            string sql = " SELECT * FROM " + tab + " LIMIT 1; ";

            if (!ExecRead(connection, transaction, out reader, sql, false).result)
            {
                return false;
            }
            reader.Close();
            return true;
        }

        /// <summary>
        /// Проверка наличия колонки во временной таблице
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tab"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public static bool TempColumnInWebCashe(IDbConnection connection, string tab, string column)
        {
            return TempColumnInWebCashe(connection, null, tab, column);
        }

        public static bool TempColumnInWebCashe(IDbConnection connection, IDbTransaction transaction, string tab, string column)
        {
            MyDataReader reader;
            if (!ExecRead(connection, transaction, out reader, "select " + column + " from " + tab, false).result)
            {
                return false;
            }
            reader.Close();
            reader = null;
            return true;
        }


        /// Проверяет наличие колонки в таблице
        /// </summary>
        /// <param name="conn_id">ID соединения</param>
        /// <param name="tab">Наименование таблицы</param>
        /// <param name="column">Наименование колонки</param>
        /// <param name="cur_pg_dn">Наименование схемы  - нужно для PG</param>
        public static bool isTableHasColumn(IDbConnection conn_id, string tab, string column, string cur_pg_dn)
        {
            return isTableHasColumn(conn_id, null, tab, column, cur_pg_dn);
        }

        public static bool isTableHasColumn(IDbConnection conn_id, string tab, string column)
        {
            return isTableHasColumn(conn_id, null, tab, column, "");
        }

        public static bool isTableHasColumn(IDbConnection conn_id, IDbTransaction transaction, string tab, string column, string cur_pg_dn)
        {
            string dop = "";
            if (cur_pg_dn != "") dop = " and lower(table_schema)= " + Utils.EStrNull(cur_pg_dn.Trim().ToLower());
            MyDataReader reader;
            if (!ExecRead(
                          conn_id,
                transaction,
                out reader,
 " Select column_name From information_schema.columns " + " Where " + "  lower(table_name)  = " + Utils.EStrNull(tab.Trim().ToLower())
                + " " + " and lower(column_name)= " + Utils.EStrNull(column.Trim().ToLower())
                + dop,
                true).result)
                return false;
            bool b = false;
            try
            {
                b = reader.Read();
            }
            finally
            {
                reader.Close();
            }
            return b;
        }

        public static bool isHasIndex(IDbConnection conn_id, string index)
        {
            return isHasIndex(conn_id, null, index);
        }

        public static bool isHasIndex(IDbConnection conn_id, IDbTransaction transaction, string index)
        {
            IDataReader reader;
            if (!ExecRead(
                          conn_id,
                transaction,
                out reader,
 " SELECT * FROM pg_class c, pg_namespace n WHERE c.relnamespace = n.oid AND lower(relname) = "
                + Utils.EStrNull(index.Trim().ToLower()) + " AND relkind = 'i'"
,
                true).result) return false;
            bool b = false;
            try
            {
                b = reader.Read();
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            return b;
        }

        public static List<string> GetTables(IDbConnection conn_db, string db, string table) //
        {
            List<string> l = new List<string>();

            IDataReader reader;
            if (!ExecRead(
                          conn_db,
                out reader,
 " Select table_name as tabname From information_schema.tables" + " Where lower(table_name) = '" + table + "' and table_schema='" + db
                + "' and table_type='BASE TABLE'",
                true).result)
            {
                return l;
            }

            try
            {
                while (reader.Read())
                {
                    if (reader["tabname"] != DBNull.Value)
                    {
                        l.Add((string)reader["tabname"]);
                    }
                }
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            return l;
        }

        public static void ExecByStep(IDbConnection conn, string tab, string pole, string sql, int step, string groupby, out Returns ret)
        {
            long i_min = 0;
            long i_max = 0;
            sql = sql.PgNormalize(conn);
            // для Postgresql нет смысла делать операции по частям
            // - сделаем всё сразу
            ret = ExecSQL(conn, sql + " " + groupby, true, 60000);
        }

        /// <summary> Добавляет к таблице поле
        /// </summary>
        /// <param name="conn_db">Идентификатор соединения</param>
        /// <param name="table">Имя таблицы</param>
        /// <param name="field">Имя колонки</param>
        /// <param name="fieldType">Тип колонки</param>
        /// <returns></returns>
        public static Returns AddFieldToTable(IDbConnection conn_db, string table, string field, string fieldType)
        {
            if (!TempTableInWebCashe(conn_db, table)) return new Returns(false, "Таблицы \"" + table + "\" не существует");
            if (!isTableHasColumn(conn_db, table, field))
            {
                Returns ret = ExecSQL(conn_db, "alter table " + table + " add " + field + " " + fieldType, true);
                if (!ret.result)
                {
                    return new Returns(false, "Не удалось добавить к таблице \"" + table + "\" поле \"" + field + " " + fieldType + "\"");
                }
                else return new Returns(true);
            }
            return new Returns(true);
        }

        /// <summary> Удаляет поле из таблицы
        /// </summary>
        /// <param name="connection">Идентификатор соединения</param>
        /// <param name="table">Имя таблицы</param>
        /// <param name="fields">Имя колонки</param>
        /// <returns></returns>
        public static Returns DropFieldFromTable(IDbConnection connection, string table, string[] fields)
        {
            if (!TempTableInWebCashe(connection, table))
                return new Returns(true, "Таблицы \"" + table + "\" не существует");
            Returns ret = new Returns(true);
            foreach (var s in fields)
            {
                if (isTableHasColumn(connection, table, s)) continue;

                ret = ExecSQL(connection, "alter table " + table + " drop " + s, true);
                if (!ret.result)
                {
                    return new Returns(false, "Не удалось удалить поле \"" + s + "\" в таблице \"" + table + "\"");
                }
            }
            return ret;
        }

        public static bool SchemaExists(string schema, IDbConnection connection)
        {
            var sql = string.Format("select count(*) from information_schema.schemata where schema_name = '{0}'", schema);
            Returns ret;
            var countStr = ExecScalar(connection, sql, out ret, true);
            int countTmp;
            return int.TryParse(countStr.ToString(), out countTmp) && countTmp != 0;
        }


        public static string GetFullBaseName(IDbConnection Connection)
        {
            return "public";
        }

        /// <summary>
        /// Получить имя таблицы с сервером
        /// </summary>
        /// <param name="Connection">Подключение</param>
        /// <param name="dbName">имя БД</param>
        /// <param name="tableName">имя таблицы</param>
        /// <returns>полный путь к таблице</returns>
        public static string GetFullBaseName(IDbConnection Connection, string dbName, string tableName)
        {
            if (dbName == Connection.Database) return String.Format("public{1}{2}", dbName, tableDelimiter, tableName);
            return String.Format("{0}{1}{2}", dbName, tableDelimiter, tableName);

        }



        /// <summary>
        ///  Возвращает содержимо запроса в таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="sql">SQL Запрос</param>
        /// <returns>Таблицу с заполненными данными по запросу</returns>
        public static DataTable ExecSQLToTable(IDbConnection connection, string sql)
        {
            DataTable Data_Table = new DataTable();
            sql = sql.PgNormalize(connection);
            IDbCommand cmd = null;
            IDataReader reader = null;
            string err = String.Empty;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection);
                reader = cmd.ExecuteReader();
                Utils.setCulture();
                if (reader != null) Data_Table.Load(reader, LoadOption.OverwriteChanges);
            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();

            }
            if (err != String.Empty)
                throw new Exception("Ошибка чтения из базы данных ");

            return Data_Table;
        }

        public static Returns SelectDatabaseOrSchema(IDbConnection connection, string databaseOrSchema)
        {
            return ExecSQL(connection,
 "set search_path to '" + databaseOrSchema + "'"
, true);
        }


        public static string MDY(int month, int day, int year)
        {
            return string.Format(" '{0}-{1}-{2}'::timestamp ", year, month, day);

        }

        public static Returns DbCreateTable(IDbConnection DbConn, CreateTableArgs DbArgs, bool IsTempTable, string DbSchema, string TableName, params string[] Columns)
        {
            bool IsPostgres = true;

            Returns ret = new Returns();
            List<string> lstQuerys = new List<string>();
            lstQuerys.Add(
                IsPostgres ?
                string.Format("SET search_path TO '{0}'", DbSchema) :
                string.Format("DATABASE {0}", DbSchema)
                );
            switch (DbArgs)
            {
                case CreateTableArgs.DropIfExists:
                    if (IsPostgres)
                    {
                        lstQuerys.Add(string.Format("DROP TABLE {0} CASCADE", TableName));
                        lstQuerys.Add(string.Format("CREATE {0} TABLE {1} ({2})",
                            IsTempTable ? "TEMP" : string.Empty, TableName, string.Join(", ", Columns)));
                    }
                    else
                    {
                        lstQuerys.Add(
                            "CREATE PROCEDURE dropTable();\n" +
                            "ON EXCEPTION in (-206)\n" +
                            "END EXCEPTION WITH RESUME\n" +
                            "DROP TABLE " + TableName + " CASCADE;\n" +
                            "END PROCEDURE;\n");
                        lstQuerys.Add("EXECUTE PROCEDURE dropTable();");
                        lstQuerys.Add("DROP PROCEDURE dropTable;");
                        lstQuerys.Add(string.Format("CREATE {0} TABLE {1} ({2}) {3}",
                        IsTempTable ? "TEMP" : string.Empty,
                        TableName, string.Join(", ", Columns),
                        IsTempTable && !IsPostgres ? "WITH NO LOG" : string.Empty));
                    }
                    break;
                case CreateTableArgs.CreateIfNotExists:
                    if (IsPostgres)
                        lstQuerys.Add(string.Format("CREATE {0} TABLE IF NOT EXISTS {1} ({2}) {3}",
                             IsTempTable ? "TEMP" : string.Empty,
                             TableName, string.Join(", ", Columns),
                             IsTempTable && !IsPostgres ? "WITH NO LOG" : string.Empty));
                    else
                    {
                        lstQuerys.Add(
                            "CREATE PROCEDURE createTable();\n" +
                            "ON EXCEPTION in (-310)\n" +
                            "END EXCEPTION WITH RESUME\n" +
                            "CREATE " + (IsTempTable ? "TEMP " : string.Empty) + "TABLE " + TableName +
                            " (" + string.Join(", ", Columns) + ")" + (IsTempTable ? " WITH NO LOG" : string.Empty) +
                            "END PROCEDURE;\n");
                        lstQuerys.Add("EXECUTE PROCEDURE createTable();");
                        lstQuerys.Add("DROP PROCEDURE createTable;");
                    }
                    break;
                default:
                    lstQuerys.Add(string.Format("CREATE {0} TABLE {1} ({2}) {3}",
                        IsTempTable ? "TEMP" : string.Empty,
                        TableName, string.Join(", ", Columns),
                        IsTempTable && !IsPostgres ? "WITH NO LOG" : string.Empty));
                    break;
            }
            if (DbConn.State != ConnectionState.Open) DbConn.Open();
            string CurrentSchema = IsPostgres ?
                ExecScalar(DbConn, "SHOW search_path", out ret, true).ToString() :
                DbConn.Database;
            lstQuerys.Add(string.Format(IsPostgres ? "SET search_path TO {0}" : "DATABASE {0}", CurrentSchema));
            lstQuerys.ForEach(query => { if (!(ret = ExecSQL(DbConn, query, true)).result) return; });
            return ret;
        }

    }

    public static class PgExtensions
    {
        public static string PgNormalize(this string sql, IDbConnection connection = null)
        {
            // sql = sql.Replace(":", ".");
            sql = Regex.Replace(sql, @"select\s*(distinct|into)* ,", "select $1 ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\(\s*,", " (");
            sql = Regex.Replace(sql, @",{2,}", ",");
            sql = Regex.Replace(sql, "\"are\".", " ");
            sql = Regex.Replace(sql, @"\s+are\.", " ");
            //sql = Regex.Replace(sql, @"@\w+", string.Empty);
            sql = Regex.Replace(sql, @"\s*drop table ((\s*if exists\s*){2,})*", " drop table if exists ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\.{2,}", ".");
            sql = Regex.Replace(sql, @"\s*update statistics for table\s*", " analyze ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (connection != null)
            {
                // Нужно больше исследования
                //CheckDatabaseInSchema(sql, connection);
            }

            return sql;
        }

        /// <summary>
        /// Предназначена для установки пути поиска таблицы в схеме public
        /// </summary>
        /// <param name="sql">Готовый к выполнению sql</param>
        public static void CheckDatabaseInSchema(string sql, IDbConnection connection)
        {
            const string tablePattern = @"(\w+\s*\.*\s*\w+\s*)";

            /*
             * Найдет [<schema>.]<table> в select|insert|create
             */
            var searchTablePatternFormat = string.Format(@"\s+from\s+{0}|.*insert\s+into\s{0}|.*create.*table\s+{0}", tablePattern);

            var matches = Regex.Matches(sql, searchTablePatternFormat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (matches.Count > 0)
            {
                var table = matches[0].Groups[1].Value;

                if (!table.Contains("."))
                {
                    var set = "set search_path to public";
                    using (var com = new NpgsqlCommand(set, (NpgsqlConnection)connection))
                    {
                        com.ExecuteNonQuery();
                    }
                }
            }
        }

        #region Type extensions

        public static string CastTo(this string field, string type, string precision = null)
        {
            return string.Format("CAST({0} as {1})", field, !string.IsNullOrEmpty(precision) ? string.Format("{0}({1})", type, precision) : type);
        }

        #endregion Type extensions

        #region Sql extensions

        public static string AddIntoStatement(this string sqlStatetment, string intoStatetment)
        {
            var regex = new Regex(" from", RegexOptions.IgnoreCase);
            return regex.Replace(sqlStatetment, intoStatetment + " from ", 1);
        }

        public static string UpdateSet(this string sql, string left, string right, string alias = null)
        {
            var lefts = left.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var rights = right.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            if (lefts.Count() != rights.Count())
            {
                throw new ArgumentException("Число параметров в правой и левой части различно!");
            }

            var sb = new StringBuilder();
            sb.Append(" set ");
            for (int i = 0; i < lefts.Count(); i++)
            {
                sb.AppendFormat("{0} = {1}.{2},", lefts[i], !string.IsNullOrEmpty(alias) ? alias.Trim() : string.Empty, rights[i].Trim());
            }

            return string.Concat(sql, sb.ToString().TrimEnd(new[] { ',' }));
        }

        #endregion Sql extensions

        #region Date extensions

        public static string DatePart(this string date, string part)
        {
            return string.Format("date_part('{0}', {1})", part, date);
        }

        public static string Month(this string date)
        {
            return date.DatePart("month");
        }

        public static string Year(this string date)
        {
            return date.DatePart("year");
        }

        public static string Interval(this string interval, string units)
        {
            return string.Format("INTERVAL '{0} {1}'", interval, units);
        }

        public static string Interval(this int interval, string units)
        {
            return Interval(interval.ToString(), units);
        }

        public static string IntervalMinutes(this string interval)
        {
            return Interval(interval, "minutes");
        }

        public static string IntervalHours(this string interval)
        {
            return Interval(interval, "hours");
        }

        public static string IntervalMinutes(this int interval)
        {
            return IntervalMinutes(interval.ToString());
        }

        public static string IntervalHours(this int interval)
        {
            return IntervalHours(interval.ToString());
        }

        #endregion Date extensions
    }

    static public class Utils //утилиты
    //----------------------------------------------------------------------
    {
        static public bool ConvertStringToMoney(string s_sum, out decimal d_sum)
        {
            d_sum = 0;
            // non-breaking space, ничего лучше пока не придумала
            s_sum = s_sum.Replace("&#160;", "").Trim();

            // замена разделителя дробной части
            s_sum = s_sum.Replace(",", ".");

            int decSepPos = s_sum.LastIndexOf(".");

            if (decSepPos > -1)
            {
                // получаем дробную часть
                string curr_part = s_sum.Substring(decSepPos + 1);
                // получаем целую часть
                string int_part = s_sum.Substring(0, decSepPos);
                // убираем разделитель групп
                int_part = int_part.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                // оставляем только цифры в дробной части
                curr_part = Regex.Replace(curr_part, @"[^\d]", "");
                // оставляем только цифры в целой части
                int_part = Regex.Replace(int_part, @"[^\d]", "");
                // собираем число
                s_sum = int_part + "." + curr_part;
            }
            else
            {
                // убираем разделитель групп
                s_sum = s_sum.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                // оставляем только цифры
                s_sum = Regex.Replace(s_sum, @"[^\d]", "");
            }

            try
            {
                d_sum = s_sum.Trim() == "" ? 0 : Decimal.Parse(s_sum);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //----------------------------------------------------------------------
        static public string RunFile(string rf)
        //----------------------------------------------------------------------
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = rf;
            proc.EnableRaisingEvents = true;

            //proc.Exited += new EventHandler(proc_Exited);

            try
            {
                proc.Start();
                proc.WaitForExit();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //----------------------------------------------------------------------
        static public void UserLogin(string cons_User, out string Login, out string Password) //вытащить логины
        //----------------------------------------------------------------------
        {
            int l = cons_User.Length;
            int k = cons_User.LastIndexOf(";");

            string[] result = cons_User.Split(new string[] { ";" }, StringSplitOptions.None);

            try
            {
                Login = result[0].Trim();
                Password = result[1].Trim();
            }
            catch
            {
                Login = "";
                Password = "";
            }
        }
        //----------------------------------------------------------------------
        static public string IfmxDatabase(string st) //данные коннекта
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("Database") & (!zap.StartsWith("Database L")))
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("Server"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("UID"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + "@" + srv.Trim() + "  (" + usr.Trim() + ")";
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbDatabase(string st) //данные коннекта FireBird
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("data source"))
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("initial catalog"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("user id"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + ":" + srv.Trim() + "  (" + usr.Trim() + ")";
        }


        //----------------------------------------------------------------------
        static public string GetCorrectFIO(string st)
        //----------------------------------------------------------------------
        {
            if (st.Trim() == "") return st;

            //string[] masStr = st.Split(" ", StringSplitOptions.None);
            char[] delimiterChars = { ' ', ',', '.', ':' };
            string[] masStr = st.Split(delimiterChars);

            int i = 1;
            StringBuilder resStr = new StringBuilder();

            foreach (string st_ in masStr)
            {
                switch (i)
                {
                    case 1: resStr.Append(st_.Trim());
                        break;
                    case 2:
                        if (st_.Trim().Length > 1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    case 3: if (st_.Trim().Length > 1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    default:
                        break;
                }
                i++;
            }
            if (i != 4) return st.Trim();
            else return resStr.ToString();

        }

        //----------------------------------------------------------------------
        static public string IfmxGetPref(string kernel) //вытащить префикс
        //----------------------------------------------------------------------
        {
            if (kernel == null) return "";
            int k, l;
            k = kernel.LastIndexOf("_kernel");
            if ((k - 9) > 0) //вызов из ConnectionString
            {
                string s;
                k = kernel.LastIndexOf("_kernel");
                s = kernel.Substring(k - 9, 9);
                l = s.Length;
                k = s.LastIndexOf("=");
                return (s.Substring(k + 1, l - k - 1)).Trim();
            }
            else  //вызов из названии
            {
                return (kernel.Substring(0, k)).Trim();
            }
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbInitialCatalog(string st) //вытащить исходный каталог
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string[] result3 = dir.Split(new string[] { "\\\\" }, StringSplitOptions.None);
            string ndir = "";
            int l = result3.Length;

            foreach (string zap in result3)
            {
                if (l != 1) ndir += zap + "\\\\";
                l -= 1;
            }

            return ndir;
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbChangeDir(string st, string pref) //заменить путь к базе на pref
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string s = st.Replace(dir, pref);
            return s;
        }
        //----------------------------------------------------------------------
        static public bool ValInString(string st_in, string st_val, string st_split)
        //----------------------------------------------------------------------
        {
            string[] result = st_in.Split(new string[] { st_split }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                if (zap.Trim() == st_val.Trim())
                    return true;
            }

            return false;
        }

        //----------------------------------------------------------------------
        public static string IfmxFormatDatetimeToHour(string datahour, out Returns ret)
        //----------------------------------------------------------------------
        {
            //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч"
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
                string[] mas3 = hm.Split(new string[] { ":" }, StringSplitOptions.None);

                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + mas3[0].Trim();
                ret.result = true;
            }
            catch
            {
                return outs;
            }

            return outs;
        }
        //----------------------------------------------------------------------
        /// <summary> Подготовка строки для вставки в SQL-запрос (экранирование символов, добавление внешних кавычек)
        /// </summary>
        public static string EStrNull(string s)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, byte l)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, l, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, string defaultValue)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, defaultValue);
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, int l, string defaultValue)
        //----------------------------------------------------------------------
        {
            if (s == null) s = "";
            else s = s.Trim();
            if (s == "")
            {
                if (defaultValue.ToUpper() == "NULL")
                    return " " + defaultValue + " ";
                else s = defaultValue;
            }
            if (s.Length > l) s = s.Substring(0, l);
            return "'" + s.Replace("'", "\"") + "'";
        }

        //----------------------------------------------------------------------
        public static int EInt0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                int i;
                int.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static long ELong0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                long i;
                Int64.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static string ENull(string s)
        //----------------------------------------------------------------------
        {
            if (s == null)
                return "";
            else return s.Trim();
        }
        //----------------------------------------------------------------------
        public static string EFlo0(decimal f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f.ToString(), "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f, "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f, string _default)
        //----------------------------------------------------------------------
        {
            if (f.Trim() == "")
                return _default;
            else
            {
                NumberFormatInfo nfi = new CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";
                double d = Double.Parse(f.Replace(",", ".").Replace(" ", ""), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, nfi);
                return d.ToString("G", nfi);
            }
        }
        //----------------------------------------------------------------------
        public static string FormatDate(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return String.Format("{0:dd.MM.yyyy}", dt);
            }
            catch
            {
                return "";
            }
        }

        //----------------------------------------------------------------------
        public static string EDateNull(string d)
        //----------------------------------------------------------------------
        {
            if ((d == null) || (d.Trim() == ""))
            {
                return "null";

            }
            else
            {
                return "'" + d.Trim() + "'";
            }
        }

        //----------------------------------------------------------------------
        public static string FormatDateMDY(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return "mdy(" + String.Format("{0:MM,dd,yyyy}", dt) + ")";
            }
            catch
            {
                return "";
            }
        }

        /// <summary> проверяет наличие параметра в строке параметров, а также определяет порядковый номер параметра
        /// </summary>
        public static bool GetParams(string prms, int p)
        {
            int num;
            return GetParams(prms, p.ToString(), out num);
        }
        public static bool GetParams(string prms, string p)
        {
            int num;
            return GetParams(prms, p, out num);
        }
        public static bool GetParams(string prms, int p, out int sequenceNumber)
        {
            return GetParams(prms, p.ToString(), out sequenceNumber);
        }
        public static bool GetParams(string prms, string p, out int sequenceNumber)
        {
            sequenceNumber = -1;
            if (prms == null) return false;

            //Regex reg = new Regex(@",\d+", RegexOptions.IgnoreCase);
            var reg = new Regex(@"[^,]+", RegexOptions.IgnoreCase);

            foreach (Match match in reg.Matches(prms))
            {
                sequenceNumber++;
                if (match.Value == p) return true;
            }
            return false;
        }

        /// <summary>Удаляет параметр из строки параметров
        /// </summary>
        /// <param name="prms">Строка с параметрами в формате ,p1,p2,p3,... </param>
        /// <param name="p">Удаляемый параметр</param>
        /// <returns>Измененная строка параметров</returns>
        public static string RemoveParam(string prms, string p)
        {
            if (prms == null) return prms;

            string[] arr = prms.Split(',');
            string res = "";

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != p) res += "," + arr[i];
            }
            return res;
        }

        public class CssClasses
        {
            private List<string> _classes;
            private int _count;

            public CssClasses()
            {
                _classes = new List<string>();
                _count = 0;
            }

            public CssClasses(string classes)
            {
                string[] cls = classes.Trim().Split(' ');
                _classes = cls.ToList<string>();
                _count = _classes.Count;
            }

            public CssClasses AddClass(string className)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_classes[i] == className) return this;
                }
                _classes.Add(className);
                _count++;
                return this;
            }

            public CssClasses RemoveClass(string className)
            {
                if (className != "")
                {
                    while (_classes.IndexOf(className) >= 0) _classes.Remove(className);
                }
                _count = _classes.Count;
                return this;
            }

            public override string ToString()
            {
                string result = "";
                for (int i = 0; i < _count; i++)
                {
                    result += " " + _classes[i];
                }
                return result.Trim();
            }
        }

        //----------------------------------------------------------------------
        public static int PutIdMonth(int y, int m)
        //----------------------------------------------------------------------
        {
            int i = 0;
            int.TryParse(y.ToString() + m.ToString("00"), out i);
            return i;
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(int id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            GetIdMonth(id.ToString(), ref y, ref m);
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(string id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            try
            {
                y = Convert.ToInt32(id.Substring(0, 4));
                m = Convert.ToInt32(id.Substring(4, 2));
            }
            catch
            {
                y = 0;
                m = 0;
            }
        }

        //----------------------------------------------------------------------
        public static int GetInt(string s)
        //----------------------------------------------------------------------
        {
            if (s == "")
                return 0;
            else
            {
                int i;
                try
                {
                    i = Convert.ToInt32(s);
                    return i;
                }
                catch { }

                int l = s.Length;
                int k = 1;
                while (k < l)
                {
                    s = s.Substring(0, l - k);
                    try
                    {
                        i = Convert.ToInt32(s);
                        return i;
                    }
                    catch { }

                    k = k + 1;
                }
                return 0;
            }
        }


        public static string GetSN(string sn)
        //----------------------------------------------------------------------
        {
            string[] result = sn.Split(new string[] { "-" }, StringSplitOptions.None);
            sn = "";
            for (int i = 0; i < result.Length; i = i + 1)
            {
                sn += result[i];
            }

            return (sn.Trim()).ToUpper();
        }
        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC10(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(9, c);

            int sum = 0;
            string s = "";
            for (int i = 1; i <= barcode.Length; i++)
            {
                s = barcode.Substring(i - 1, 1);
                if (i != 10)
                {
                    if (i % 2 == 0)
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);

            return Convert.ToInt64(s);
        }


        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC13(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(12, c);

            int sum = 0;
            string s = "";
            for (int i = 0; i < barcode.Length; i++)
            {
                s = barcode.Substring(i, 1);
                if (i != 12)
                {
                    if (i % 2 == 0)
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);

            return Convert.ToInt64(s);
        }

        //----------------------------------------------------------------------
        public static string GetKontrSamara(string als)
        //----------------------------------------------------------------------
        {

            int sum_mod = 0;
            int i;
            int j;
            int first_k;
            int second_k;
            string ss;

            for (i = 0; i < als.Length; i++)
            {
                ss = als.Substring(i, 1);
                j = Convert.ToInt32(ss);
                if (i % 2 == 0)
                {
                    switch (j)
                    {
                        case 0: sum_mod = sum_mod + 4; break;
                        case 1: sum_mod = sum_mod + 6; break;
                        case 2: sum_mod = sum_mod + 8; break;
                        case 3: sum_mod = sum_mod + 1; break;
                        case 4: sum_mod = sum_mod + 3; break;
                        case 5: sum_mod = sum_mod + 5; break;
                        case 6: sum_mod = sum_mod + 7; break;
                        case 7: sum_mod = sum_mod + 9; break;
                        case 8: sum_mod = sum_mod + 2; break;
                        case 9: sum_mod = sum_mod + 0; break;
                    }
                }
                else sum_mod = sum_mod + j;
            }
            first_k = (10 - sum_mod % 10) % 10;

            sum_mod = 0;

            for (i = 0; i < Math.Min(11, als.Length); i++)
            {
                j = Convert.ToInt16(als.Substring(i, 1));
                switch (i + 1)
                {
                    case 1: sum_mod = sum_mod + j * 6; break;
                    case 2: sum_mod = sum_mod + j * 5; break;
                    case 3: sum_mod = sum_mod + j * 4; break;
                    case 4: sum_mod = sum_mod + j * 3; break;
                    case 5: sum_mod = sum_mod + j * 2; break;
                    case 6: sum_mod = sum_mod + j * 1; break;
                    case 7: sum_mod = sum_mod + j * 1; break;
                    case 8: sum_mod = sum_mod + j * 2; break;
                    case 9: sum_mod = sum_mod + j * 3; break;
                    case 10: sum_mod = sum_mod + j * 4; break;
                    case 11: sum_mod = sum_mod + j * 5; break;
                }
            }

            second_k = (10 - (sum_mod % 10)) % 10;

            return first_k.ToString() + second_k.ToString();

        }


        public static string BarcodeCrcSamara(string acode)
        {
            var sum = 0;

            for (int i = 0; i < acode.Length; i++)
            {
                switch (i + 1)
                {
                    case 1:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 29;
                        break;
                    case 2:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 27;
                        break;
                    case 3:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 25;
                        break;
                    case 4:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 23;
                        break;
                    case 5:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 21;
                        break;
                    case 6:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 19;
                        break;
                    case 7:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 17;
                        break;
                    case 8:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 15;
                        break;
                    case 9:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 13;
                        break;
                    case 10:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 11;
                        break;
                    case 11:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 9;
                        break;
                    case 12:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 7;
                        break;
                    case 13:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 5;
                        break;
                    case 14:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 3;
                        break;
                    case 15:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 1;
                        break;
                    case 16:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 2;
                        break;
                    case 17:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 4;
                        break;
                    case 18:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 6;
                        break;
                    case 19:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 8;
                        break;
                    case 20:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 10;
                        break;
                    case 21:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 12;
                        break;
                    case 22:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 14;
                        break;
                    case 23:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 16;
                        break;
                    case 24:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 18;
                        break;
                    case 25:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 20;
                        break;
                    case 26:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 22;
                        break;
                    case 27:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 24;
                        break;
                    case 28:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 26;
                        break;

                }

            }

            String s = (sum % 99).ToString("00");

            return s.Substring(0, 2);

        }

        //----------------------------------------------------------------------
        /// <summary> Установить региональные настройки
        /// </summary>
        public static void setCulture()
        //----------------------------------------------------------------------
        {
            CultureInfo culture = new CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            culture.DateTimeFormat.ShortTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }



        /// <summary>
        /// получение названия месяца по целочисленному значению
        /// </summary>
        /// <param name="month">месяц</param>
        /// <returns></returns>
        static public string GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "Январь";
                case 2:
                    return "Февраль";
                case 3:
                    return "Март";
                case 4:
                    return "Апрель";
                case 5:
                    return "Май";
                case 6:
                    return "Июнь";
                case 7:
                    return "Июль";
                case 8:
                    return "Август";
                case 9:
                    return "Сентябрь";
                case 10:
                    return "Октябрь";
                case 11:
                    return "Ноябрь";
                case 12:
                    return "Декабрь";
                default:
                    return "";
            }
        }

        /// <summary>
        /// процедура подсчета MD5 строки
        /// </summary>
        /// <param name="input">строка</param>
        /// <returns></returns>
        static public string CreateMD5StringHash(string input)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }

        public static bool PrmValToBool(this string s)
        {
            if (s != null && s != "")
            {
                int i = 0;
                int.TryParse(s, out i);
                return i == 1 ? true : false;
            }
            return false;
        }

        public class SplitedAddress
        {
            public string street;
            public string dom;
            public string kor;
            public string kvar;

            public SplitedAddress()
            {
                street = "";
                dom = "";
                kor = "";
                kvar = "";
            }
        }

        public static SplitedAddress SplitAddress(string address)
        {
            var adr = new SplitedAddress();
            if (address.IndexOf("УМЕР") == -1 && address.IndexOf(',') != -1)
            {
                adr.street = address.Substring(0, address.IndexOf(','));
                address = address.Substring(address.IndexOf(',') + 1).Trim();

                if (address.IndexOf("Д.") != -1)
                {
                    address = address.Substring(address.IndexOf("Д.") + 2).Trim();
                    adr.dom = address.Substring(0, address.IndexOf(' '));
                }
                else
                {
                    int dom;
                    if (address.IndexOf(',') == -1)
                    {
                        if (Int32.TryParse(address.Trim(), out dom))
                        {
                            adr.dom = dom.ToString();
                        }
                        else
                        {
                            adr.dom = address.Substring(0, address.Length - 1);
                            adr.kor = address.Substring(address.Length - 1, 1);
                        }
                    }
                    else if (Int32.TryParse(address.Trim().Substring(0, address.IndexOf(',')), out dom))
                    {
                        adr.dom = dom.ToString();
                    }
                    else
                    {
                        adr.dom = address.Substring(0, address.IndexOf(',') - 1);
                        adr.kor = address.Substring(address.IndexOf(',') - 1, 1);
                    }
                }

                if (address.IndexOf("КОРПУС") != -1)
                {
                    address = address.Substring(address.IndexOf("КОРПУС") + 6).Trim();
                    adr.kor = address.Substring(0, 1);
                }
                if (address.IndexOf(',') != -1)
                {
                    address = address.Substring(address.IndexOf(',') + 1).Trim();
                    int kvar;
                    if (Int32.TryParse(address, out kvar))
                        adr.kvar = kvar.ToString();
                }
            }

            return adr;
        }
    }
}
