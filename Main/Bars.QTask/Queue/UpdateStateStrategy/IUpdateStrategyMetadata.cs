using Bars.QTask.Tasks;

namespace Bars.QTask.Queue.UpdateStateStrategy
{
    /// <summary>
    /// Метаданные стратерии обновления статуса
    /// </summary>
    public interface IUpdateStrategyMetadata
    {
        /// <summary>
        /// Запрошенное действие
        /// </summary>
        TaskAction Action { get; }
    }
}
