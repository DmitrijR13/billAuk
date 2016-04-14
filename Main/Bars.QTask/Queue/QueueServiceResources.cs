using Bars.QTask.Queue.UpdateStateStrategy;
using Bars.QTask.Tasks;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Bars.QTask.Queue
{
    /// <summary>
    /// Сервис обработки фоновых задач
    /// </summary>
    public partial class QueueService
    {
        /// <summary>
        /// Обрабатываемые задачи
        /// </summary>
        private readonly IList<ExecutableTask> _tasks = new List<ExecutableTask>();

        /// <summary>
        /// Контейнер для стратегий обновления
        /// </summary>
        private static readonly CompositionContainer _updateStrategyContainer = null;

        /// <summary>
        /// Стратегии обновления статусов
        /// </summary>
        [ImportMany(typeof(IUpdateStrategy))]
        private static readonly IEnumerable<Lazy<IUpdateStrategy, IUpdateStrategyMetadata>> _updateStrategys = null;

        /// <summary>
        /// Имя таблицы в СУБД
        /// </summary>
        protected internal const string TableName = "public.Queue";

        /// <summary>
        /// IP адрес сервиса
        /// </summary>
        protected internal static string QueueAddress
        {
            get
            {
                return Dns.GetHostEntry(Dns.GetHostName()).AddressList.
                    FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            }
        }

        /// <summary>
        /// Идентификатор сервиса
        /// </summary>
        protected internal static string QueueIdentifier { get { return WCFParams.AdresWcfHost != null ? WCFParams.AdresWcfHost.Adres : WCFParams.AdresWcfWeb.Adres; } }

        /// <summary>
        /// Уровни статусов задач
        /// </summary>
        protected internal static readonly Dictionary<int, ReadOnlyCollection<TaskState>> _stateLevels;

        /// <summary>
        /// Сервис обработки фоновых задач
        /// </summary>
        static QueueService()
        {
            _stateLevels = new Dictionary<int, ReadOnlyCollection<TaskState>>();
            _stateLevels.Add(0, new ReadOnlyCollection<TaskState>(new TaskState[] { TaskState.New }.ToList()));
            _stateLevels.Add(1, new ReadOnlyCollection<TaskState>(new TaskState[] { 
                TaskState.Queued,
                TaskState.Executing,
                TaskState.WaitingForFreeThread,
                TaskState.Suspended
            }.ToList()));
            _stateLevels.Add(2, new ReadOnlyCollection<TaskState>(new TaskState[] { 
                TaskState.CancellationRequired,
                TaskState.ResumeRequired,
                TaskState.SuspendRequired
            }.ToList()));
            _stateLevels.Add(3, new ReadOnlyCollection<TaskState>(new TaskState[] { 
                TaskState.Aborted,
                TaskState.Cancelled,
                TaskState.Executed
            }.ToList()));

            _updateStrategyContainer = new CompositionContainer(new AssemblyCatalog(typeof(IUpdateStrategy).Assembly));
            _updateStrategys = _updateStrategyContainer.GetExports<IUpdateStrategy, IUpdateStrategyMetadata>();
        }
    }
}
