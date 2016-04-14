namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Метаданные объекта
    /// </summary>
    public interface IExecutableTaskMetadata
    {
        /// <summary>
        /// Тип объекта
        /// </summary>
        TaskType TaskType { get; }

        /// <summary>
        /// Имя объекта
        /// </summary>
        string Description { get; }
    }
}
