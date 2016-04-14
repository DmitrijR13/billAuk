using System.Configuration;
using System.Threading;
using Bars.KP50.Utils;
using Bars.KP50.Utils.Extension;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using IBM.Data.Informix;

    using Npgsql;

    using STCLINE.KP50.Global;
    using System.Diagnostics;

    public static class DBManager
    {
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
        /// <returns>Sql комманда</returns>
        public static IDbCommand newDbCommand(string sqlString, IDbConnection connection, IDbTransaction transaction = null)
        {
            // какое подключение передали, то такую команду и возвращаем
            //#if DEBUG

            if (Points.FullLogging && sqlString.IndexOf("Select 1 From " + sDefaultSchema + "calc_fon_", StringComparison.Ordinal) < 0
                && sqlString.IndexOf("Select * From " + sDefaultSchema + "calc_fon_", StringComparison.Ordinal) < 0
                && sqlString.IndexOf("Select *  From " + sDefaultSchema + "bill_fon", StringComparison.Ordinal) < 0)
            {
                if (connection != null)
                    MonitorLog.WriteLog(" Выполнение запроса " + (transaction != null ? " с транзакцией " : " ") +
                        (connection is NpgsqlConnection ?
                            "[ProcessID: " + (connection as NpgsqlConnection).ProcessID + "] " : string.Empty) +
                        (connection is MyDbConnection && (connection as MyDbConnection).RealConnection is NpgsqlConnection ?
                            "[ProcessID: " + ((connection as MyDbConnection).RealConnection as NpgsqlConnection).ProcessID + "] " : string.Empty) +
                        sqlString + " ", MonitorLog.typelog.Info, 1, 1, true);
                else
                    MonitorLog.WriteLog(" Выполнение запроса connection = null " + sqlString, MonitorLog.typelog.Info, 1,
                        1, true);
            }
            //#endif

#if PG
            if (connection.GetType() == typeof(NpgsqlConnection))
            {
                return new NpgsqlCommand(sqlString, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);
            }
            else if (connection.GetType() == typeof(MyDbConnection))
            {
                return new NpgsqlCommand(sqlString, (NpgsqlConnection)(((MyDbConnection)connection).RealConnection), (NpgsqlTransaction)transaction);
            }
            else
            {
                return new NpgsqlCommand(sqlString, (NpgsqlConnection)(((MyDbConnection)connection).RealConnection), (NpgsqlTransaction)transaction);
            }
#else
            if (connection.GetType() == typeof(IfxConnection))
            {
                return new IfxCommand(sqlString, (IfxConnection)connection, (IfxTransaction)transaction);
            }
            else if (connection.GetType() == typeof(MyDbConnection))
            {
                return new IfxCommand(sqlString, (connection as MyDbConnection).RealConnection as IfxConnection, (IfxTransaction)transaction);
            }
#endif
            /*
            if (Points.FullLogging &&
                !(new Regex(@"^\s*select\s+.\s+from\s+" + sDefaultSchema.Replace(".", @"\.") + @"(calc|bill)_fon.*$", 
                    RegexOptions.IgnoreCase)).IsMatch(sqlString))
                MonitorLog.WriteLog(
                    connection != null ?
                            "Выполнение запроса " +
                            (transaction != null ? " с транзакцией " : string.Empty) +
                            (connection is NpgsqlConnection ?
                                "[ProcessID: " + (connection as NpgsqlConnection).ProcessID + "] " : string.Empty) +
                            (connection is MyDbConnection && (connection as MyDbConnection).RealConnection is NpgsqlConnection ?
                                "[ProcessID: " + ((connection as MyDbConnection).RealConnection as NpgsqlConnection).ProcessID + "] " : string.Empty) +
                            sqlString :
                        " Выполнение запроса connection = null " + sqlString, 
                    MonitorLog.typelog.Info, 1, 1, true);

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sqlString;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 600;
            return command;
            */
        }

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

        // создание нового подключения по строке соединения
        public static IDbConnection newDbConnection(string connectionString)
        {

            // пока жестко возвращаем подключение к informix
            // потом будем думать как разделять подключения
#if PG
            //connectionString = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=websmr;Preload Reader=true;";
            return new NpgsqlConnection(connectionString);
#else
            // TODO: убрать это?!
            connectionString = connectionString.Replace("UID", "User ID");
            connectionString = connectionString.Replace("Pwd", "Password");
            IfxConnectionStringBuilder conn = new IfxConnectionStringBuilder(connectionString);
            return new IfxConnection(conn.ToString());
#endif
        }

        // получить информацию о сервере подключения
        // (т.к. нет в стандартном интерфейсе IDbConnection)
        public static string getServer(IDbConnection connectionID)
        {
            // какое подключение передали, то такой сервер и возвращаем
#if PG
            if (connectionID is NpgsqlConnection)
            {
                //return (connectionID as NpgsqlConnection).Host;
                //TODO: для Postgresql сервер будет определять потом, это например будет год и месяц и он будет подставляться в конце имени схемы
                return "";
            }
#else
            if (connectionID.GetType() == typeof(IfxConnection))
            {
                return (connectionID as IfxConnection).Server;
            }
            else if (connectionID.GetType() == typeof(MyDbConnection))
            {
                return ((connectionID as MyDbConnection).RealConnection as IfxConnection).Server;
            }
#endif
            return null;
        }

        public static System.Data.Common.DbConnectionStringBuilder getDbStringBuilder(string connectionString)
        {
#if PG
            return new NpgsqlConnectionStringBuilder(connectionString);
#else
            return new IfxConnectionStringBuilder(connectionString);
#endif
        }

        public static string getDbName(string connectionString)
        {
#if PG
            //connectionString = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=websmr;Preload Reader=true;";
            var conn = new NpgsqlConnectionStringBuilder(connectionString);
#else
            // TODO: убрать это?!
            connectionString = connectionString.Replace("UID", "User ID");
            connectionString = connectionString.Replace("Pwd", "Password");
            IfxConnectionStringBuilder conn = new IfxConnectionStringBuilder(connectionString);
#endif
            return conn.Database;
        }

        public const string BasePwd = "IfmxPwd2";

#if PG
#else
        /// <summary>
        /// Время ожидания снятия блокировки с таблицы
        /// </summary>
        public const int WaitingTimeout = 5;
#endif

        public const string ConfPref = "W";

        public const bool LongType = false;

        /// <summary>
        /// Разделитель наименования базы данных (схемы) и таблицы
        /// </summary>
        public static string tableDelimiter
        {
            get
            {
#if PG
                return ".";
#else
                return ":";
#endif
            }
        }

        /// <summary>
        /// ключевые слова для перевода Informix в PostgreSQL
        /// </summary>
#if PG
        public const string sKernelAliasRest = "_kernel.";
        public const string sDataAliasRest = "_data.";
        public const string sUploadAliasRest = "_upload.";
        public const string sSupgAliasRest = "_supg.";
        public const string sDebtAliasRest = "_debt.";
        public const string tbluser = "";
        public const string sDecimalType = "numeric";
        public const string sCharType = "character";
        public const string sUniqueWord = "distinct";
        public const string sNvlWord = "coalesce";
        public const string sConvToNum = "::numeric";
        public const string sConvToInt = "::int";
        public const string sConvToChar = "::character";
        public const string sConvToChar10 = "::character(10)";
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
        public const string Limit1 = " limit 1 ";
        public const string First1 = "";

#else
        public const string sKernelAliasRest = "_kernel:";
        public const string sDataAliasRest   = "_data:";
        public const string sUploadAliasRest = "_upload:";
        public const string sSupgAliasRest = "_supg:";
        public const string sDebtAliasRest = "_debt:";
        public const string tbluser = "are.";
        public const string sDecimalType = "decimal";
        public const string sCharType = "char";
        public const string sUniqueWord  = "unique";
        public const string sNvlWord     = "nvl";
        public const string sConvToNum   = "+0";
        public const string sConvToInt   = "+0";
        public const string sConvToChar10 = "";
        public const string sConvToChar = "";
        public const string sConvToVarChar = "";
        public const string sConvToDate = "";
        public const string sDefaultSchema = "";
        public const string s0hour = "0 units hour";
        public const string sUpdStat = "Update statistics for table";
        public const string sCrtTempTable = "temp";
        public const string sUnlogTempTable = " with no log";
        public const string sCurDate = "today";
        public const string sCurDateTime = "current";
        public const string DateNullString = "''";        
        public const string sFirstWord = "first";
        public const string sSerialDefault = "0";
        public const string sYearFromDate = "year(";
        public const string sMonthFromDate = "month(";
        public const string sDateTimeType = "datetime year to second";
        public const string sLockMode = " lock mode row";
        public const string sMatchesWord = "matches";
        public const string sRegularExpressionAnySymbol = "*";
        public const string Limit1 = "";
        public const string First1 = " first 1 ";
#endif

        public static string SetLimitOffset(string sql, int limit, int offset)
        {
#if PG
            sql = sql.Insert(sql.Length, limit != 0 ? " limit " + limit : "");
            sql = sql.Insert(sql.Length, offset != 0 ? " offset " + offset : "");
#else
            sql = sql.Insert(sql.IndexOf("select") + 6, offset!=0 ? " skip " + offset : "");
            sql = sql.Insert(offset != 0 ? sql.IndexOf(" skip ") + 6 + offset.ToString().Length : sql.IndexOf("select") + 6, limit != 0 ? " first " + limit : "");
#endif
            return sql;
        }

        public static string SetSubString(string str, string start, string end)
        {
#if PG
            return " substring(" + str + " from " + start + " +1 for " + end + ") ";
#else
            return " substr(" + str + ", " + start + ", " + end + ") ";
#endif

        }

        public static string SetInterval(string interval, string kind)
        {
            string result = String.Empty;
            switch (kind)
            {
#if PG
                case "minute":
                    result = "extract (epoch from " + interval + ")::int/60";
                    break;
                case "hour":
                    result = "extract (epoch from " + interval + ")::int/3600";
                    break;
                case "day":
                    result = "extract (epoch from " + interval + ")::int/86400";
                    break;
#else
                case "minute":
                    result = "(" + interval + ")::interval  minute(6) to minute";
                    break;
                case "hour":
                    result = "(" + interval + ")::interval  minute(6) to minute/60";
                    break;
                case "day":
                    result = "(" + interval + ")::interval  minute(6) to minute/1440";
                    break;
#endif
            }
            return result;
        }

        public static string SetTempTable(string query, string tempTableName)
        {
#if PG
            return query.Substring(0, query.ToLower().IndexOf("from") - 1) + " into temp " + tempTableName +
                   query.Substring(query.ToLower().IndexOf("from") - 1);
#else
            return query + " into temp " + tempTableName;
#endif
        }

        public static IDbConnection GetConnection(string connectionString)
        {
            return Constants.UseExtendedConnectionfactory ?
                DatabaseConnectionKernel.GetConnection(connectionString) :
                newDbConnection(connectionString);
        }

        public static Returns OpenDb(IDbConnection connection, bool inlog)
        {
            string err;
            Returns ret;
            //#if DEBUG
            if (Points.FullLogging)
            {
                if (connection != null)
                    MonitorLog.WriteLog(" Открытие БД " + connection, MonitorLog.typelog.Info, 1, 1, true);
                else
                    MonitorLog.WriteLog(" Открытие БД connection = null", MonitorLog.typelog.Info, 1, 1, true);
            }
            //#endif
            ret = Utils.InitReturns();
            try
            {
                if (connection.State == ConnectionState.Closed) connection.Open();
                else if (connection.State == ConnectionState.Broken)
                {
                    connection.Close();
                    connection.Open();
                }


#if PG
#else
                ExecSQL(connection, "set lock mode to " + (WaitingTimeout > 0 ? " wait " + WaitingTimeout : "not wait"), true);
#endif
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка доступа к базе. ";
                ret.sql_error = (connection != null ? " БД " + connection.Database + ": " : "") + ex.Message;
                err = ret.text + " \n  " + ret.sql_error + "\n" + ex.StackTrace;

                //#if DEBUG
                if (Points.FullLogging)
                {
                    if (connection != null)
                        MonitorLog.WriteLog(" Открытие БД неудачно " + connection, MonitorLog.typelog.Info, 1, 1, true);
                    else
                        MonitorLog.WriteLog(" Открытие БД неудачно connection = null", MonitorLog.typelog.Info, 1, 1,
                            true);
                }
                //#endif
                if (inlog)
                {
                    MonitorLog.WriteLog(err, MonitorLog.typelog.Error, 1, 1, true);
                }

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

                connection.Close();

                return ret;
            }
        }

        public static void CloseDb(IDbConnection connection)
        {
            //#if DEBUG
            if (Points.FullLogging)
            {
                if (connection != null)
                    MonitorLog.WriteLog(" Закрытие БД " + connection, MonitorLog.typelog.Info, 1, 1, true);
                else
                    MonitorLog.WriteLog(" Закрытие БД connection = null", MonitorLog.typelog.Info, 1, 1, true);
            }
            //#endif
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


        public static Returns ExecSQL(IDbConnection connection, IDbTransaction transaction, string sql, bool inlog, int time)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
#if DEBUG
            var st = new Stopwatch();
            if (Constants.TraceLong)
            {
                st.Start();
            }
#endif
            Returns ret;

            ret = Utils.InitReturns();

#if PG
            sql = sql.PgNormalize(connection);
#endif

            IDbCommand cmd = null;

            try
            {
                cmd = newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = time;
                _affectedRowsCount = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка выполнения операции в базе данных ";

                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                    " " + GetIfxError(ex); ;

                if (ex.Message == "ОШИБКА: 21000: подзапрос в выражении вернул больше одной строки")
                    ret.sql_error += " \n Возможна ошибка в данных  \n";
                if (ex.Message.ContainsIgnoreCase("ОШИБКА: 57014") || ex.Message.ContainsIgnoreCase("A timeout has occured"))
                {
                    HandlerSqlExplain(connection, transaction, sql, cmd);
                }

                string err = Environment.NewLine +
                    ret.text + " \n " + ret.sql_error;

                if (inlog)
                {

                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                        err += stackFrame.GetMethod().Name + " \n"; // write method name
                    }


                    MonitorLog.WriteException(err);
                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                }


                if (Constants.Viewerror)
                {
                    ret.text = err;
                }
            }
            finally
            {
#if DEBUG
                if (Constants.TraceLong)
                {
                    st.Stop();
                    if (st.Elapsed.TotalSeconds > Constants.ThresHoldTimeQuery || _affectedRowsCount > Constants.ThresHoldAffectedRows)
                    {
                        MonitorLog.WriteLog("affectedRowsCount:" + _affectedRowsCount + " time:" + st.Elapsed.TotalSeconds + Environment.NewLine +
                            "  sql:" + cmd.CommandText, MonitorLog.typelog.Warn,
                            1, 1, true);
                    }
                }
#endif
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

            return ret;
        }

        private static void HandlerSqlExplain(IDbConnection connection, IDbTransaction transaction, string sql, IDbCommand cmd)
        {
            if (!(sql.ToUpper().Contains("TRUNCATE ")
                  || sql.ToUpper().Contains("DROP ")
                  || sql.ToUpper().Contains("CREATE ")
                  || sql.ToUpper().Contains("ANALYZE ")
                  || (sql.ToUpper().Contains("SET ") && !sql.ToUpper().Contains("UPDATE "))))
            {
                try
                {
                    var DT =
                        ClassDBUtils.OpenSQL(" EXPLAIN " + sql, "table", null, connection, transaction,
                            ClassDBUtils.ExecMode.Exception).resultData;
                    var explainText = new StringBuilder();
                    foreach (DataRow row in DT.Rows)
                    {
                        explainText.AppendLine(row[0].ToString());
                    }
                    MonitorLog.WriteLog(
                        " БД " + connection.Database + " \n '" + sql + "' \n Таймаут по запросу: " + cmd.CommandTimeout +
                        " сек. \n EXPLAIN:" + explainText,
                        MonitorLog.typelog.Warn, 1, 1, true);
                }
                catch (Exception innerEx)
                {
                    var errMsg = " БД " + connection.Database + " \n '" + sql + "' \n " + innerEx.Message + " " +
                                 GetIfxError(innerEx);
                    MonitorLog.WriteLog(" Ошибка анализа запроса:" + sql + " " + errMsg,
                        MonitorLog.typelog.Error,
                        1, 1, true);
                }
            }
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
            var ret = Utils.InitReturns();


#if PG
            sql = sql.PgNormalize(connection);
#endif

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
                ret.result = false;
                ret.text = "Ошибка чтения из базы данных ";
                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                    " " + GetIfxError(ex) + " \n " + connection + ": type = " + connection.GetType() +
                    ", datatbase = " + connection.Database + ", state = " + connection.State + " \n";
                string err = ret.text + " \n " + ret.sql_error;

                if (inlog)
                {
                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                        err += stackFrame.GetMethod().Name + " \n"; // write method name
                    }

                    MonitorLog.WriteException(err);
                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                }



                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

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
            Returns ret = Utils.InitReturns();


#if PG
            sql = sql.PgNormalize(connection);
#endif

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
                ret.text = "Ошибка чтения из базы данных ";
                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                                " " + GetIfxError(ex) + " \n " + connection + ": type = " + connection.GetType() +
                                ", datatbase = " + connection.Database + ", state = " + connection.State + " \n";
                string err = ret.text + " \n " + ret.sql_error;



                if (inlog)
                {
                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                        err += stackFrame.GetMethod().Name + " \n"; // write method name
                    }

                    //MonitorLog.WriteLog(err, MonitorLog.typelog.Error, 1, 3, true);
                    MonitorLog.WriteException(err);
                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                }

                if (Constants.Viewerror) ret.text = err;

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


        /// <summary>Получить результат sql запроса в виде типизированного значения</summary>
        /// <param name="sql">Sql запрос</param>
        public static T ExecScalar<T>(IDbConnection connectionID, string sql, out Returns ret, bool inlog) where T : struct
        {
            return ExecScalar<T>(connectionID, sql, out ret, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде типизированного значения</summary>
        /// <param name="sql">Sql запрос</param>
        public static T ExecScalar<T>(IDbConnection connection, string sql, out Returns ret, bool inlog, int timeout) where T : struct
        {
            var obj = ExecScalar(connection, null, sql, out ret, inlog, timeout);
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
            ret = Utils.InitReturns();


#if PG
            sql = sql.PgNormalize(connection);
#endif

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
                ret.text = "Ошибка одиночного чтения из базы данных ";
                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message + " " +
                    GetIfxError(ex) +
                " \n " + connection + ": type = " + connection.GetType() +
                ", datatbase = " + connection.Database + ", state = " + connection.State + " \n";
                string err = ret.text + " \n " + ret.sql_error;


                if (inlog)
                {
                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                        err += stackFrame.GetMethod().Name + " \n"; // write method name
                    }

                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                    MonitorLog.WriteException(err);
                    //string stateObject = String.Format("{0}Состояние объектов: connection.State:{1} ",
                    //    Environment.NewLine, connection.State.ToString());
                    //MonitorLog.WriteLog(stateObject, MonitorLog.typelog.Warn, true);
                }

                if (Constants.Viewerror)
                {
                    ret.text = err;
                }

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
#if PG
            int res = Constants._ZERO_;
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
#else
            int res = Constants._ZERO_;
            MyDataReader reader;
            if (!ExecRead(conn, transaction, out reader, "Select first 1 dbinfo('sqlca.sqlerrd" + tip + "') as key From systables", false).result)
            {
                return res;
            }
            try
            {
                if (reader.Read())
                {
                    res = Convert.ToInt32(reader["key"]);
                }
            }
            finally
            {
                reader.Close();
            }
            return res;
#endif
        }

        public static int LogSQL(IDbConnection conn_web, int nzp_user, string sql)
        {
            return -1;
            //if (!Constants.Debug) return -1;

            //int res = 0;

            //string s = sql.Replace("  ", " ").Trim();
            //s = s.Replace("  ", "");
            //s = s.Replace("'", "*");

            //int l = s.Length;
            //int k = 0;
            //bool b = true;

            //while (k < l)
            //{
            //    int u = 255;
            //    if (k + u > l) u = l - k;
            //    string sq;
            //    try
            //    {
            //        sq = s.Substring(k, u);
            //    }
            //    catch
            //    {
            //        return -191;
            //    }



            //    string sql_txt =
            //        " Insert into log_sql (nzp_user, dat_log, err_kod, sql_txt, sql_err)  Values ("
            //        + nzp_user + ", " + DBManager.sCurDate + ", " + res + ", '" + sq + "', null ) ";




            //    if (!ExecSQL(conn_web, sql_txt, false).result)
            //    {
            //        return -192;
            //    }
            //    if (b)
            //    {

            //        res = GetSerialValue(conn_web);
            //        b = false;
            //    }
            //    k += u;
            //}
            //return res;
        }

        public static bool LogSQL_Error(IDbConnection conn_web, int key, string err)
        {
            if (!Constants.Debug) return true;

            return
                ExecSQL(
                        conn_web,
                    " Update log_sql " + " Set err_kod = -1, sql_err = '" + err.Trim() + "'" + " Where nzp_lsql = " + key.ToString(),
                    false).result;
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
#if PG
 " Select * From information_schema.schemata Where schema_name = '" + db.Trim().ToLower() + "'",
                false);
#else
 " Select * From sysmaster:sysdatabases Where name = '" + db.Trim().ToLower() + "'", false);
