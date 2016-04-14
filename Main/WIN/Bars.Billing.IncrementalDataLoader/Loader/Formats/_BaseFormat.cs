using System;
using System.Data;
using System.Threading;
using Dapper;
using Bars.Billing.IncrementalDataLoader.Loader;
using Npgsql;

namespace Bars.Billing.IncrementalDataLoader.Formats
{
    public abstract class Format : IDisposable
    {
        public string AbsolutePath { get; set; }
        /// <summary>
        /// Событие отображающее текущий прогресс
        /// </summary>
        public event ProgressEventHandler Progress;
        public int nzp_load { get; set; }
        public string ConnectionString { get; set; }
        public string Schema { get; set; }
        public DateTime DateCharge { get; set; }
        public IDbConnection Connection { get; set; }
        public Request param { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        protected abstract string Run();
        public Statuses State = Statuses.InQueue;
        public ManualResetEvent Events = new ManualResetEvent(true);
        public string ConnString { get; set; }
        public string PsqlPath { get; set; }
        public string Database { get; set; }
        public string Port { get; set; }
        public string Server { get; set; }
        public string Password { get; set; }
        public string User { get; set; }
        public int nzp_user { get; set; }
        public string Username { get; set; }

        protected void OnStop(object sender, StopArgs args)
        {
            if (args.nzp_load != nzp_load) return;
            if (args.is_alive)
            {
                Instance.SendMessage(nzp_load, "", Statuses.Stopped);
                Events.Reset();
            }
            else
            {
                Instance.SendMessage(nzp_load, "", Statuses.Execute);
                Events.Set();
            }
        }
        protected virtual void SetProgress(decimal progress)
        {
            if (Progress != null)
                Progress(this, new ProgressArgs(progress, nzp_load));
        }
        public void Initialize()
        {
            State = Statuses.Execute;
            string link = null;
            var error = "";
            Instance.SendMessage(nzp_load, "", Statuses.Execute);
            try
            {
                OpenConnection();
                link = Run();
                State = Statuses.Finished;
            }
            catch (Exception ex)
            {
                State = Statuses.Error;
                error = "Ошибка:" + ex.Message;
                UpdateResultAndStatus(Connection, "Завершено с ошибкой(-ами)", (int)Statuses.Error, "Не верный формат файла.В процессе выполнения возникли ошибки:" + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
            Instance.SendMessage(nzp_load, error, State, link);
        }
        public IDbConnection OpenConnection(string conn = null)
        {
            if (conn != null) ConnectionString = conn;
            if (Connection == null)
            {
                Connection = new NpgsqlConnection(ConnString);
                if (Connection.State == ConnectionState.Closed) Connection.Open();
                else if (Connection.State == ConnectionState.Broken)
                {
                    Connection.Close();
                    Connection.Open();
                }
            }
            return Connection;
        }

        public void CloseConnection()
        {
            try
            {
                if (Connection == null) return;
                Connection.Close();
                Connection = null;
            }
            catch (Exception exc)
            {
                throw new Exception("Не удалось закрыть соединение", exc);
            }
        }

        public void UpdateResultAndStatus(IDbConnection conn, string status, int statusid, string result)
        {
            conn.Execute("UPDATE sys_imports " +
                               " SET status='" + status + "' , result = '" + result.Replace("\'", "\'\'") +
                               "', statusid = " + statusid + " WHERE nzp_load = " + nzp_load + "; ", null);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
