using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Load.Obninsk
{
    public class ConnectionToDB
    {
        public IDbConnection Connection { get; set; }

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected virtual IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                //Connection = DBManager.GetConnection(ReportParams.ConnectionString);
                Connection = DBManager.GetConnection(Constants.cons_Kernel);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new ReportException(result.text);
                }
            }

            return Connection;
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
                throw new ReportException("Не удалось закрыть соединение", exc);
            }
        }



        /// <summary>Палучить ключ вставленной записи</summary>
        protected virtual int GetSerialValue()
        {
            return DBManager.GetSerialValue(Connection);
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

        protected virtual IntfResultTableType ExecSQLDbUtils(string sql)
        {
           IntfResultTableType res= ClassDBUtils.OpenSQL(sql, Connection);
            if (!res.GetReturnsType().result)
            {
                throw new ReportException(res.GetReturnsType().text);
            }
            return res;
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
                throw new ReportException(result.text);
            }
        }

        /// <summary>Проверка на существование таблицы</summary>
        /// <param name="tableName">Имя таблицы</param>
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
        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        protected virtual void ExecRead(out IDataReader reader, string sql)
        {
            ExecRead(out reader, sql, true, 300);
        }


        /// <summary>Получить результат sql запроса в виде значения</summary>
        /// <param name="sql">Sql запрос</param>
        protected virtual object ExecScalar(string sql)
        {
            Returns ret;
            return DBManager.ExecScalar(Connection, sql, out ret, true);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog)
        {
            ExecRead(out reader, sql, inlog, 300);
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет</param>
        /// <param name="timeout">Таймаут</param>
        protected virtual void ExecRead(out IDataReader reader, string sql, bool inlog, int timeout)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog, timeout);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="ret">Результат разпроса (выполнен ли)</param>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual object ExecScalar(string sql, out Returns ret, bool inlog)
        {

            object obj = DBManager.ExecScalar(Connection, sql, out ret, inlog);
            if (!ret.result)
            {
                throw new ReportException(ret.text);
            }
            return obj;
        }

        /// <summary>Получить результат sql запроса в виде IDataReader</summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="sql">Sql запрос</param>
        /// <param name="inlog">Логировать да/нет (по умолчанию да)</param>
        protected virtual void ExecRead(out MyDataReader reader, string sql, bool inlog = true)
        {
            var result = DBManager.ExecRead(Connection, out reader, sql, inlog);
            if (!result.result)
            {
                throw new ReportException(result.text);
            }
        }
    }
}
