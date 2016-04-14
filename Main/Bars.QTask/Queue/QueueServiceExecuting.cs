using Bars.QTask.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Bars.QTask.Queue
{
    /// <summary>
    /// Сервис обработки фоновых задач
    /// </summary>
    public partial class QueueService
    {
        /// <summary>
        /// Фабрика задач
        /// </summary>
        private readonly TaskFactory _factory = null;

        /// <summary>
        /// Источник токена отмены задания
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = null;

        /// <summary>
        /// Обрабатывыются ли задачи
        /// </summary>
        private volatile bool _isProcessingItems = false;

        /// <summary>
        /// Планировщик
        /// </summary>
        private readonly QueueScheduler _scheduler = null;

        /// <summary>
        /// Планировщик
        /// </summary>
        internal QueueScheduler Scheduler { get { return _scheduler; } }

        /// <summary>
        /// Выполнить задачу
        /// </summary>
        /// <param name="task">Задача</param>
        /// <param name="parameter">Параметр задачи</param>
        private void ExecuteTask(ExecutableTask task, object parameter)
        {
            task.Task = _factory.StartNew(() =>
            {
                task.State = TaskState.Executing;
                task.Perform = DateTime.UtcNow;
                try
                {
                    task.Execute(parameter, new ThreadValidationToken(task.Identifier, _cancellationTokenSource.Token));
                    task.State = TaskState.Executed;
                }
                catch (OperationCanceledException) { task.State = TaskState.Cancelled; }
                catch (Exception) { task.State = TaskState.Aborted; }
                finally { task.Compleated = DateTime.UtcNow; }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Слушает очередь на наличие новых задач
        /// </summary>
        private void ListernQueue()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                _isProcessingItems = true;
                while (_isProcessingItems)
                {
                    if (_scheduler.FreeThreads > 0)
                    {
                        var query = string.Format(
                            "UPDATE {0} alias_queue " +
                            "SET (TaskState, QueueProcessor, TaskPerform, TaskProgress) = " +
                            "	({1:D}, '{2}', TIMESTAMP '{3:yyyy-MM-dd HH:mm:ss}', 0) " +
                            "FROM ( " +
                            "	SELECT * " +
                            "	FROM {0} " +
                            "	WHERE TaskState = {4:D} AND ( " +
                            "		TaskRunAfter IS NULL OR " +
                            "		TaskRunAfter <= TIMESTAMP '{3:yyyy-MM-dd HH:mm:ss}') AND " +
                            "		TaskType IN ({5}) " +
#if DEBUG
                            "		AND TRIM(QueuePublisherAddress) ILIKE ('{6}') " +
#endif
                            "	ORDER BY TaskPriority, TaskQueued " +
                            "	LIMIT {7} " +
                            "	FOR UPDATE " +
                            ") collection " +
                            "WHERE alias_queue.TaskId = collection.TaskId " +
                            "RETURNING alias_queue.TaskId, alias_queue.TaskPriority, alias_queue.TaskType, alias_queue.TaskState, " +
                            "	alias_queue.TaskCalculationDate, alias_queue.TaskRunAfter, alias_queue.TaskQueued, alias_queue.TaskPerform, " +
                            "	alias_queue.TaskCompleated, alias_queue.TaskProgress, alias_queue.TaskParameter, " +
                            "	alias_queue.QueuePublisher, alias_queue.QueueProcessor ",
                            TableName, TaskState.Queued, QueueIdentifier, DateTime.UtcNow, TaskState.New, 
                            string.Join(", ", _taskContainer.SupportedTasks), QueueAddress, _scheduler.FreeThreads);

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
                                var abortedTasks = new List<int>();
                                while (reader.Read())
                                {
                                    var TaskId = reader["TaskId"] != DBNull.Value ? Convert.ToInt32(reader["TaskId"]) : 0;
                                    try
                                    {
                                        var TaskPriority = (TaskPriority)(reader["TaskPriority"] != DBNull.Value ? Convert.ToInt32(reader["TaskPriority"]) : 0);
                                        var TaskType = (TaskType)(reader["TaskType"] != DBNull.Value ? Convert.ToInt32(reader["TaskType"]) : 0);
                                        var taskState = (TaskState)(reader["TaskState"] != DBNull.Value ? Convert.ToInt32(reader["TaskState"]) : 0);
                                        var TaskCalculationDate = reader["TaskCalculationDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskCalculationDate"]) : null;
                                        var TaskRunAfter = reader["TaskRunAfter"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskRunAfter"]) : null;
                                        var TaskQueued = reader["TaskQueued"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskQueued"]) : null;
                                        var TaskProgress = reader["TaskProgress"] != DBNull.Value ? Convert.ToDecimal(reader["TaskProgress"]) : 0.0m;
                                        var QueuePublisher = reader["QueuePublisher"] != DBNull.Value ? reader["QueuePublisher"].ToString().Trim() : null;
                                        var JsonParameter = reader["TaskParameter"] != DBNull.Value ? reader["TaskParameter"].ToString().Trim() : null;

                                        var objectInstance = _taskContainer.CreateInstance(TaskType);
                                        objectInstance.Identifier = TaskId;
                                        objectInstance.Priority = TaskPriority;
                                        objectInstance._state = taskState;
                                        objectInstance.CalculationDate = TaskCalculationDate;
                                        objectInstance.RunAfter = TaskRunAfter;
                                        objectInstance.Queued = TaskQueued;
                                        objectInstance._progress = TaskProgress;
                                        objectInstance.Publisher = QueuePublisher;
                                        var parameter = objectInstance.ContainerType != typeof(object) && !string.IsNullOrWhiteSpace(JsonParameter) ?
                                            (new JavaScriptSerializer()).Deserialize(JsonParameter, objectInstance.ContainerType) : null;
                                        ExecuteTask(objectInstance, parameter);
                                        lock (_tasks) _tasks.Add(objectInstance);
                                    }
                                    catch (Exception ex)
                                    {
                                        abortedTasks.Add(TaskId);
                                        MonitorLog.WriteException("Не удалось поставить задачу в очередь", ex);
                                    }
                                }
                                if (abortedTasks.Any())
                                {
                                    query = string.Format(
                                        "UPDATE {0} " +
                                        "SET (TaskState, TaskCompleated) = ({1:D}, TIMESTAMP '{2:yyyy-MM-dd HH:mm:ss}') " +
                                        "WHERE TaskId IN ({3})",
                                        TableName, TaskState.Aborted, DateTime.UtcNow, string.Join(", ", abortedTasks));
                                    result = DBManager.ExecSQL(connection, query, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteException("Не удалось поставить задачу в очередь", ex);
                            }
                            finally
                            {
                                connection.Close();
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                    }

                    ValidateTasks();
                    Thread.Sleep(5000);
                }
            }, null);
        }
    }
}
