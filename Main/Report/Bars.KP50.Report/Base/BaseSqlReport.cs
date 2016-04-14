using System.Diagnostics;
using System.Linq;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;

    using Bars.KP50.Report.Base;
    using Bars.QueueCore;

    using Castle.Windsor;

    using Npgsql;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Interfaces;
    using IBM.Data.Informix;

    /// <summary>Базовый отчет, ориентированный на получение данных через sql запросы</summary>
    public abstract class BaseSqlReport : BaseFastReport
    {
        protected JobArguments JobArguments { get; set; }

        protected string ReportSavePathResult { get; set; }

        protected virtual IDbConnection Connection { get; set; }

        /// <summary>Выполнить</summary>
        /// <param name="container">IoC контейнер</param>
        /// <param name="jobArguments">Параметры выполнения</param>
        public override void Run(IWindsorContainer container, JobArguments jobArguments)
        {
            Container = container;
            
            ReportParams = ReportParams ?? new ReportParams(Container);

            JobArguments = jobArguments;
            SetProcessState();
            bool success;
            string message;
            try
            {
                GenerateReport(jobArguments.Parameters);
                success = true;
                message = ReportSavePathResult;
            }
            catch (ReportException exc)
            {
                MonitorLog.WriteException("Ошибка при формировании отчета! ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при формировании отчета ", exc);
                }
                success = false;
                message = exc.Message;
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка при формировании отчета! ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при формировании отчета ", exc);
                }
                success = false;
                message = exc.Message;
            }
            SetEndState(success, message);
        }

        /// <summary>Получить состояние отчета</summary>
        /// <returns>Состояние отчета</returns>
        public override JobState GetState()
        {
            return JobState;
        }

        public override void GenerateReport(NameValueCollection reportParameters)
        {
            try
            {
                SetReportParameters(reportParameters);
                OpenConnection();
                PrepareParams();
                CreateTempTable();
                var makeDataWatch = new Stopwatch();
                makeDataWatch.Start();
                var ds = GetData();
                makeDataWatch.Stop();

                var generateBillWatch = new Stopwatch();
                generateBillWatch.Start();
                Generate(ds);
                generateBillWatch.Stop();

                var makeData = makeDataWatch.Elapsed;
                var generateBill = generateBillWatch.Elapsed;
                var elapsedTime = String.Format(
                    "Отчет " + Name + ", " +
                    (ds.Tables.Count > 0
                        ? string.Join(",", (from DataTable table in ds.Tables select table.TableName + ": " + table.Rows.Count + " строк ").ToList())
                        : string.Empty) +
                    ", время работы - заполнение данных: {0:00}:{1:00}:{2:00}.{3:00} генерация отчета: {4:00}:{5:00}:{6:00}.{7:00}",
                    makeData.Hours, makeData.Minutes, makeData.Seconds, makeData.Milliseconds/10,
                    generateBill.Hours, generateBill.Minutes, generateBill.Seconds, generateBill.Milliseconds/10);
                MonitorLog.WriteLog(elapsedTime, MonitorLog.typelog.Info, false);

            }
            catch (ReportException ex)
            {
                MonitorLog.WriteException("Произошла ошибка при формировании отчета ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при формировании отчета ", ex);
                }
                throw;
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Произошла ошибка при формировании отчета ", exc);
                if (exc.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при формировании отчета ", exc);
                }
                throw new ReportException("Произошла ошибка при формировании отчета " + exc.Message, exc);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }







        protected void SetProcessState()
        {
            try
            {
                JobState = JobState.Proccess;
                var connection = OpenConnection();
                var command = connection.CreateCommand();
#if PG
                command.CommandText =
                    "update public.jobs set job_state = :job_state, start_date = :start_date where id = :id";
                command.Parameters.Add(new NpgsqlParameter("job_state", (int) JobState));
                command.Parameters.Add(new NpgsqlParameter("start_date", DateTime.Now));
                command.Parameters.Add(new NpgsqlParameter("id", JobArguments.JobId));
#else
                command.CommandText = "update jobs set job_state = ?, start_date = ? where id = ?";
                command.Parameters.Add(new IfxParameter("job_state", (int)JobState));
                command.Parameters.Add(new IfxParameter("start_date", DateTime.Now));
                command.Parameters.Add(new IfxParameter("id", JobArguments.JobId));
#endif

                command.ExecuteNonQuery();
                CloseConnection();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при записи информации об отчете в таблицу jobs! ", ex);
            }
        }

        protected void SetEndState(bool success, string message)
        {
            JobState = JobState.End;
            var connection = OpenConnection();
            var command = connection.CreateCommand();
#if PG
            command.CommandText = "update public.jobs set job_state = :job_state, end_date = :end_date, success = :success, message = :message where id = :id";
            command.Parameters.Add(new NpgsqlParameter("job_state", (int)JobState));
            command.Parameters.Add(new NpgsqlParameter("end_date", DateTime.Now));
            command.Parameters.Add(new NpgsqlParameter("success", success));
            command.Parameters.Add(new NpgsqlParameter("message", message));
            command.Parameters.Add(new NpgsqlParameter("id", JobArguments.JobId));
#else
            command.CommandText = "update jobs set job_state = ?, end_date = ?, success = ?, message = ? where id = ?";
            command.Parameters.Add(new IfxParameter("job_state", (int)JobState));
            command.Parameters.Add(new IfxParameter("end_date", DateTime.Now));
            command.Parameters.Add(new IfxParameter("success", success));
            command.Parameters.Add(new IfxParameter("message", message));
            command.Parameters.Add(new IfxParameter("id", JobArguments.JobId));
#endif

            command.ExecuteNonQuery();
            CloseConnection();
        }

        /// <summary>Создать временные таблицы</summary>
        protected abstract void CreateTempTable();

        /// <summary>Удалить временные таблицы</summary>
        protected abstract void DropTempTable();

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected virtual IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(ReportParams.ConnectionString);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new ReportException(result.text);
                }
            }

            return Connection;
        }

        /// <summary>Подготовить параметры отчета</summary>
        protected abstract void PrepareParams();

        /// <summary>Закрыть соединение с БД</summary>
        protected virtual void CloseConnection()
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

        /// <summary>Закрыть IDataReader</summary>
        /// <param name="reader">Экземпляр IDataReader</param>
        protected virtual void CloseReader(ref IDataReader reader)
        {
            if (reader != null)
            {
                if (!reader.IsClosed)
                {
                    reader.Close();
                }

                reader.Dispose();
                reader = null;
            }
        }

        /// <summary>Получить пользователя</summary>
        /// <returns>Пользователь</returns>
        protected virtual User GetUser()
        {
            return null;
        }

        /// <summary>Получить список ролей пользователя</summary>
        /// <returns>Список ролей</returns>
        protected virtual List<_RolesVal> GetUserRoles()
        {
            return null;
        }

        /// <summary>Открыть соединение с БД</summary>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void OpenDb(bool inlog = true)
        {
            var result = DBManager.OpenDb(Connection, inlog);
            if (!result.result)
            {
                throw new ReportException(result.text);
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
                throw new ReportException(result.text);
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
            return ExecSQLToTable(sql, 0);
        }

        /// <summary>Получить результат sql запроса в виде таблицы</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="time">Время таймаута</param>
        /// <returns>Таблица</returns>
        protected virtual DataTable ExecSQLToTable(string sql, int time)
        {
            return DBManager.ExecSQLToTable(Connection, sql, time);
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