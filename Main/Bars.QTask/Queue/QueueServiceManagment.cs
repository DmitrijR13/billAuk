using Bars.QTask.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bars.QTask.Queue
{
    /// <summary>
    /// Сервис обработки фоновых задач
    /// </summary>
    public partial class QueueService
    {
        /// <summary>
        /// Список задач для обновления в СУБД
        /// </summary>
        private readonly Dictionary<int, Dictionary<string, string>> _updates = new Dictionary<int, Dictionary<string, string>>();

        /// <summary>
        /// Контейнер экспорта реализаций заданий
        /// </summary>
        private readonly TaskContainer _taskContainer = null;

        /// <summary>
        /// Singleton
        /// </summary>
        private sealed class QueueServiceCreator
        {
            /// <summary>
            /// Сервис обработки задач
            /// </summary>
            private static QueueService _instance = null;

            /// <summary>
            /// Сервис обработки фоновых задач
            /// </summary>
            /// <param name="degreeOfParallelism">Количество потоков обработки</param>
            /// <returns>Сервис обработки фоновых задач</returns>
            internal static QueueService CreateInstance(int degreeOfParallelism)
            {
                if (_instance != null) throw new TypeLoadException("Instance of object already existed.");
                return (_instance = (QueueService)typeof(QueueService).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new[] { typeof(int) }, new ParameterModifier[] { }).Invoke(new object[] { degreeOfParallelism }));
            }

            /// <summary>
            /// Возвращает созданный сервис обработки задач
            /// </summary>
            internal static QueueService Instance { get { return _instance; } }

            /// <summary>
            /// Освобождает ресурсы
            /// </summary>
            internal static void Release() { _instance = null; }
        }

        /// <summary>
        /// Возвращает задачу с указанным идентификатором
        /// </summary>
        /// <param name="identifier">Идентификатор задачи</param>
        /// <returns>Задача</returns>
        protected internal ExecutableTask GetTask(int identifier)
        {
            return _tasks.FirstOrDefault(x => x.Identifier == identifier);
        }

        /// <summary>
        /// Обновляет задачи, если они были изменены
        /// </summary>
        private void ValidateTasks()
        {
            lock (_updates)
            {
                if (_updates.Any())
                {
                    foreach (var state in _updates.Where(x => x.Value.ContainsKey("TaskState")))
                    {
                        var level = _stateLevels.First(x => x.Value.Contains((TaskState)Convert.ToInt32(state.Value["TaskState"]))).Key;
                        var inside = new List<string>();
                        foreach (var col in _stateLevels.Where(x => x.Key <= level).Select(x => x.Value.Select(y => y.ToString("D"))))
                            inside = inside.Union(col).ToList();
                        state.Value["TaskState"] = string.Format("CASE WHEN TaskState IN ({0}) THEN {1} ELSE TaskState END",
                            string.Join(", ", inside), state.Value["TaskState"]);
                    }

                    // Use external connection factory only
                    using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
                    {
                        try
                        {
                            if (!DBManager.OpenDb(connection, true).result) throw new Exception("Can not open connection.");
                            foreach (var update in _updates)
                            {
                                var query = string.Format(
                                    "UPDATE {0} SET ({1}) = ({2}) WHERE TaskId = {3}",
                                    TableName, string.Join(", ", update.Value.Keys),
                                    string.Join(", ", update.Value.Values), update.Key);
                                DBManager.ExecSQL(connection, query, true);
                            }
                            _updates.Clear();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog(
                                "QTask: Exception was throwed while update process states in process.\n" + ex,
                                MonitorLog.typelog.Warn, true);
                        }
                        finally { connection.Close(); }
                    }
                }

                lock (_tasks)
                {
                    var compleated = new List<ExecutableTask>(_tasks.Where(x => _stateLevels[3].Contains(x.State)));
                    foreach (var task in compleated)
                        _tasks.Remove(task);
                    if (_tasks.Any())
                    {
                        var query = string.Format(
                            "SELECT TaskId, TaskState FROM {0} WHERE TaskId IN ({1})",
                            TableName, string.Join(", ", _tasks.Select(x => x.Identifier)));
                        
                        // Use external connection factory only
                        using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
                        {
                            MyDataReader reader = null;
                            try
                            {
                                if (!DBManager.OpenDb(connection, true).result) throw new Exception("Can not open connection.");
                                DBManager.ExecRead(connection, out reader, query, true);
                                while (reader.Read())
                                {
                                    var TaskId = reader["TaskId"] != DBNull.Value ? Convert.ToInt32(reader["TaskId"]) : 0;
                                    var TaskState = reader["TaskState"] != DBNull.Value ? (TaskState)Convert.ToInt32(reader["TaskState"]) : 0;
                                    _tasks.First(x => x.Identifier == TaskId)._state = TaskState;
                                }
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteLog(
                                    "QTask: Exception was throwed while validation in process.\n" + ex,
                                    MonitorLog.typelog.Warn, true);
                            }
                            finally
                            {
                                connection.Close();
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Возвращает рабочий сервис обработки задач
        /// </summary>
        protected internal static QueueService Instance { get { return QueueServiceCreator.Instance; } }

        /// <summary>
        /// Регистрирует действие для обновления задачи в СУБД
        /// </summary>
        /// <param name="identifier">Идентификатор задачи</param>
        /// <param name="key">Обновленное поле</param>
        /// <param name="value">Новое значение</param>
        protected internal void RegisterAction(int identifier, string key, string value)
        {
            lock (_updates)
            {
                if (!_updates.ContainsKey(identifier))
                    _updates.Add(identifier, new Dictionary<string, string>());
                _updates[identifier][key] = value;
            }
        }

        /// <summary>
        /// Регистрирует действие для обновления задачи в СУБД
        /// </summary>
        /// <param name="identifier">Идентификатор задачи</param>
        /// <param name="key">Обновленное поле</param>
        /// <param name="value">Новое значение</param>
        protected internal void SetState(int identifier, string value)
        {
            var query = string.Format(
                "UPDATE {0} SET (TaskState) = ({1}) WHERE TaskId = {2}",
                TableName, value, identifier);
            
            // Use external connection factory only
            using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
            {
                MyDataReader reader = null;
                try
                {
                    if (!DBManager.OpenDb(connection, true).result) throw new Exception("Can not open connection.");
                    DBManager.ExecRead(connection, out reader, query, true);
                }
                finally
                {
                    connection.Close();
                    if (reader != null)
                        reader.Close();
                }
            }
        }

        /// <summary>
        /// Восстанавливает статусы невыполненных задач после падения службы
        /// </summary>
        private void RestoreTasks()
        {
            var query = string.Format(
                "UPDATE {0} SET ( " +
                "TaskState, TaskPerform, TaskCompleated, " +
                "TaskProgress, QueueProcessor) = " +
                "(CASE WHEN TaskState IN ({1}) THEN {2:D} ELSE {3:D} END, " +
                "NULL, NULL, 0, NULL) " +
                "WHERE TRIM(QueueProcessor) ILIKE '{4}' AND TaskState IN ({5}) RETURNING TaskId",
                TableName, string.Join(", ", _stateLevels[1].Select(x => x.ToString("D"))),
                TaskState.New, TaskState.Aborted, QueueIdentifier,
                string.Join(", ", _stateLevels[1].Select(x => x.ToString("D")).
                Union(_stateLevels[2].Select(y => y.ToString("D")))));

            // Use external connection factory only
            using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
            {
                MyDataReader reader = null;
                try
                {
                    var result = DBManager.OpenDb(connection, true);
                    if (!result.result) throw new Exception(result.text);
                    result = DBManager.ExecRead(connection, out reader, query, true);
                    if (!result.result) throw new Exception(result.text);
                    var tasks = new List<int>();
                    while (reader.Read())
                        if (reader["TaskId"] != DBNull.Value) tasks.Add(Convert.ToInt32(reader["TaskId"]));
                    if (tasks.Any())
                        MonitorLog.WriteLog(string.Format(
                            "Tasks with identifiers {0} was restored.",
                            string.Join(", ", tasks)),
                            MonitorLog.typelog.Warn, true);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Завершает работу службы
        /// </summary>
        /// <param name="waitForCompleate">Ожидать завершения задач</param>
        private void StopService(bool waitForCompleate = false)
        {
            _scheduler.NotifyStoppedService();
            ValidateTasks();
            var cancelled = new List<ExecutableTask>(_tasks.Where(x => x.State == TaskState.Queued));
            _scheduler.NotifyTasksDequeued(cancelled.Select(x => x.Task).ToArray());
            foreach (var task in cancelled)
            {
                SetState(task.Identifier, TaskState.New.ToString("D"));
                _tasks.Remove(task);
            }
            if (!waitForCompleate)
                foreach (var task in _tasks)
                    task.State = TaskState.CancellationRequired;
            else
                foreach (var task in _tasks.Where(x => x.State == TaskState.Suspended || x.State == TaskState.SuspendRequired))
                    task.State = TaskState.ResumeRequired;
            try { Task.WaitAll(_tasks.Select(x => x.Task).ToArray()); }
            finally
            {
                _isProcessingItems = false;
                ValidateTasks();
                _scheduler.Release();
                _cancellationTokenSource.Dispose();
                _taskContainer.Dispose();
                QueueServiceCreator.Release();
            }
        }
        
        /// <summary>
        /// Сервис обработки фоновых задач
        /// </summary>
        /// <param name="degreeOfParallelism">Количество потоков обработки</param>
        private QueueService(int degreeOfParallelism = 5)
        {
            _taskContainer = new TaskContainer();
            _scheduler = new QueueScheduler(degreeOfParallelism);
            _cancellationTokenSource = new CancellationTokenSource();
            _factory = new TaskFactory(_cancellationTokenSource.Token,
                TaskCreationOptions.PreferFairness, TaskContinuationOptions.PreferFairness, _scheduler);
        }
    }
}
