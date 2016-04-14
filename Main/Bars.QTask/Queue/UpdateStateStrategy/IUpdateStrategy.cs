namespace Bars.QTask.Queue.UpdateStateStrategy
{
    /// <summary>
    /// Стратегия изменения статуса
    /// </summary>
    internal interface IUpdateStrategy
    {
        /// <summary>
        /// Строка обновления
        /// </summary>
        string UpdateStatement { get; }
    }
}
