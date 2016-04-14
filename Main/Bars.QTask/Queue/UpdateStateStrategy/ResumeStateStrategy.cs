using Bars.QTask.Tasks;
using System.ComponentModel.Composition;

namespace Bars.QTask.Queue.UpdateStateStrategy
{
    /// <summary>
    /// Стратегия изменения на статус возобновлен
    /// </summary>
    [Export(typeof(IUpdateStrategy))]
    [ExportMetadata("Action", TaskAction.Resume)]
    public class ResumeStateStrategy : IUpdateStrategy
    {
        /// <summary>
        /// Стратегия изменения на статус возобновлен
        /// </summary>
        public string UpdateStatement
        {
            get
            {
                return string.Format(
                    "CASE WHEN TaskState = {0:D} THEN {1:D} ELSE TaskState END",
                    TaskState.Suspended, TaskState.ResumeRequired);
            }
        }
    }
}
