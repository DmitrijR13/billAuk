using Bars.QTask.Queue;
using STCLINE.KP50.DataBase;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Задача для выполнения в планировщике задач
    /// </summary>
    public abstract class ExecutableTask : DataBaseHead, IExecutableTask
    {
        /// <summary>
        /// Тип задачи
        /// </summary>
        public TaskType TaskType { get; protected internal set; }

        /// <summary>
        /// Наименование задачи
        /// </summary>
        public string Description { get; protected internal set; }

        /// <summary>
        /// Задача обработки
        /// </summary>
        protected internal Task Task { get; set; }

        /// <summary>
        /// Тип объекта контейнера для десереализации
        /// </summary>
        public virtual Type ContainerType { get { return typeof(object); } }

        /// <summary>
        /// Идентификатор задачи
        /// </summary>
        public int Identifier { get; protected internal set; }

        /// <summary>
        /// Статус задачи
        /// </summary>
        protected internal TaskState _state;

        /// <summary>
        /// Статус задачи
        /// </summary>
        public TaskState State
        { 
            get{ return _state; }
            protected internal set {
                _state = value;
                QueueService.Instance.RegisterAction(Identifier, "TaskState", _state.ToString("D"));
            }
        }

        /// <summary>
        /// Приоритет задачи
        /// </summary>
        public TaskPriority Priority { get; protected internal set; }

        /// <summary>
        /// Рассчетная дата
        /// </summary>
        public DateTime? CalculationDate { get; protected internal set; }

        /// <summary>
        /// Запустить задачу на выполнение после указанного времени
        /// </summary>
        private DateTime? _runAfter;

        /// <summary>
        /// Запустить задачу на выполнение после указанного времени
        /// </summary>
        public DateTime? RunAfter
        {
            get { return _runAfter.HasValue ? (DateTime?)_runAfter.Value.ToLocalTime() : null; }
            protected internal set { _runAfter = value; }
        }

        /// <summary>
        /// Поставлена в очередь
        /// </summary>
        private DateTime? _queued;

        /// <summary>
        /// Поставлена в очередь
        /// </summary>
        public DateTime? Queued
        {
            get { return _queued.HasValue ? (DateTime?)_queued.Value.ToLocalTime() : null; }
            protected internal set { _queued = value; }
        }

        /// <summary>
        /// Взята на исполнение
        /// </summary>
        protected internal DateTime? _perform;

        /// <summary>
        /// Взята на исполнение
        /// </summary>
        public DateTime? Perform
        {
            get { return _perform.HasValue ? (DateTime?)_perform.Value.ToLocalTime() : null; }
            protected internal set
            {
                _perform = value;
                QueueService.Instance.RegisterAction(Identifier, "TaskPerform", "TIMESTAMP '" + _perform.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            }
        }

        /// <summary>
        /// Выполнена
        /// </summary>
        protected internal DateTime? _compleated;

        /// <summary>
        /// Выполнена
        /// </summary>
        public DateTime? Compleated
        {
            get { return _compleated.HasValue ? (DateTime?)_compleated.Value.ToLocalTime() : null; }
            protected internal set
            {
                _compleated = value;
                QueueService.Instance.RegisterAction(Identifier, "TaskCompleated", "TIMESTAMP '" + _compleated.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            }
        }

        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        protected internal decimal _progress;

        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        public decimal Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                QueueService.Instance.RegisterAction(Identifier, "TaskProgress", _progress.ToString("0.0000", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Постановщик задачи в очередь
        /// </summary>
        public string Publisher { get; protected internal set; }

        /// <summary>
        /// Исполнитель задачи
        /// </summary>
        public string Processor { get; protected internal set; }

        /// <summary>
        /// Точка входа
        /// </summary>
        /// <param name="container">Параметр</param>
        /// <param name="token">Токен отмены задачи</param>
        public abstract void Execute(object container, ThreadValidationToken token);
    }
}
