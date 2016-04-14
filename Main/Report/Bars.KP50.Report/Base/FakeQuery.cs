namespace Bars.KP50.Report
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Threading;

    using Bars.KP50.Report.Base;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;

    public static class FakeQueue
    {
        public const int MaxThreads = 10;
        private  static readonly object LockObject = new object();
        private static readonly Thread[] ListThreads = new Thread[MaxThreads];
        private static readonly List<ReportThreadParams> ListReports = new List<ReportThreadParams>();

        static FakeQueue()
        {
            var thread = new Thread(() =>
                {
                    while (true)
                    {
                        var reportForExecute = ListReports.ToArray();
                        foreach (var reportParams in reportForExecute)
                        {
                            if (Start(reportParams))
                            {
                                lock (LockObject)
                                {
                                    ListReports.Remove(reportParams);
                                }
                            }
                        }

                        Thread.Sleep(1000);
                    }
                })
                { IsBackground = true };
            thread.Start();
        }

        public enum State
        {
            Wait,
            Progress,
            Done
        }

        /// <summary>Добавить отчет на выполение</summary>
        /// <param name="report">Отчет</param>
        /// <param name="parameters">Параметры задачи</param>
        /// <param name="nzpExcelUtility">Идентификатор записи в бд</param>
        /// <returns>Идентификатор созданной задачи</returns>
        public static string AddReport(IBaseReport report, NameValueCollection parameters, int nzpExcelUtility)
        {
            var taskId = Guid.NewGuid().ToString();
#warning Тут должно быть добавление в очередь, пока рулим сами
            lock (LockObject)
            {
                ListReports.Add(new ReportThreadParams { Report = report, Parameters = parameters, NzpExcelUtility = nzpExcelUtility });
            }

            return taskId;
        }

        /// <summary>Проверить статус задачи</summary>
        /// <param name="taskId">Идентификатор задачи</param>
        /// <returns>Статус</returns>
        public static State GetState(string taskId)
        {
            return State.Wait;
        }

        /// <summary>Закрыть задачу</summary>
        /// <param name="taskId">Идентификатор задачи</param>
        public static void CloseTask(string taskId)
        {
        }

        private static bool Start(ReportThreadParams reportParams)
        {
            var isStart = false;
            for (var i = 0; i < 10; i++)
            {
                if (ListThreads[i] != null && ListThreads[i].IsAlive)
                {
                    continue;
                }

                ListReports.Remove(reportParams);
                var thread = new Thread(ExecuteReport) { IsBackground = true };
                thread.Start(reportParams);
                ListThreads[i] = thread;
                isStart = true;
                break;
            }

            return isStart;
        }

        private static void ExecuteReport(object param)
        {
            IDbConnection connection = null;
            ReportThreadParams reportParams = null;
            string updateSql;

            try
            {
                reportParams = param as ReportThreadParams;

                try
                {
                    reportParams.Report.GenerateReport(reportParams.Parameters);
                }
                catch
                {                    
                    connection = GetConnection();
                    updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                    + "excel_utility set stats = -1 where nzp_exc = " + reportParams.NzpExcelUtility;
                    DBManager.ExecSQL(connection, updateSql, true);
                     
                    throw;
                }

                connection = GetConnection();
                updateSql = "update " + DBManager.GetFullBaseName(connection) + DBManager.tableDelimiter
                                + "excel_utility set stats = 2, "
                                + " dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) 
                                + " where nzp_exc = " + reportParams.NzpExcelUtility;
                DBManager.ExecSQL(connection, updateSql, true);
            }
            catch (Exception exc)
            {
                MonitorLog.WriteLog(
                    string.Format(
                        "Ошибка построения отчета ({0}): {1}",
                        reportParams == null ? 0 : reportParams.NzpExcelUtility,
                        exc.Message),
                    MonitorLog.typelog.Error,
                    true);
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }
        }

        private static IDbConnection GetConnection()
        {
            var connection = DBManager.GetConnection(Constants.cons_Webdata);
            var ret = DBManager.OpenDb(connection, true);
            if (!ret.result)
            {
                throw new Exception(string.Format("Не удалось открыть соединение с БД: \"{0}\"", ret.text));
            }

            return connection;
        }

        private class ReportThreadParams
        {
            public IBaseReport Report { get; set; }

            public NameValueCollection Parameters { get; set; }

            public int NzpExcelUtility { get; set; }
        }
    }
}