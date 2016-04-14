using Bars.QTask.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Bars.QTask.Queue.UpdateStateStrategy
{
    /// <summary>
    /// Стратегия изменения статуса
    /// </summary>
    [Export(typeof(IUpdateStrategy))]
    [ExportMetadata("Action", TaskAction.Cancel)]
    public class CancelStateStrategy : IUpdateStrategy
    {
        /// <summary>
        /// Строка обновления
        /// </summary>
        public string UpdateStatement
        {
            get
            {
                return string.Format(
                    "CASE WHEN TaskState IN ({0}) THEN {1:D} ELSE CASE WHEN TaskState = {2:D} THEN {3:D} ELSE TaskState END END",
                    string.Join(", ", QueueService._stateLevels[1].Select(x=>x.ToString("D"))), 
                    TaskState.CancellationRequired, TaskState.New, TaskState.Cancelled);
            }
        }
    }
}
