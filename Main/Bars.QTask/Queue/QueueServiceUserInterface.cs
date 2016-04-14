using Bars.QTask.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace Bars.QTask.Queue
{
    /// <summary>
    /// Сервис обработки фоновых задач
    /// </summary>
    public partial class QueueService
    {
        /// <summary>
        /// Запускает службу обработки фоновых задач
        /// </summary>
        /// <param name="degreeOfParallelism">Максимальное число потоков обработки задач</param>
        public static void StartQueueService(int degreeOfParallelism)
        {
            var service = QueueServiceCreator.CreateInstance(degreeOfParallelism);
            service.RestoreTasks();
            service.ListernQueue();
        }

        /// <summary>
        /// Останавливает службу обработки фоновых задач
        /// </summary>
        /// <param name="waitForCompleate">Ожидать завершения задач</param>
        public static void Shutdown()
        {
#if DEBUG
            Shutdown(false);
#else
            Shutdown(true);
#endif
        }

        /// <summary>
        /// Останавливает службу обработки фоновых задач
        /// </summary>
        /// <param name="waitForCompleate">Ожидать завершения задач</param>
        public static void Shutdown(bool waitForCompleate)
        {
            if (QueueService.Instance != null)
                QueueService.Instance.StopService(waitForCompleate);
        }

        /// <summary>
        /// Возвращает статусы задач, поставленных в очередь на исполнение
        /// </summary>
        /// <param name="limit">Ограничение количества задач</param>
        /// <param name="offset">Пропустить N первых задач</param>
        /// <returns>Статусы задач, поставленных в очередь на исполнение</returns>
        public static IEnumerable<IExecutableTask> GetTasksState(uint limit = 10, uint offset = 0)
        {
            var query = string.Format(
                "SELECT TaskId, TaskPriority, TaskType, TaskState, " +
                "	TaskCalculationDate, TaskRunAfter, TaskQueued, TaskPerform, " +
                "	TaskCompleated, TaskProgress, " +
                "	QueuePublisher, QueueProcessor " +
                "FROM {0} " +
                "ORDER BY TaskQueued DESC " +
                "OFFSET {1} LIMIT {2}", TableName, offset, limit);

            var ret = new List<IExecutableTask>();
            
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
                    while (reader.Read())
                    {
                        var TaskId = reader["TaskId"] != DBNull.Value ? Convert.ToInt32(reader["TaskId"]) : 0;
                        var TaskPriority = (TaskPriority)(reader["TaskPriority"] != DBNull.Value ? Convert.ToInt32(reader["TaskPriority"]) : 0);
                        var TaskType = (TaskType)(reader["TaskType"] != DBNull.Value ? Convert.ToInt32(reader["TaskType"]) : 0);
                        var taskState = (TaskState)(reader["TaskState"] != DBNull.Value ? Convert.ToInt32(reader["TaskState"]) : 0);
                        var TaskCalculationDate = reader["TaskCalculationDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskCalculationDate"]) : null;
                        var TaskRunAfter = reader["TaskRunAfter"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskRunAfter"]) : null;
                        var TaskQueued = reader["TaskQueued"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskQueued"]) : null;
                        var TaskPerform = reader["TaskPerform"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskPerform"]) : null;
                        var TaskCompleated = reader["TaskCompleated"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskCompleated"]) : null;
                        var TaskProgress = reader["TaskProgress"] != DBNull.Value ? Convert.ToDecimal(reader["TaskProgress"]) : 0.0m;
                        var QueuePublisher = reader["QueuePublisher"] != DBNull.Value ? reader["QueuePublisher"].ToString().Trim() : null;
                        var QueueProcessor = reader["QueueProcessor"] != DBNull.Value ? reader["QueueProcessor"].ToString().Trim() : null;

                        var metadata = new TaskExportAttribute(TaskType);
                        var obj = new StatusTask()
                        {
                            TaskType = metadata.TaskType,
                            Description = metadata.Description,
                            Identifier = TaskId,
                            Priority = TaskPriority,
                            _state = taskState,
                            CalculationDate = TaskCalculationDate,
                            RunAfter = TaskRunAfter,
                            Queued = TaskQueued,
                            _perform = TaskPerform,
                            _compleated = TaskCompleated,
                            _progress = TaskProgress,
                            Publisher = QueuePublisher,
                            Processor = QueueProcessor,
                        };
                        ret.Add(obj);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("В процессе получения списка процессов возникло исключение.", ex);
                    throw;
                }
                finally
                {
                    connection.Close();
                    if (reader != null)
                        reader.Close();
                }
            }
            return ret.AsReadOnly();
        }

        /// <summary>
        /// Возвращает статус указанной задачи
        /// </summary>
        /// <param name="identifier">Идентификатор задачи</param>
        /// <returns></returns>
        public static IExecutableTask GetTaskState(int identifier)
        {
            var query = string.Format(
                "SELECT TaskId, TaskPriority, TaskType, TaskState, " +
                "	TaskRunAfter, TaskQueued, TaskPerform, " +
                "	TaskCompleated, TaskProgress, " +
                "	QueuePublisher, QueueProcessor " +
                "FROM {0} queue " +
                "WHERE TaskId = {2}", TableName, identifier);

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
                    while (reader.Read())
                    {
                        var TaskId = reader["TaskId"] != DBNull.Value ? Convert.ToInt32(reader["TaskId"]) : 0;
                        TaskPriority TaskPriority = (TaskPriority)(reader["TaskPriority"] != DBNull.Value ? Convert.ToInt32(reader["TaskPriority"]) : 0);
                        TaskType TaskType = (TaskType)(reader["TaskType"] != DBNull.Value ? Convert.ToInt32(reader["TaskType"]) : 0);
                        TaskState taskState = (TaskState)(reader["TaskState"] != DBNull.Value ? Convert.ToInt32(reader["TaskState"]) : 0);
                        DateTime? TaskRunAfter = reader["TaskRunAfter"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskRunAfter"]) : null;
                        DateTime? TaskQueued = reader["TaskQueued"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskQueued"]) : null;
                        DateTime? TaskPerform = reader["TaskPerform"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskPerform"]) : null;
                        DateTime? TaskCompleated = reader["TaskCompleated"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["TaskCompleated"]) : null;
                        var TaskProgress = reader["TaskProgress"] != DBNull.Value ? Convert.ToDecimal(reader["TaskProgress"]) : 0.0m;
                        var QueuePublisher = reader["QueuePublisher"] != DBNull.Value ? reader["QueuePublisher"].ToString().Trim() : null;
                        var QueueProcessor = reader["QueueProcessor"] != DBNull.Value ? reader["QueueProcessor"].ToString().Trim() : null;

                        var metadata = new TaskExportAttribute(TaskType);
                        return new StatusTask()
                        {
                            TaskType = metadata.TaskType,
                            Description = metadata.Description,
                            Identifier = TaskId,
                            Priority = TaskPriority,
                            _state = taskState,
                            RunAfter = TaskRunAfter,
                            Queued = TaskQueued,
                            _perform = TaskPerform,
                            _compleated = TaskCompleated,
                            _progress = TaskProgress,
                            Publisher = QueuePublisher,
                            Processor = QueueProcessor,
                        };
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("В процессе получения списка процессов возникло исключение.", ex);
                    throw;
                }
                finally
                {
                    connection.Close();
                    if (reader != null)
                        reader.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Ставит задачу в очередь на выполнение
        /// </summary>
        /// <param name="type">Тип зпдпчи к выполнению</param>
        /// <param name="parameter">Параметр функции</param>
        /// <param name="runAfter">Запустить после указанного времени</param>
        /// <returns>Идентификатор поставленной задачи</returns>
        public static int QueueTask(TaskType type, object parameter = null, DateTime? calculationDate = null, TaskPriority priority = TaskPriority.Low, DateTime? runAfter = null)
        {
            var query = string.Format(
                "INSERT INTO {0} " +
                "  (TaskType, TaskPriority, TaskState, TaskRunAfter, TaskQueued, " +
                "  TaskParameter, QueuePublisher, QueuePublisherAddress, TaskCalculationDate) " +
                "SELECT {1:D}, {2:D}, {3:D}, {4}, TIMESTAMP '{5:yyyy-MM-dd HH:mm:ss}', " +
                "  {6}, '{7}', '{8}', TIMESTAMP '{11:yyyy-MM-dd HH:mm:ss}' " +
                "WHERE NOT EXISTS (SELECT 1 FROM Queue WHERE TaskType = {1:D} AND TaskState NOT IN ({9}) AND TaskParameter {10} {6}) " +
                "RETURNING TaskId ", TableName, type, priority, TaskState.New,
                runAfter != null ? "TIMESTAMP '" + ((DateTime)runAfter).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL",
                DateTime.UtcNow, parameter != null ? "'" + (new JavaScriptSerializer()).Serialize(parameter) + "'" : "NULL",
                QueueIdentifier, QueueAddress, string.Join(", ", _stateLevels[3].Select(x=>x.ToString("D"))), parameter != null ? "ILIKE" : "IS",
                calculationDate.HasValue ? calculationDate.Value : Points.CalcMonth.RecordDateTime);
            try
            {
                // Use external connection factory only
                using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
                {
                    DBManager.OpenDb(connection, true);
                    var identifier = DBManager.ExecScalar<int>(connection, query);
                    connection.Close();
                    return identifier == 0 ? -1 : identifier;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("В процессе постановки задачи в очередь возникло исключение.", ex);
                throw;
            }
        }

        /// <summary>
        /// Обновляет задачу
        /// </summary>
        /// <param name="identifier">Идентификатор задачи</param>
        /// <param name="action">Действие над задачей</param>
        /// <param name="priority">Приоритет задачи</param>
        /// <param name="runAfter">Запустить задачу после</param>
        public static void UpdateTask(int identifier, TaskAction action = TaskAction.None, TaskPriority priority = TaskPriority.Default, DateTime? runAfter = null)
        {
            var updates = new List<KeyValuePair<string, string>>();
            var strategy = _updateStrategys.FirstOrDefault(x => x.Metadata.Action == action);
            if (strategy != null) updates.Add(new KeyValuePair<string, string>("TaskState", strategy.Value.UpdateStatement));
            if (priority != TaskPriority.Default) 
                updates.Add(new KeyValuePair<string, string>("TaskPriority", priority.ToString("D")));
            if (runAfter.HasValue) updates.Add(new KeyValuePair<string, string>(
                 "TaskRunAfter", "TIMESTAMP '" + runAfter.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"));

            if (updates.Any())
            {
                var query = string.Format(
                    "UPDATE {0} SET ({1}) = ({2}) WHERE TaskId = {3}",
                    TableName, string.Join(", ", updates),
                    string.Join(", ", updates.Select(x => x.Value)), identifier);

                // Use external connection factory only
                using (var connection = DatabaseConnectionKernel.GetConnection(Constants.cons_Kernel))
                {
                    if (DBManager.OpenDb(connection, true).result)
                        DBManager.ExecSQL(connection, query, true);
                    connection.Close();
                }
            }
        }
    }
}