#endif
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
                MonitorLog.WriteLog(
                                    "При определении наличия банка данных " + db + " произошла ошибка.\n" + ex.Message,
                    MonitorLog.typelog.Warn,
                    30,
                    1,
                    true);
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
#if PG
            return ProcedureExists(conn_web, proc, db);
#else
            return (ProcedureInWebCasheID(conn_web, proc, db) > 0);
#endif
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
            int.TryParse(ret.text, out procCount);

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
#if PG
 " select pg_class.oid as tabid from pg_class inner join pg_namespace on pg_class.relnamespace = pg_namespace.oid Where lower(relname) = '"
                + tab.Trim().ToLower() + "' and pg_namespace.nspname = CURRENT_SCHEMA()",
                false).result)
#else
 " Select tabid From systables Where lower(tabname) = '" + tab.Trim().ToLower() + "'", false).result)
#endif
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
            //todo postgreSQL All function
            IDataReader reader;
            int tabid = 0;
            base_name = base_name.Trim();
            Returns ret;
#if PG
            string sql = //" Select COUNT(*) as tabid From pg_class Where lower(relname) = '" + tab.Trim().ToLower() + "'";
                "select COUNT(*) as tabid from pg_tables where lower(tablename)='" + tab.Trim().ToLower() + "' and lower(schemaname)='" + base_name + "'";
