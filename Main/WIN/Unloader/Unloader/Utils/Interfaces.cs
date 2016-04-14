using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using Npgsql;

namespace Unloader
{
    public interface IInstanse
    {
        event ProgressEventHandler mainProgress;
        List<AssembleAttribute> GetAllFormats();
        int Run(Request name);
        void StopResume(int unloadID, bool is_alive);
        List<string> GetDatabases();
        List<string> GetShemas(string db);
        Points GetPoints(string db);
        string GetConnectionString(string db);
    }

    /// <summary>
    /// Возвращаемый тип
    /// </summary>
    public class Returns
    {
        public Returns(bool result = true, string resultMessage = "")
        {
            this.result = result;
            this.resultMessage = resultMessage;
        }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string resultMessage { get; set; }
        /// <summary>
        /// Рузультат
        /// </summary>
        public bool result { get; set; }
    }

    public abstract class Unload : DB
    {
        /// <summary>
        /// Обработчик события остановки потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void onStop(object sender, StopArgs args)
        {
            if (args.unloadID != unloadID) return;
            if (args.is_alive)
            {
                UnloadInstanse.SendMessage(unloadID, "", Statuses.Stopped);
                events.Reset();
            }
            else
            {
                UnloadInstanse.SendMessage(unloadID, "", Statuses.Execute);
                events.Set();
            }
        }
        /// <summary>
        /// ID 
        /// </summary>
        public int unloadID { get; set; }
        public Points points { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public Returns Run(ref object dt)
        {
            try
            {
                OpenConnection();
                UnloadFromFile(ref dt);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            finally
            {
                CloseConnection();
            }
            return new Returns();
        }
        public abstract void UnloadFromFile(ref object dt);

        /// <summary>
        /// Событие отображающее текущий прогресс
        /// </summary>
        public event ProgressEventHandler Progress;
        /// <summary>
        /// Запуск события отображения прогресса
        /// </summary>
        protected virtual void SetProgress(decimal progress)
        {
            if (Progress != null)
                Progress(this, new ProgressArgs(progress, unloadID));
        }
        public ManualResetEvent events = new ManualResetEvent(true);
    }
}
