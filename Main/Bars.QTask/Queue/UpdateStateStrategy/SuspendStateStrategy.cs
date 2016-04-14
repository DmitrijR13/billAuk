using Bars.QTask.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace Bars.QTask.Queue.UpdateStateStrategy
{
    /// <summary>
    /// Стратегия изменения на статус приостановки
    /// </summary>
    [Export(typeof(IUpdateStrategy))]
    [ExportMetadata("Action", TaskAction.Suspend)]
    public class SuspendStateStrategy : IUpdateStrategy
    {
        /// <summary>
        /// Статусы, при которых возможна приостановка задачи
        /// </summary>
        private readonly ReadOnlyCollection<string> _states = null;

        /// <summary>
        /// Строка обновления
        /// </summary>
        public string UpdateStatement
        {
            get
            {
                return string.Format(
                    "CASE WHEN TaskState IN ({0}) THEN {1:D} ELSE TaskState END",
                    string.Join(", ", _states), TaskState.SuspendRequired);
            }
        }

        /// <summary>
        /// Стратегия изменения на статус приостановки
        /// </summary>
        public SuspendStateStrategy()
        {
            _states = new ReadOnlyCollection<string>(new [] {
                TaskState.Queued.ToString("D"),
                TaskState.Executing.ToString("D")
            }.ToList());
        }
    }
}
