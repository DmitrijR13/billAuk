namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using STCLINE.KP50.Interfaces;

    using Npgsql;

    using STCLINE.KP50.Global;

    public interface IDataBaseCommon
    {
        void Close();
    }


    public class DataBaseHead : IDisposable, IDataBaseCommon
    {
#if PG
        protected readonly string pgDefaultSchema = "public";
#else
#endif

        protected static string BasePwd = DBManager.BasePwd;

#if PG
#else
        /// <summary>
        /// Время ожидания снятия блокировки с таблицы
        /// </summary>
        public static int WaitingTimeout = DBManager.WaitingTimeout;
#endif

        public const string ConfPref = DBManager.ConfPref;

        public bool LongType = DBManager.LongType;

        private List<IDataReader> _readers;

        private Dictionary<string, IDbConnection> _connections = new Dictionary<string, IDbConnection>();

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
        protected static string sSupgAliasRest = DBManager.sSupgAliasRest;
        protected static string tbluser = DBManager.tbluser;
        protected static string sDecimalType = DBManager.sDecimalType;
        protected static string sCharType = DBManager.sCharType;
        protected static string sUniqueWord = DBManager.sUniqueWord;
        protected static string sNvlWord = DBManager.sNvlWord;
        protected static string sConvToNum = DBManager.sConvToNum;
        protected static string sConvToInt = DBManager.sConvToInt;
        protected static string sConvToChar = DBManager.sConvToChar;
        protected static string sConvToChar10 = DBManager.sConvToChar10;
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

        public DataBaseHead()
        {
            _connections = new Dictionary<string, IDbConnection>();
            _readers = new List<IDataReader>();
        }

        ~DataBaseHead()
        {
            Close();
        }

        public virtual void Dispose()
        {
            Close();
        }

        protected IDbConnection GetConnection(string connectionString)
        {
            if (Constants.UseExtendedConnectionfactory)
                return DBManager.GetConnection(connectionString);
            IDbConnection connection;
            if (_connections.TryGetValue(connectionString, out connection)) return connection;
            else
            {
                connection = new MyDbConnection(DBManager.GetConnection(connectionString));
                _connections.Add(connectionString, connection);
            }
            return connection;
        }

        protected IDbConnection GetConnection()
        {
            return GetConnection(Constants.cons_Kernel);
        }

        public void Close()
        {
            if (!Constants.UseExtendedConnectionfactory)
            {
                if (_readers != null)
                    for (int i = _readers.Count - 1; i >= 0; i--)
                        if (_readers[i] != null)
                        {
                            _readers[i].Close();
                            _readers[i].Dispose();
                            _readers.RemoveAt(i);
                        }
                if (_readers != null) _readers.Clear();
                _readers = null;

                if (_connections != null)
                    foreach (KeyValuePair<string, IDbConnection> keyPair in _connections)
                        if (keyPair.Value != null)
                        {
                            try
                            {
                                ((MyDbConnection)keyPair.Value).RealClose();
                                //keyPair.Value.Dispose();
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteLog("Ошибка при закрытии соединения с базой данных\n" + ex.Message,
                                    MonitorLog.typelog.Error, true);
                            }
                        }
                if (_connections != null) _connections.Clear();
                _connections = null;
            }
        }

        protected void CloseReader(ref IDataReader reader)
        {
            if (reader != null)
            {
                if (!reader.IsClosed) reader.Close();
                reader.Dispose();
                _readers.Remove(reader);
                reader = null;
            }
        }

        protected Returns OpenDb(IDbConnection connectionID, bool inlog)
        //----------------------------------------------------------------------
        {
            return DBManager.OpenDb(connectionID, inlog);
        }

        protected Returns ExecSQL(IDbConnection connectionID, string sql) //
        {
            return ExecSQL(connectionID, null, sql, true);
        }

        /// <summary>исполнение sql-запроса</summary>
        protected Returns ExecSQL(IDbConnection connectionID, string sql, bool inlog) //
        {
            return ExecSQL(connectionID, null, sql, inlog, 6000); //300);
        }

        /// <summary>
        /// ExecSQL c вызовом исключения, если результат запроса = false
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="transaction"></param>
        /// <param name="sql"></param>
        /// <param name="inlog"></param>
        /// <returns>Returns</returns>
        protected Returns ExecSQLWE(IDbConnection connectionID, IDbTransaction transaction, string sql, bool inlog) //
        {
            Returns ret = ExecSQL(connectionID, transaction, sql, inlog);
            if (!ret.result) throw new Exception(ret.text);
            return ret;
        }

        protected Returns ExecSQLWE(IDbConnection connectionID, IDbTransaction transaction, string sql) //
        {
            return ExecSQLWE(connectionID, transaction, sql, true);
        }

        protected Returns ExecSQLWE(IDbConnection connectionID, string sql)
        {
            return ExecSQLWE(connectionID, null, sql, true);
        }

        protected Returns ExecSQLWE(IDbConnection connectionID, string sql, bool inlog)
        {
            return ExecSQLWE(connectionID, null, sql, inlog);
        }

        protected Returns ExecSQL(IDbConnection connectionID, string sql, bool inlog, int time) //
        {
            return ExecSQL(connectionID, null, sql, inlog, time);
        }

        protected Returns ExecSQL(IDbConnection connectionID, IDbTransaction transaction, string sql, bool inlog) //
        {
            return ExecSQL(connectionID, transaction, sql, inlog, 300);
        }

        protected Returns ExecSQL(
            IDbConnection connectionID,
            IDbTransaction transaction,
            string sql,
            bool inlog,
            int time) //
        {
            return DBManager.ExecSQL(connectionID, transaction, sql, inlog, time);
        }

        protected Returns ExecRead(IDbConnection connectionID, out IDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connectionID, null, out reader, sql, inlog, 300);
        }

        protected Returns ExecRead(
            IDbConnection connectionID,
            out IDataReader reader,
            string sql,
            bool inlog,
            int timeout)
        {
            return ExecRead(connectionID, null, out reader, sql, inlog, timeout);
        }

        protected Returns ExecRead(
            IDbConnection connectionID,
            IDbTransaction transaction,
            out IDataReader reader,
            string sql,
            bool inlog) //
        {
            return ExecRead(connectionID, transaction, out reader, sql, inlog, 300);
        }

        protected Returns ExecRead(
            IDbConnection connectionID,
            IDbTransaction transaction,
            out IDataReader reader,
            string sql,
            bool inlog,
            int timeout)
        {
            return DBManager.ExecRead(connectionID, transaction, out reader, sql, inlog, timeout);
        }

        protected Returns ExecRead(IDbConnection connectionID, out MyDataReader reader, string sql, bool inlog)
        {
            return ExecRead(connectionID, null, out reader, sql, inlog, 300);
        }

        protected Returns ExecRead(
            IDbConnection connectionID,
            IDbTransaction transaction,
            out MyDataReader reader,
            string sql,
            bool inlog)
        {
            return ExecRead(connectionID, transaction, out reader, sql, inlog, 300);
        }

        protected Returns ExecRead(
            IDbConnection connectionID,
            IDbTransaction transaction,
            out MyDataReader reader,
            string sql,
            bool inlog,
            int timeout)
        {
            return DBManager.ExecRead(connectionID, transaction, out reader, sql, inlog, timeout);
        }

        protected object ExecScalar(IDbConnection connectionID, string sql, out Returns ret, bool inlog)
        {
            return ExecScalar(connectionID, null, sql, out ret, inlog);
        }

        /// <summary>Получить результат sql запроса в виде типизированного значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual T ExecScalar<T>(IDbConnection connectionID, string sql, out Returns ret, bool inlog) where T : struct
        {
            return ExecScalar<T>(connectionID, sql, out ret, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде типизированного значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual T ExecScalar<T>(IDbConnection connection, string sql, out Returns ret, bool inlog, int timeout) where T : struct
        {
            var obj = DBManager.ExecScalar(connection, null, sql, out ret, inlog, timeout);
            return obj == null || obj == DBNull.Value ?
                default(T) : (T)Convert.ChangeType(obj, typeof(T));
        }


        protected object ExecScalar(
            IDbConnection connectionID,
            IDbTransaction transaction,
            string sql,
            out Returns ret,
            bool inlog)
        {
            return DBManager.ExecScalar(connectionID, transaction, sql, out ret, inlog);
        }

        protected object ExecScalar(
            IDbConnection connectionID,
            IDbTransaction transaction,
            string sql,
            out Returns ret,
            bool inlog,
            int timeout)
        {
            return DBManager.ExecScalar(connectionID, transaction, sql, out ret, inlog, timeout);
        }

        protected int GetSerialValue(IDbConnection conn)
        {
            return GetSerialValue(conn, 1, null);
        }

        protected int GetSerialValue(IDbConnection conn, IDbTransaction transaction) //
        {
            return GetSerialValue(conn, 1, transaction);
        }

        protected int GetSerialValue2(IDbConnection conn)
        {
            return GetSerialValue(conn, 2, null);
        }

        protected int GetSerialValue2(IDbConnection conn, IDbTransaction transaction)
        {
            return GetSerialValue(conn, 2, transaction);
        }

        protected int GetSerialValue(IDbConnection conn, int tip, IDbTransaction transaction)
        {
            return DBManager.GetSerialValue(conn, tip, transaction);
        }

        protected object GetNextSerial(string tableSchema, string tableName, string serialField, IDbConnection conn, IDbTransaction transaction)
        {
            Returns ret = new Returns(true);
            
            tableSchema = tableSchema.Trim().ToLower();
            tableName = tableName.Trim().ToLower();
            serialField = serialField.Trim().ToLower();
                
            string seq = tableSchema + DBManager.tableDelimiter + tableName + "_" + serialField + "_seq";
            seq = seq.Replace("..", ".");

            
#if PG
            string sql = " SELECT nextval('" + seq + "') ";
#else
            string sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
            object num = ExecScalar(conn, sql, out ret, true);
            if (!ret.result) throw new Exception(ret.text);
            
            return num;
        }

        public int GetNextSerialInt(string tableSchema, string tableName, string serialField, IDbConnection conn, IDbTransaction transaction)
        {
            return Convert.ToInt32(GetNextSerial(tableSchema, tableName, serialField, conn, transaction));
        }

        public decimal GetNextSerialDecimal(string tableSchema, string tableName, string serialField, IDbConnection conn, IDbTransaction transaction)
        {
            return Convert.ToDecimal(GetNextSerial(tableSchema, tableName, serialField, conn, transaction));
        }

        protected int LogSQL(IDbConnection conn_web, int nzp_user, string sql)
        {
            return DBManager.LogSQL(conn_web, nzp_user, sql);
        }

        protected bool LogSQL_Error(IDbConnection conn_web, int key, string err)
        {
            return DBManager.LogSQL_Error(conn_web, key, err);
        }

        protected bool DatabaseOnServer(IDbConnection conn_db, string db)
        {
            return DBManager.DatabaseOnServer(conn_db, db);
        }

        protected bool DatabaseOnServer(IDbConnection conn_db, string db, out Returns ret)
        {
            return DBManager.DatabaseOnServer(conn_db, db, out ret);
        }

        protected bool ProcedureInWebCashe(IDbConnection conn_web, string proc)
        {
            return DBManager.ProcedureInWebCashe(conn_web, proc);
        }

        protected bool ProcedureInWebCashe(IDbConnection conn_web, string proc, string db)
        {
            return DBManager.ProcedureInWebCashe(conn_web, proc, db);
        }

        protected int ProcedureInWebCasheID(IDbConnection conn_web, string proc, string db)
        {
            return DBManager.ProcedureInWebCasheID(conn_web, proc, db);
        }

        protected bool TableInWebCashe(IDbConnection conn_web, string tab)
        {
            return DBManager.TableInWebCashe(conn_web, tab);
        }

        protected uint TableInWebCasheID(IDbConnection conn_web, string tab)
        {
            return DBManager.TableInWebCasheID(conn_web, tab);
        }

        protected bool TableInBase(IDbConnection conn_web, IDbTransaction transaction, string base_name, string tab)
        {
            return DBManager.TableInBase(conn_web, transaction, base_name, tab);
        }

        protected int TableInBaseID(IDbConnection conn_web, IDbTransaction transaction, string base_name, string tab)
        {
            return DBManager.TableInBaseID(conn_web, transaction, base_name, tab);
        }

        /// <summary>
        /// Проверка наличия доступа к таблице
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tab"></param>
        /// <returns></returns>
        protected bool TempTableInWebCashe(IDbConnection connection, string tab)
        {
            return DBManager.TempTableInWebCashe(connection, tab);
        }

        protected bool TempTableInWebCashe(IDbConnection connection, IDbTransaction transaction, string tab)
        {
            return DBManager.TempTableInWebCashe(connection, transaction, tab);
        }

        /// <summary>
        /// Проверяет наличие колонки в таблице
        /// </summary>
        /// <param name="conn_id">ID соединения</param>
        /// <param name="tab">Наименование таблицы</param>
        /// <param name="column">Наименование колонки</param>
        /// <param name="cur_pg_dn">Наименование схемы  - нужно для PG</param>
        protected bool isTableHasColumn(IDbConnection conn_id, string tab, string column, string cur_pg_dn)
        {
            return DBManager.isTableHasColumn(conn_id, tab, column, cur_pg_dn);
        }

        protected bool isTableHasColumn(IDbConnection conn_id, string tab, string column)
        {
            return DBManager.isTableHasColumn(conn_id, tab, column, "");
        }

        protected bool isTableHasColumn(IDbConnection conn_id, IDbTransaction transaction, string tab, string column, string cur_pg_dn)
        {
            return DBManager.isTableHasColumn(conn_id, transaction, tab, column, cur_pg_dn);
        }

        protected bool isHasIndex(IDbConnection conn_id, string index)
        {
            return DBManager.isHasIndex(conn_id, index);
        }

        protected bool isHasIndex(IDbConnection conn_id, IDbTransaction transaction, string index)
        {
            return DBManager.isHasIndex(conn_id, transaction, index);
        }

        protected List<string> GetTables(IDbConnection conn_db, string db, string table) //
        {
            return DBManager.GetTables(conn_db, db, table);
        }

        protected void ExecByStep(
            IDbConnection conn,
            string tab,
            string pole,
            string sql,
            int step,
            string groupby,
            out Returns ret)
        {
            DBManager.ExecByStep(conn, tab, pole, sql, step, groupby, out ret);
        }

        /// <summary> Добавляет к таблице поле
        /// </summary>
        /// <param name="conn_db">Идентификатор соединения</param>
        /// <param name="table">Имя таблицы</param>
        /// <param name="field">Имя колонки</param>
        /// <param name="fieldType">Тип колонки</param>
        /// <returns></returns>
        protected Returns AddFieldToTable(IDbConnection conn_db, string table, string field, string fieldType)
        {
            return DBManager.AddFieldToTable(conn_db, table, field, fieldType);
        }

        /// <summary> Удаляет поле из таблицы
        /// </summary>
        /// <param name="connection">Идентификатор соединения</param>
        /// <param name="table">Имя таблицы</param>
        /// <param name="field">Имя колонки</param>
        /// <returns></returns>
        public Returns DropFieldsFromTable(IDbConnection connection, string table, string[] fields)
        {
            return DBManager.DropFieldFromTable(connection, table, fields);
        }

#if PG
        protected void CreateIndexIfNotExists(
            IDbConnection conn,
            string indexName,
            string tableName,
            string indexColumns,
            bool inLog = false,
            IDbTransaction dbTransaction = null)
        {
            DBManager.CreateIndexIfNotExists(conn, indexName, tableName, indexColumns, inLog, dbTransaction);
        }

        protected bool SchemaExists(string schema, IDbConnection connection)
        {
            return DBManager.SchemaExists(schema, connection);
        }
#endif
        public static string MDY(int month, int day, int year)
        {
            return DBManager.MDY(month, day, year);
        }

        public static Returns SelectDatabaseOrSchema(IDbConnection connection, string databaseOrSchema)
        {
            return DBManager.SelectDatabaseOrSchema(connection, databaseOrSchema);
        }

        public static string GetTableFullName(IDbConnection connection, string dbName, string tableName)
        {
            return DBManager.GetFullBaseName(connection, dbName, tableName);
        }

        public static T CastValue<T>(object value)
        {
            return DBManager.CastValue<T>(value);
        }

        public string GetRolesCondition(Finder finder, long typeCondition)
        {
            if (finder.RolesVal == null) return "";

            foreach (_RolesVal role in finder.RolesVal)
            {
                if (role.tip == Constants.role_sql)
                    if (role.kod == typeCondition) return role.val;
            }
            return "";
        }

        public static List<T> Query<T>(IDbConnection connection, string sql, out Returns ret) where T : new()
        {
            return DBManager.Query<T>(connection, sql, out ret);
        }

        public static List<T> Query<T>(IDbConnection connection, IDbTransaction transaction, string sql, out Returns ret) where T : new()
        {
            return DBManager.Query<T>(connection, transaction, sql, out ret);
        }

        public static List<T> Query<T>(IDbConnection connection, string sql) where T : new()
        {
            return DBManager.Query<T>(connection, null, sql);
        }
    }
}