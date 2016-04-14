using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public abstract class DbQueueClient: DataBaseHead
    {
        protected FonTask task;

        public DbQueueClient()
        {
            task = null;
        }

        public DbQueueClient(FonTask Task): base()
        {
            task = Task;
        }

        /// <summary>
        /// Перезапускает зависшие задачи
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected static Returns ReQueueOldTasks(IDbConnection conn_web, string tableName)
        {
            string sql = " UPDATE " + tableName + " SET kod_info = " + (int)TaskStates.New + " WHERE kod_info = " + (int)TaskStates.InProcess +
#if PG
                " AND NOW() - INTERVAL '3 hours' > dat_in ";
#else
                " AND CURRENT - 3 UNITS HOUR > dat_in ";
#endif
            return DBManager.ExecSQL(conn_web, sql, true);
        }

        /// <summary>
        /// Создает таблицу в базе данных для очереди задач (при необходимости) 
        /// и перезапускает задачи, которые не выполнились за долгое время (зависшие задачи)
        /// </summary>
        /// /// <param name="conn_web"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        public virtual Returns PrepareQueue(IDbConnection conn_web, TaskQueue queue) { throw new NotImplementedException(); }

        /// <summary>
        /// Создает таблицу в базе данных для очереди задач (при необходимости) 
        /// и перезапускает задачи, которые не выполнились за долгое время (зависшие задачи)
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public Returns PrepareQueue(TaskQueue queue)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            try
            {
                ret = PrepareQueue(conn_web, queue);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("DbQueueClient.PrepareQueue\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_web.Close();
            }
            return ret;
        }

        public static bool IsAnyTaskBeingProcessed(IDbConnection conn_web, string tab, out Returns ret)
        {
//#if PG
//            DBManager.ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif

            MyDataReader reader = null;
            ret = Utils.InitReturns();

            try
            {
                ret = DBManager.ExecRead(conn_web, out reader,
                    " Select 1 From " + tab +
                    " Where kod_info = 0 and " + sNvlWord + "(dat_when, " + MDY(1, 1, 1900) + ") < " + sCurDateTime, true);
                if (!ret.result)
                {
                    return false;
                }
                if (reader.Read())
                {
                    ret.result = false;
                    ret.text = "Другой фоновый процесс уже выполнятеся. Подождите, пожалуйста!";
                    ret.tag = -1;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("DbQueueClient.IsQueueBusy\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return false;
        }

        /// <summary>
        /// Обновляет процент выполнения задания
        /// </summary>
        /// <param name="taskId">уникальный идентификатор задачи</param>
        /// <param name="progress">процент выпонения задачи (от 0 до 1)</param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected Returns SetTaskProgress(int taskId, decimal progress, string tableName)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

//#if PG
//            ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif

            string sql = "update " + sDefaultSchema + tableName + " set progress = " + progress.ToString("N4").Replace(',', '.') + " where nzp_key = " + taskId;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Добавляет задачу в очередь
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="calcfon"></param>
        /// <param name="ret"></param>
        public Returns AddTask(FonTask calcfon)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            try
            {
                ret = AddTask(conn_web, null, calcfon);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("DbQueueClient.AddTask\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_web.Close();
            }

            return ret;
        }

        public abstract Returns AddTask(IDbConnection conn_web, IDbTransaction transaction, FonTask calcfon);
        
        public abstract Returns CloseTask(IDbConnection conn_web, IDbTransaction transaction, FonTask calcfon);

        protected string makeWhereForProcess(FonTask finder, string alias, ref Returns ret)
        {
            if (alias != "") alias = alias + ".";

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return "";
            }

            DateTime datIn = DateTime.MinValue;
            DateTime datInPo = DateTime.MaxValue;

            if (finder.dat_in != "" && !DateTime.TryParse(finder.dat_in, out datIn))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }
            if (finder.dat_in_po != "" && !DateTime.TryParse(finder.dat_in_po, out datInPo))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }

            string where = "";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key.ToString();

            if (finder.dat_in_po != "")
            {
                where += " and " + alias + "dat_in < '" + datInPo.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";
                if (finder.dat_in != "")
                    where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "'";
            }
            else if (finder.dat_in != "")
                where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "' and "
                         + alias + "dat_in < '" + datIn.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";

            string prms = "";
            if (Utils.GetParams(finder.prms, Constants.act_process_in_queue.ToString()))
                prms += "," + FonTask.getKodInfo(Constants.act_process_in_queue);
            if (Utils.GetParams(finder.prms, Constants.act_process_active.ToString()))
                prms += "," + FonTask.getKodInfo(Constants.act_process_active);
            if (Utils.GetParams(finder.prms, Constants.act_process_finished.ToString()))
                prms += "," + FonTask.getKodInfo(Constants.act_process_finished);
            if (Utils.GetParams(finder.prms, Constants.act_process_with_errors.ToString()))
                prms += "," + FonTask.getKodInfo(Constants.act_process_with_errors);
            if (prms != "") where += " and " + alias + "kod_info in (" + prms.Substring(1, prms.Length - 1) + ")";

            return where;
        }

        protected string makeWhereForProcess(FonTaskWithYearMonth finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess((FonTask)finder, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.year_ > 0) where += " and " + alias + "year_ = " + finder.year_;
            if (finder.month_ > 0) where += " and " + alias + "month_ = " + finder.month_;

            return where;
        }
    }
}