#else
            string sql = " Select tabid From " + base_name + ":systables Where lower(tabname) = '" + tab.Trim().ToLower() + "'";
#endif

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

#if PG
            string sql = " SELECT * FROM " + tab + " LIMIT 1; ";
#else
            string sql = " SELECT first 1 * FROM " + tab ;
#endif
            if (!ExecRead(connection, transaction, out reader, sql, false).result)
            {
                return false;
            }
            reader.Close();
            return true;
            //#if PG
            //            string[] tabname = tab.Split(new char[1] { '.' }).Select(x => x.Trim()).ToArray(); ;
            //            string schema = " CURRENT_SCHEMA() ";
            //            string tablename = tab;
            //            if (tabname.Length > 1)
            //            {
            //                schema = "'" + tabname[0] + "'";
            //                tablename = tabname[1];
            //            }
            //            if (
            //                !ExecRead(
            //                          connection,
            //                    transaction,
            //                    out reader,
            //                    String.Format(" Select table_name as tabname From information_schema.tables "+
            //                    "Where lower(table_name) = lower('{0}') and ((table_schema={1} and table_type='BASE TABLE') "+
            //                    "or (table_type='LOCAL TEMPORARY'))", tablename, schema),
            //                    false).result)
            //            {
            //                return false;
            //            }
            //            try
            //            {
            //                if (reader.Read())
            //                {
            //                    return true;
            //                }
            //                else
            //                {
            //                    return false;
            //                }
            //            }
            //            catch
            //            {
            //                return false;
            //            }
            //            finally
            //            {
            //                reader.Close();
            //            }
            //#else
            //            if (!ExecRead(connection, transaction, out reader, "Select * From " + tab, false).result)
            //            {
            //                return false;
            //            }
            //            reader.Close();
            //            reader = null;
            //            return true;
            //#endif
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

        /// <summary>
        /// Возвращает текущий расчетный месяц для отчетов
        /// </summary>
        /// <returns></returns>
        public static DataTable GetCurMonthYear()
        {
            //DataTable monthYear;
            //try
            //{
            //    string connectionString = Encryptor.Decrypt(ConfigurationManager.AppSettings[DBManager.ConfPref + "4"],
            //        null);
            //    string pref = Encryptor.Decrypt(ConfigurationManager.AppSettings[DBManager.ConfPref + "10"], null);
            //    var connection = DBManager.GetConnection(connectionString);
            //    connection.Open();
            //    monthYear = DBManager.ExecSQLToTable(connection,
            //        " select month_, yearr from " + pref + DBManager.sDataAliasRest + "saldo_date where iscurrent = 0 ");
            //    connection.Close();
            //}
            //catch (Exception e)
            //{
            //    monthYear = null;
            //    MonitorLog.WriteException("Ошибка при попытке вытащить текущий расчетный месяц", e);
            //}
            //return monthYear;
            return null;
        }

        /// <summary>
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
            return true;
