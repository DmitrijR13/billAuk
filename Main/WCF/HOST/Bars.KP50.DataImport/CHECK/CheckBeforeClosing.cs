using System;
using System.Data;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK
{
    /// <summary>Базовый класс проверок перед закрытием месяца</summary>
    public abstract class CheckBeforeClosing : IDisposable
    {
        protected int Month { get; set; }
        protected int Year { get; set; }
        protected bool IsCritical { get; set; }

        /// <summary>
        /// Входные параметры
        /// </summary>
        protected CheckBeforeClosingParams inputParams { get; set; }

        public CheckBeforeClosing(CheckBeforeClosingParams Params)
        {
            inputParams = Params;
            OpenConnection();
        }

        public void Dispose()
        {
            CloseConnection();
        }

        /// <summary>Соединение с БД </summary>
        /// 
        protected virtual IDbConnection Connection { get; set; }

        #region Вспомогательные методы работы с БД

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        protected IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(Constants.cons_Webdata);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.text);
                }
            }

            return Connection;
        }
        
        /// <summary>Закрыть соединение с БД</summary>
        /// 
        protected void CloseConnection()
        {
            try
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Не удалось закрыть соединение", ex);
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
        protected virtual void ExecSQL(string sql, IDbTransaction transaction)
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
        /// <param name="transaction">Транзакция (Необязательный параметр)</param>

        protected virtual void ExecSQL(string sql, bool inlog, int timeout, IDbTransaction transaction = null)
        {
            var result = DBManager.ExecSQL(Connection, transaction, sql, inlog, timeout);
            if (!result.result && inlog)
            {
                throw new Exception(result.text);
            }
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
                throw new Exception(result.text);
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
                throw new Exception(result.text);
            }
        }

        /// <summary>Получить ключ вставленной записи</summary>
        /// 
        protected virtual int GetSerialValue()
        {
            return DBManager.GetSerialValue(Connection);
        }

        #endregion Вспомогательные методы работы с БД


        /// <summary>
        /// Старт проверки
        /// </summary>
        public abstract Returns StartCheck();

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public abstract void CreateCheckBeforeClosingReport();

        /// <summary>
        /// Запись в таблицу проверок (central_data.CheckChMon)
        /// </summary>
        protected void InsertIntoCheckChMon(int CheckGroupId, bool is_success, string t_table)
        {
            string sql = 
                " DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + "checkChMon " +
                " WHERE  month_='" + Month + "'  AND yearr='" + Year + "'" +
                " AND (nzp_grp='" + CheckGroupId + "' or nzp_grp='0' ) and pref='" + inputParams.Bank.pref + "'";
            ExecSQL(sql);

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "checkChMon " +
                " (dat_check,month_,yearr,note,nzp_grp,pref,name_prov, status_, is_critical ) " +
                " VALUES ( " + DBManager.sCurDateTime + " ,'" + Month + "','" + Year + "'," +
                " (SELECT trim(ngroup)||' : " + (is_success ? "успешно" : "не прошла") + "'" +
                " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + CheckGroupId + ")," + 
                CheckGroupId + ",'" + inputParams.Bank.pref + "'," +
                " (SELECT MAX(substr(ngroup, 1, 40)) FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + CheckGroupId + ")," +
                " '" + (is_success ? 1 : 2) + "','" + IsCritical.ToInt() + "')";
            ExecSQL(sql);

            sql =
                " DELETE FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "link_group " +
                " WHERE nzp_group = " + CheckGroupId;
            ExecSQL(sql);

            if (!is_success)
            {
                sql =
                    " INSERT INTO " + inputParams.Bank.pref + DBManager.sDataAliasRest + "link_group" +
                    " (nzp,nzp_group)" +
                    " SELECT DISTINCT nzp,nzp_group" +
                    " FROM " + t_table;
                ExecSQL(sql);
            }
        }


        /// <summary>
        /// Выставление прогресса выполнения
        /// </summary>
        /// <param name="progress">Прогресс выполнения (от 0 до 100)</param>
        public static void SetCheckBeforeClosingProgress(decimal progress)
        {
        }

        /// <summary>
        /// Записать в таблицу системных событий
        /// </summary>
        /// <param name="pref">Префикс банка данных, в котором произошло данное событие</param>
        /// <param name="nzpUser">Уникальный код пользователя, совершившего данное действие</param>
        /// <param name="nzpDict">Уникальный код события</param>
        /// <param name="nzpObj">Уникальный код сущности, по отношению которого было совершено действие</param>
        /// <param name="note">Расшифровка действия</param>
        /// <param name="transaction">Транзакция (необязательный параметр)</param>
        public void InsertSysEvent(string pref, int nzpUser, int nzpDict, int nzpObj, string note, IDbTransaction transaction = null)
        {
            try
            {
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = pref,
                    nzp_user = nzpUser,
                    nzp_dict = nzpDict,
                    nzp_obj = nzpObj,
                    note = note
                }, transaction, Connection);
            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog(
                    String.Format
                        (
                            "Ошибка при записи в таблицу системных событий! Ошибка выполнения процедуры {0}{1}{2}",
                            System.Reflection.MethodInfo.GetCurrentMethod().Name + Environment.NewLine,
                            ex.Message + Environment.NewLine,
                            ex.StackTrace + Environment.NewLine
                        ),
                    MonitorLog.typelog.Error,
                    true);
                throw;
            }
            
        }

        /// <summary>
        /// Записать событие "Проверки перед закрытием месяца"
        /// </summary>
        /// <param name="pref">Префикс банка данных, в котором произошло данное событие</param>
        /// <param name="nzpUser">Уникальный код пользователя, совершившего данное действие</param>
        /// <param name="note">Расшифровка действия</param>
        /// <param name="transaction">Транзакция (необязательный параметр)</param>
        public void InsertSysEvent(string pref, int nzpUser, string note)
        {
            //Уникальный код события "Проверки перед закрытием месяца"
            const int nzpDict = 8217;
            
            InsertSysEvent(pref, nzpUser, nzpDict, 0, note);
        }

    }

    /// <summary>
    /// Идентификаторы групп проверок
    /// </summary>
    public enum ECheckGroupId
    {
        RassogPaymentDistrib = 3,
        RassoglPaymentInSaldo = 4,
        RassoglPaymentPerekidka = 5,
        TooBigIPUVal = 6,
        TooBigKvarPUVal = 7,
        TooBigGroupVal = 8,
        TooBigODPUVal = 9,
        NotCalcNedop = 10,
        IsmAfterRasprOdn = 11,
        ChangedParam = 12,
        InOutSaldo = 14,
        FinMonthOperDay = 15,
        BigPayment = 16,
        ValIPUWithoutPU = 17,
        ValODPUWithoutODPU = 20,
        ValGrPUWithoutGrPU = 21,
        DoubleChargeTrio = 18,
        FinBankExists = 19,
        DoubleValIPU = 22,
        DoubleValODPU = 23,
        DoubleValGrPU = 24,
        LsWithoutAccrual = 25
    }
}
