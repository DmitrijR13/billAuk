using System.Configuration;
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

    /// <summary>Базовый отчет, ориентированный на получение данных через sql запросы c функциями получения дат</summary>
    public abstract class BaseSqlReportWithDates : BaseSqlReport
    {
        public static  DateTime Operday ;
        public  BaseSqlReportWithDates  ()
        {
            var SaldoDay = GetCurSaldoDay();
            Operday= SaldoDay == null ? DateTime.Now : Convert.ToDateTime(SaldoDay.Rows[0]["dat_oper"]);
        }
          
        /// <summary>
        /// Возвращает текущий расчетный день для отчетов
        /// </summary>
        /// <returns></returns>
        public static DataTable GetCurSaldoDay()
        {
            DataTable SaldoDay;
            try
            {
                string connectionString = Encryptor.Decrypt(ConfigurationManager.AppSettings[DBManager.ConfPref + "4"],
                    null);
                string pref = Encryptor.Decrypt(ConfigurationManager.AppSettings[DBManager.ConfPref + "10"], null);
                var connection = DBManager.GetConnection(connectionString);
                connection.Open();
                SaldoDay = DBManager.ExecSQLToTable(connection,
                    " select dat_oper from " + pref + DBManager.sDataAliasRest + "fn_curoperday ");
                connection.Close();
                 
            }
            catch (Exception e)
            {
                SaldoDay = null;
                MonitorLog.WriteException("Ошибка при попытке вытащить текущий расчетный день", e);
            }
            return SaldoDay;
        }


    }

}