#if PG
            string dop = "";
            if (cur_pg_dn != "") dop = " and lower(table_schema)= " + Utils.EStrNull(cur_pg_dn.Trim().ToLower());
#endif
            MyDataReader reader;
            if (!ExecRead(
                          conn_id,
                transaction,
                out reader,
#if PG
 " Select column_name From information_schema.columns " + " Where " + "  lower(table_name)  = " + Utils.EStrNull(tab.Trim().ToLower())
                + " " + " and lower(column_name)= " + Utils.EStrNull(column.Trim().ToLower())
                + dop,
                true).result)
#else
 " Select * From systables t, syscolumns c " +
                " Where c.tabid = t.tabid " +
                "  and lower(tabname)  = " + Utils.EStrNull(tab.Trim().ToLower()) +
                 " and lower(c.colname)= " + Utils.EStrNull(column.Trim().ToLower()), true).result)
#endif
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
#if PG
 " SELECT * FROM pg_class c, pg_namespace n WHERE c.relnamespace = n.oid AND lower(relname) = "
                + Utils.EStrNull(index.Trim().ToLower()) + " AND relkind = 'i'"
#else
 " Select * From sysindexes " +
                " Where lower(idxname)  = " + Utils.EStrNull(index.Trim().ToLower())
#endif
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
#if PG
 " Select table_name as tabname From information_schema.tables" + " Where lower(table_name) = '" + table + "' and table_schema='" + db
                + "' and table_type='BASE TABLE'",
                true).result)
