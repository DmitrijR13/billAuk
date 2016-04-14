namespace Bars.QueueListener
{
    /// <summary>
    /// Перечислитель для состояний очереди
    /// </summary>
    public enum QueueState
    {
        /// <summary>
        /// В режиме ожидания
        /// </summary>
        Listen,

        /// <summary>
        /// Работает
        /// </summary>
        InProgress
    }
}
