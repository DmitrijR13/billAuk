
//--------------------------------------------------------------------------------80
//Файл: _DBUtils.cs
//Дата создания: 26.09.2012
//Дата изменения: 26.09.2012
//Назначение: Утилиты Informix
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2012. 
//--------------------------------------------------------------------------------80
using System;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.DataBase
{

    /// <summary>
    ///Класс полезных утилит работы с БД 
    /// </summary>
    public class ClassDBUtils
    {

        /// <summary>
        ///Метод получения длинного ключа по сериальному ключу таблицы
        ///Автор: Зыкин А.А.
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="serialID"></param>
        /// <param name="tableName"></param>
        /// <param name="keyField"></param>
        /// <returns></returns>
        static public decimal GetGlobalKey(IDbConnection connectionID, decimal serialID, string tableName, string keyField)
        {
            return ClassDBUtils.GetGlobalKey(connectionID, null, serialID, tableName, keyField);
        }
        static public decimal GetGlobalKey(IDbConnection connectionID, IDbTransaction transactionID, decimal serialID, string tableName, string keyField)
        {
            IntfResultTableType r = ClassDBUtils.OpenSQL(
                                      "select " + keyField +
                                      " from " + tableName +
                                      " where no=" + serialID.ToString()
                                      , connectionID, transactionID);
            return (decimal)r.resultData.Rows[0][0];
        }

        /// <summary>
        ///Метод получения сериального ключа таблицы
        ///Автор: Зыкин А.А. 
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="transactionID"></param>
        /// <returns></returns>
        static public decimal GetSerialKey(IDbConnection connectionID, IDbTransaction transactionID)
        {
            //todo проверить как взять id для pg
#if PG
            IDbCommand getSerial = DBManager.newDbCommand("select lastval()", connectionID, transactionID);
#else
            IDbCommand getSerial = DBManager.newDbCommand("select dbinfo('sqlca.sqlerrd1') from systables where tabid = 1", connectionID, transactionID);
#endif
            decimal new_id = Convert.ToDecimal(getSerial.ExecuteScalar());
            return new_id;
        }

        /// <summary>
        ///Метод получения сериального ключа таблицы
        ///Автор: Зыкин А.А. 
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="transactionID"></param>
        /// <returns></returns>
        static public Int32 GetAffectedRowsCount(IDbConnection connectionID)
        {
            return GetAffectedRowsCount(connectionID, null);
        }
        static public Int32 GetAffectedRowsCount(IDbConnection connectionID, IDbTransaction transactionID)
        {
            IDbCommand getSerial = DBManager.newDbCommand("select dbinfo('sqlca.sqlerrd2') from systables where tabid = 1", connectionID, transactionID);
            return Convert.ToInt32(getSerial.ExecuteScalar()); ;
        }

        /// <summary>
        ///Методы для создания обекта соединения с БД
        /// по умолчанию соединение с текущей БД и соединение закрыто
        ///Автор: Зыкин А.А.
        /// </summary>
        /// <returns></returns>
        //static public IfxConnection ConnectDB()
        //{
        //    return ConnectDB(true, ClassDB.dataBaseConnection);
        //}
        //static public IfxConnection ConnectDB(bool isOpen)
        //{
        //    return ConnectDB(isOpen, ClassDB.dataBaseConnection);
        //}

        //static public IfxConnection ConnectDB(bool isOpen, string connectionString)
        //{
        //    //ClassDB db = new ClassDB();
        //    ClassConnectionParams userConnectionParams = new ClassConnectionParams();
        //    //-----------------------------------------------------------
        //    // Формирование строки параметров соединения
        //    //-----------------------------------------------------------
        //    string cs = userConnectionParams.getConnectionString(connectionString);

        //    IfxConnection connectionID = new IfxConnection(cs);
        //    if (isOpen) connectionID.Open();
        //    return connectionID;

        //}

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        ///Методы для выполнения запроса выборки из БД
        ///Автор: Зыкин А.А.
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="connectionID"></param>
        /// <returns></returns>
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       IDbConnection connectionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, "table", null, connectionID, null, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       ExecMode execMode
                                        )
        {
            return ClassDBUtils.OpenSQL(sqlString, "table", null, connectionID, null, execMode);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, "table", null, connectionID, transactionID, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID,
                                       ExecMode execMode
                                    )
        {
            return ClassDBUtils.OpenSQL(sqlString, "table", null, connectionID, transactionID, execMode);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       string tableName,
                                       IDbConnection connectionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, tableName, null, connectionID, null, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       DataTable existTable,
                                       IDbConnection connectionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, "", existTable, connectionID, null, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       string tableName,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, tableName, null, connectionID, transactionID, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       DataTable existTable,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID)
        {
            return ClassDBUtils.OpenSQL(sqlString, "", existTable, connectionID, transactionID, ExecMode.Exception);
        }
        //----------------------------------------------------------------
        static public IntfResultTableType OpenSQL(
                                       string sqlString,
                                       string tableName,
                                       DataTable existTable,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID,
                                       ExecMode execMode,
                                       int timeOut = 180
                                    )
        {
            //Если на вход не подали таблицу - создать
            if (existTable == null) existTable = new DataTable(tableName);


            //выполнить запрос
            if (sqlString != "")
            {
                try
                {
                    IDbCommand myCommand = DBManager.newDbCommand(sqlString, connectionID, transactionID);
                    myCommand.CommandTimeout = timeOut;
                    using (IDataReader reader = myCommand.ExecuteReader(CommandBehavior.SingleResult))
                    {
                        //режим отладки
                        if (execMode == ExecMode.LogTrace) Utility.ClassLog.WriteLog(sqlString.Replace(";", "").Trim() + ";");
                        Utils.setCulture();
                        existTable.Load(reader, LoadOption.OverwriteChanges);
                        reader.Close();
                        return (new IntfResultTableType(existTable));
                    }
                }
                catch (Exception ex)
                {
                    //режим отладки
                    if (execMode == ExecMode.LogTrace) Utility.ClassLog.WriteLog(sqlString.Replace(";", "").Trim() + "; -- Ошибка!!! " + ex.Message);

                    throw
                        new Exception("Ошибка выполнения запроса к БД: " + ex.Message +
                                      Environment.NewLine +
                                     "Текст запроса: [" + sqlString + "]");
                }
            }
            else
                throw new Exception("Не задан текст запроса");
        }
        /// <summary>
        ////Методы для выполнения запроса изменения БД
        ///Автор: Зыкин А.А.
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="connectionID"></param>
        /// <returns></returns>
        /// 
        public enum ExecMode
        {
            Exception = 1,
            Log = 2,
            LogTrace = 3
        }

        static public IntfResultType ExecSQL(
                                       string sqlString,
                                       IDbConnection connectionID)
        {
            return ClassDBUtils.ExecSQL(sqlString, connectionID, null, false, ExecMode.Exception);
        }
        static public IntfResultType ExecSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID)
        {
            return ClassDBUtils.ExecSQL(sqlString, connectionID, transactionID, false, ExecMode.Exception);
        }
        static public IntfResultType ExecSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       Boolean returningID
                                        )
        {
            return ClassDBUtils.ExecSQL(sqlString, connectionID, null, returningID, ExecMode.Exception);
        }

        static public IntfResultType ExecSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       ExecMode execMode
                                        )
        {
            return ClassDBUtils.ExecSQL(sqlString, connectionID, null, false, execMode);
        }

        public static IntfResultType ExecSQL(
            string sqlString,
            IDbConnection connectionID,
            IDbTransaction transaction,
            ExecMode execMode
            )
        {
            return ClassDBUtils.ExecSQL(sqlString, connectionID, transaction, false, execMode);
        }

        //----------------------------------------------------------------
        static public IntfResultType ExecSQL(
                                       string sqlString,
                                       IDbConnection connectionID,
                                       IDbTransaction transactionID,
                                       Boolean returningID,
                                       ExecMode execMode
                                    )
        {


            //выполнить запрос
            if (sqlString != "")
            {
                IDbCommand myCommand = DBManager.newDbCommand(sqlString, connectionID, transactionID);
                try
                {
                    //выполнить запрос

                    myCommand.CommandTimeout = 180;
                    int count = myCommand.ExecuteNonQuery();

                    //режим отладки
                    if (execMode == ExecMode.LogTrace)
                        Utility.ClassLog.WriteLog(sqlString.Replace(";", "").Trim() + ";");

                    decimal new_id = 0;
                    if (returningID) new_id = ClassDBUtils.GetSerialKey(connectionID, transactionID);

                    return (new IntfResultType(0, "", new_id) { resultAffectedRows = count });
                }
                catch (Exception ex)
                {
                    string msg = "Ошибка выполнения запроса к БД: " + ex.Message +
                                 Environment.NewLine +
                                 "Текст запроса: [" + sqlString + "]";

                    //режим без исключения
                    if (execMode == ExecMode.Log)
                    {
                        return (new IntfResultType(-1, msg));
                    }

                    //режим без исключения с отладкой в файл
                    if (execMode == ExecMode.LogTrace)
                    {
                        Utility.ClassLog.WriteLog(sqlString.Replace(";", "").Trim() + "; -- Ошибка!!! " + ex.Message);
                        return (new IntfResultType(-1, msg));
                    }
                    //если не вышли - бросить исключение
                    throw new Exception(msg);
                }
                finally
                {
                    myCommand.Dispose();
                }
            }
            else
                throw new Exception("Не задан текст запроса");
        }

        static public IntfResultType ExecCommand(
                                       IDbCommand command
                                    )
        {
            return ExecCommand(command, false);
        }

        static public IntfResultType ExecCommand(
                                       IDbCommand command,
                                       Boolean returningID
                                    )
        {
            if (command == null) throw new Exception("Объект на инициализирован");
            if (command.CommandText.Trim() == "") throw new Exception("Не задан текст запроса");

            try
            {
                command.CommandTimeout = 180;
                int count = command.ExecuteNonQuery();

                decimal new_id = 0;
                if (returningID) new_id = ClassDBUtils.GetSerialKey(command.Connection, command.Transaction);

                return (new IntfResultType(0, "", new_id) { resultAffectedRows = count });
            }
            catch (Exception ex)
            {
                string msg = "Ошибка выполнения запроса к БД: " + ex.Message +
                    Environment.NewLine +
                    "Текст запроса: [" + command.CommandText + "]" +
                    Environment.NewLine +
                    "Параметры запроса:";

                if (command.Parameters != null)
                {
                    foreach (IDbDataParameter param in command.Parameters)
                    {
                        msg += Environment.NewLine +
                            param.ParameterName +
                            ", тип: " + param.DbType.ToString() +
                            ", значение: " + (param.Value == DBNull.Value ? "не задано" : param.Value.ToString());
                    }
                }

                throw new Exception(msg);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ifxCommand"></param>
        /// <param name="parameterName"></param>
        /// <param name="ifxType"></param>
        /// <param name="value"></param>
        /// <param name="nullIfEmpty"></param>
        /// <returns></returns>
        static public IDbDataParameter AddIfxParam(IDbCommand ifxCommand, string parameterName, DbType ifxType, Object value, bool nullIfEmpty)
        {
            if (value != null)
            {
                if (value.GetType() == typeof(string))
                {
                    value = (value ?? "").ToString().Trim();
                    if (nullIfEmpty) { value = (Convert.ToString(value) != "") ? value : null; }
                }
                else if ((value.GetType() == typeof(decimal)) || (value.GetType() == typeof(decimal?)))
                {
                    if (nullIfEmpty) { value = (Convert.ToDecimal(value ?? -1) > 0) ? value : null; }
                }
                else if ((value.GetType() == typeof(DateTime)) || (value.GetType() == typeof(DateTime?)))
                {
                    if (nullIfEmpty) { value = (Convert.ToDateTime(value ?? DateTime.MinValue) > DateTime.MinValue) ? value : null; }
                }

                else if ((value.GetType() == typeof(Int32)) || (value.GetType() == typeof(Int32?)) ||
                    (value.GetType() == typeof(Int16)) || (value.GetType() == typeof(Int16?)) ||
                    (value.GetType() == typeof(Int64)) || (value.GetType() == typeof(Int64?)))
                {
                    if (nullIfEmpty) { value = (Convert.ToInt64(value ?? -1) > 0) ? value : null; }
                }
            }

            if (value == null) { value = DBNull.Value; }

            IDbDataParameter ifxParameter = DBManager.addDbCommandParameter(ifxCommand, parameterName, ifxType, value);

            return ifxParameter;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ifxCommand"></param>
        /// <param name="parameterName"></param>
        /// <param name="ifxType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static public IDbDataParameter AddIfxParam(IDbCommand ifxCommand, string parameterName, DbType ifxType, Object value)
        {
            return AddIfxParam(ifxCommand, parameterName, ifxType, value, false);
        }

        /// <summary>
        ///Метод преобразования строки к понятному для СУБД типа даты
        ///Автор: 
        /// </summary>
        /// 
        static public string ConvertToDate(string inputStr)
        {
#if PG
            if (inputStr == "")
            {
                inputStr = "null";
            }
            else inputStr = "'" + inputStr + "'";
#else
            if (inputStr == "")
            {
                inputStr = "''";
            }
#endif
            return inputStr;
        }


    }

    public class IfxCommandQueryParam
    {
        public IfxCommandQueryParam(IDbCommand ifxCommand)
        {
            this.ifxCommand = ifxCommand;
        }
        private readonly IDbCommand ifxCommand = null;

        public void AddParam(string prmName, string value)
        {
            DBManager.addDbCommandParameter(ifxCommand, prmName, DbType.String, value ?? Convert.DBNull);
        }
        public void AddParam(string prmName, decimal? value)
        {
            DBManager.addDbCommandParameter(ifxCommand, prmName, DbType.Decimal, value ?? Convert.DBNull);
        }
        public void AddParam(string prmName, DateTime? value)
        {
            DBManager.addDbCommandParameter(ifxCommand, prmName, DbType.DateTime, value ?? Convert.DBNull);
        }
    }


}