#else
 " Select tabname From " + db + ":systables " +
                " Where lower(tabname) matches '" + table + "' and tabid > 100", true).result)
#endif
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
#if PG
            long i_min = 0;
            long i_max = 0;
            sql = sql.PgNormalize(conn);
            // для Postgresql нет смысла делать операции по частям
            // - сделаем всё сразу
            ret = ExecSQL(conn, sql + " " + groupby, true, 60000);
            return;
#else 
            int i_min = 0;
            int i_max = 0;
#endif

            MyDataReader reader;
            ret = ExecRead(conn, out reader, " Select max(" + pole + ") as i_max, min(" + pole + ") as i_min From " + tab, true);
            if (!ret.result)
            {
                return;
            }

            try
            {
                if (reader.Read())
                {
                    if (reader["i_max"] != DBNull.Value)
                    {
#if PG
                        //i_max = (long)reader["i_max"];
                        i_max = Convert.ToInt64(reader["i_max"]);
#else
                        i_max = (int)reader["i_max"];
#endif
                    }
                    else
                    {
                        return;
                    }

                    if (reader["i_min"] != DBNull.Value)
                    {
#if PG
                        //i_min = (long)reader["i_min"];
                        i_min = Convert.ToInt64(reader["i_min"]);
#else
                        i_min = (int)reader["i_min"];
#endif
                    }
                    else
                    {
                        return;
                    }

                    string s;

                    while (i_min <= i_max)
                    {
                        s = sql + " and " + pole + ">=" + i_min + " and " + pole + "<=" + (i_min + step) + " " + groupby;
                        ret = ExecSQL(conn, s, true, 6000);
                        if (!ret.result)
                        {
                            return;
                        }

                        i_min = i_min + step + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка считывания min-max " + tab;
                MonitorLog.WriteLog(ret.text + "\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                reader.Close();
            }
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


        public static void CreateIndexIfNotExists(IDbConnection conn, string indexName, string tableName, string indexColumns, bool inLog = false, IDbTransaction dbTransaction = null)
        {

            indexColumns = indexColumns.Replace("(", string.Empty).Replace(")", string.Empty);
            var tableParts = tableName.Split(new[] { tableDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (tableParts.Count() != 2)
            {
                throw new ArgumentException("Wrong table format! Expected <schema>.<table> format!");
            }

            var schema = tableParts[0];
            var table = tableParts[1];
#if PG
            string sql = string.Format(" set SEARCH_path to  '" + schema + "'; " +
                "select count(*) from pg_indexes where UPPER(schemaname) = UPPER('{0}') and UPPER(tablename) = UPPER('{1}') and UPPER(indexname) = UPPER('{2}') ", schema,
                table,
                indexName);
            var createIndex = string.Format(" set SEARCH_path to  '" + schema + "';  " + "create index {0} on {1}({2})", indexName, tableName, indexColumns);
#else
            var createIndex = string.Format( " create index {0} on {1}({2})", indexName, tableName, indexColumns);
            string sql = "select count(*)  from  " + schema + ":" + "sysindexes where UPPER(idxname)=" + "UPPER('"+indexName+"')"+ ";";

#endif
            Returns ret;
            object result;

            if (dbTransaction == null)
            {
                result = ExecScalar(conn, sql, out ret, true);
            }
            else
            {
                result = ExecScalar(conn, dbTransaction, sql, out ret, true);
            }
            if (!ret.result)
            {


                MonitorLog.WriteLog("Ошибка  строка 1257 " + ret.text, MonitorLog.typelog.Error, true);
                return;
            }
            long count;
            if (long.TryParse(result.ToString(), out count) && count == 0)
            {

                // CreateIndex(conn, dbTransaction, createIndex);
                if (dbTransaction != null)
                {
#if PG
                    ExecSQL(conn, dbTransaction, createIndex, true);
#else
                    ExecSQL(conn, dbTransaction, " Database " + schema + "; ", true);
                    ExecSQL(conn, dbTransaction, createIndex, false);
#endif


                }
                else
                {
#if PG
                    ExecSQL(conn, createIndex, true);
#else
                    ExecSQL(conn, " Database " + schema + "; ", true);
                    ExecSQL(conn, createIndex, false);
#endif

                }
            }
        }

        public static bool SchemaExists(string schema, IDbConnection connection)
        {
#if PG
            var sql = string.Format("select count(*) from information_schema.schemata where schema_name = '{0}'", schema);


            Returns ret;
            var countStr = ExecScalar(connection, sql, out ret, true);

            int countTmp;

            return int.TryParse(countStr.ToString(), out countTmp) && countTmp != 0;
#else
            return true;
#endif
        }


        public static string GetFullBaseName(IDbConnection Connection)
        {
#if PG
            return "public";
#else            
            return Connection.Database + "@" + getServer(Connection);
#endif

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
#if PG
            if (dbName == Connection.Database) return String.Format("public{1}{2}", dbName, tableDelimiter, tableName);
            return String.Format("{0}{1}{2}", dbName, tableDelimiter, tableName);
#else
            return String.Format("{0}@{1}{2}{3}", dbName, getServer(Connection), tableDelimiter ,tableName);
#endif

        }

        /// <summary>
        ///  Возвращает содержимо запроса в таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="sql">SQL Запрос</param>
        /// <returns>Таблицу с заполненными данными по запросу</returns>
        public static DataTable ExecSQLToTable(IDbConnection connection, string sql)
        {
            return ExecSQLToTable(connection, sql, 300);
        }

        /// <summary>
        ///  Возвращает содержимо запроса в таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="sql">SQL Запрос</param>
        /// <param name="time">Время таймаута</param>
        /// <returns>Таблицу с заполненными данными по запросу</returns>
        public static DataTable ExecSQLToTable(IDbConnection connection, string sql, int time)
        {


            DataTable Data_Table = new DataTable();

#if PG
            sql = sql.PgNormalize(connection);
#endif

            IDbCommand cmd = null;
            IDataReader reader = null;
            string err = String.Empty;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection);
                if (time != 0) cmd.CommandTimeout = time;
                reader = cmd.ExecuteReader();
                Utils.setCulture();
                if (reader != null) Data_Table.Load(reader, LoadOption.OverwriteChanges);
            }
            catch (Exception ex)
            {
                err = " Ошибка чтения из базы данных  \n " +
                      " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                      " " + GetIfxError(ex);

                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                // write call stack method names
                foreach (StackFrame stackFrame in stackFrames)
                {
                    if (stackFrame.GetMethod().Name.Trim() == "Invoke") break;
                    err += stackFrame.GetMethod().Name + " \n"; // write method name
                }

                MonitorLog.WriteException(err);
                MonitorLog.WriteLog(" Выполнение запроса неудачно в DataTable " + err, MonitorLog.typelog.Info, 1, 1, true);

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
#if PG
 "set search_path to '" + databaseOrSchema + "'"
#else
                "database " + databaseOrSchema
#endif
, true);
        }


        public static string MDY(int month, int day, int year)
        {
#if PG
            return string.Format(" '{0}-{1}-{2}'::timestamp ", year, month, day);
#else
            return string.Format(" mdy({0},{1},{2}) ", month, day, year);
#endif
        }

        /// <summary>
        /// Создает таблицу в указанной схеме
        /// </summary>
        /// <param name="DbConnection">База данных, к которой будет открыто подключение.
        /// Параметр игнорируется для PostgreSQL</param>
        /// <param name="DbSchema">Схема или база данных, в которой необходимо создать таблицу</param>
        /// <param name="TableName">Имя таблицы</param>
        /// <param name="Columns">Описание колонок.
        /// Например, nzp SERIAL NOT NULL</param>
        public static Returns DbCreateTable(ConnectToDb DbConnection, CreateTableArgs DbArgs, string DbSchema, string TableName, params string[] Columns)
        {
#if PG
            bool IsPostgres = true;
#else
            bool IsPostgres = false;
#endif
            Returns ret = Utils.InitReturns();
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
                        lstQuerys.Add(string.Format("CREATE TABLE {0} ({1})",
                            TableName, string.Join(", ", Columns)));
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
                        lstQuerys.Add(string.Format("CREATE TABLE {0} ({1})",
                        TableName, string.Join(", ", Columns)));
                    }
                    break;
                case CreateTableArgs.CreateIfNotExists:
                    if (IsPostgres)
                        lstQuerys.Add(string.Format("CREATE TABLE IF NOT EXISTS {0} ({1})",
                             TableName, string.Join(", ", Columns)));
                    else
                    {
                        lstQuerys.Add(
                            "CREATE PROCEDURE createTable();\n" +
                            "ON EXCEPTION in (-310)\n" +
                            "END EXCEPTION WITH RESUME\n" +
                            "CREATE TABLE " + TableName +
                            " (" + string.Join(", ", Columns) + ");" +
                            "END PROCEDURE;\n");
                        lstQuerys.Add("EXECUTE PROCEDURE createTable();");
                        lstQuerys.Add("DROP PROCEDURE createTable;");
                    }
                    break;
                default:
                    lstQuerys.Add(string.Format("CREATE TABLE {0} ({1})",
                        TableName, string.Join(", ", Columns)));
                    break;
            }

            using (IDbConnection dbConn = newDbConnection(
                DbConnection == ConnectToDb.Host ?
                Constants.cons_Kernel :
                Constants.cons_Webdata)
                )
            {
                if (dbConn.State != ConnectionState.Open) dbConn.Open();
                lstQuerys.ForEach(query => { if (!(ret = ExecSQL(dbConn, query, true)).result) return; });
                dbConn.Close();
            }
            return ret;
        }

        public static Returns DbCreateTable(IDbConnection DbConn, CreateTableArgs DbArgs, bool IsTempTable, string DbSchema, string TableName, params string[] Columns)
        {
#if PG
            bool IsPostgres = true;
#else
            bool IsPostgres = false;
#endif
            Returns ret = Utils.InitReturns();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        static private string GetIfxError(Exception ex)
        {
            string _InformixException = "";
#if PG
#else
            if (ex is IfxException)
            {
                if ((ex as IfxException).Errors.Count > 0)
                {
                    IfxError ifxErr = (ex as IfxException).Errors[0];

                    _InformixException = Environment.NewLine + "-------------------------[Informix Error]-------------------------" + Environment.NewLine +
                    "Message :" + ifxErr.Message + Environment.NewLine +
                    "Native error :" + ifxErr.NativeError + Environment.NewLine +
                    "SQL state :" + ifxErr.SQLState +
                    Environment.NewLine;
                }
            }
#endif
            return _InformixException;
        }

        public static List<T> Query<T>(IDbConnection connection, string sql, out Returns ret) where T : new()
        {
            return Query<T>(connection, null, sql, out ret);
        }

        public static List<T> Query<T>(IDbConnection connection, string sql) where T : new()
        {
            Returns ret;
            return Query<T>(connection, null, sql, out ret);
        }

        public static List<T> Query<T>(IDbConnection connection, IDbTransaction transaction, string sql) where T : new()
        {
            Returns ret;
            return Query<T>(connection, transaction, sql, out ret);
        }

        public static List<T> Query<T>(IDbConnection connection, IDbTransaction transaction, string sql, out Returns ret, bool inlog = true, int timeout = 6000) where T : new()
        {
            ret = Utils.InitReturns();
#if PG
            sql = sql.PgNormalize(connection);
#endif
            var resList = new List<T>();
            IDataReader reader = null;
            IDbCommand cmd = null;
            T obj;
            try
            {
                cmd = newDbCommand(sql, connection, transaction);
                cmd.CommandTimeout = timeout;
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    obj = reader.ConvertedEntity<T>();
                    resList.Add(obj);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка чтения из базы данных ";
                ret.sql_error = " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message +
                                " " + GetIfxError(ex) + " \n " + connection + ": type = " + connection.GetType() +
                                ", datatbase = " + connection.Database + ", state = " + connection.State + " \n";
                var err = ret.text + " \n " + ret.sql_error;
                if (inlog)
                {
                    var stackTrace = new StackTrace(); // get call stack
                    var stackFrames = stackTrace.GetFrames(); // get method calls (frames)

                    // write call stack method names
                    err =
                        stackFrames.TakeWhile(stackFrame => stackFrame.GetMethod().Name.Trim() != "Invoke")
                            .Aggregate(err, (current, stackFrame) => current + (stackFrame.GetMethod().Name + " \n"));

                    //MonitorLog.WriteLog(err, MonitorLog.typelog.Error, 1, 3, true);
                    MonitorLog.WriteException(err);
                    //#if DEBUG
                    if (Points.FullLogging)
                    {
                        MonitorLog.WriteLog(" Выполнение запроса неудачно  " + sql + " " + ex, MonitorLog.typelog.Info,
                            1, 1, true);
                    }
                    //#endif
                }
                if (Constants.Viewerror) ret.text = err;

                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
            return resList;
        }

        /// <summary>
        /// Получить значение параметра в текущем расчетном месяце
        /// </summary>
        /// <typeparam name="T">тип параметра</typeparam>
        /// <param name="conn_db">соединение</param>
        /// <param name="pref">префикс</param>
        /// <param name="nzpPrm">номер параметра</param>
        /// <param name="prm_num">номер таблицы</param>
        /// <param name="ret">returns </param>
        /// <returns></returns>
        public static T GetParamValueInCurrentMonth<T>(IDbConnection conn_db, string pref, int nzpPrm, int prm_num, out Returns ret)
        {
            var prm = new CalcMonthParams { pref = pref };
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            var LastDayCalcMonth = new DateTime(rec.year_, rec.month_, DateTime.DaysInMonth(rec.year_, rec.month_));
            return GetParamValueInPeriod<T>(conn_db, pref, nzpPrm, prm_num, CalcMonth, LastDayCalcMonth, out ret);
        }

        /// <summary>
        /// Получить значение параметра в периоде (на пересечение)
        /// </summary>
        /// <typeparam name="T">тип параметра</typeparam>
        /// <param name="conn_db">соединение</param>
        /// <param name="pref">префикс</param>
        /// <param name="nzpPrm">номер параметра</param>
        /// <param name="prm_num">номер таблицы</param>
        /// <param name="date_to"></param>
        /// <param name="ret">returns </param>
        /// <param name="date_from"></param>
        /// <returns></returns>
        public static T GetParamValueInPeriod<T>(IDbConnection conn_db, string pref, int nzpPrm, int prm_num,
            DateTime date_from, DateTime date_to, out Returns ret)
        {
            var tableName = pref + sDataAliasRest + "prm_" + prm_num;
            var val = "max(val_prm)";
            if (typeof(T) == typeof(bool))
            {
                val = "max(case when val_prm='1' then 1 else 0 end)";
            }
            var sql = string.Format(" Select {0} From {1} p " + " Where p.nzp_prm =  {2} and p.is_actual <> 100 " +
                                  " and p.dat_s<={4} and p.dat_po>={3}",
                                  val, tableName, nzpPrm, Utils.EStrNull(date_from.ToShortDateString()),
                                  Utils.EStrNull(date_to.ToShortDateString()));
            var res = CastValue<T>(ExecScalar(conn_db, sql, out ret, true));
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка получения параметра GetParamValueInPeriod<T>: " +
                     ret.text, MonitorLog.typelog.Error, 1, 2, true);
            }
            return res;
        }
    }
}