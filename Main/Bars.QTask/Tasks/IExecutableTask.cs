using System;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Задача для выполнения в планировщике задач
    /// </summary>
    public interface IExecutableTask : IExecutableTaskMetadata
    {
        /// <summary>
        /// Приоритет задачи
        /// </summary>
        TaskPriority Priority { get; }

        /// <summary>
        /// Статус задачи
        /// </summary>
        TaskState State { get; }

        /// <summary>
        /// Идентификатор задачи
        /// </summary>
        int Identifier { get; }

        /// <summary>
        /// Рассчетная дата
        /// </summary>
        DateTime? CalculationDate { get; }

        /// <summary>
        /// Запустить задачу на выполнение после указанного времени
        /// </summary>
        DateTime? RunAfter { get; }

        /// <summary>
        /// Поставлена в очередь
        /// </summary>
        DateTime? Queued { get; }

        /// <summary>
        /// Взята на исполнение
        /// </summary>
        DateTime? Perform { get; }

        /// <summary>
        /// Выполнена
        /// </summary>
        DateTime? Compleated { get; }

        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        decimal Progress { get; }

        /// <summary>
        /// Постановщик задачи в очередь
        /// </summary>
        string Publisher { get; }

        /// <summary>
        /// Исполнитель задачи
        /// </summary>
        string Processor { get; }

        /// <summary>
        /// Точка входа
        /// </summary>
        /// <param name="container">Параметр</param>
        /// <param name="token">Токен отмены задачи</param>
        void Execute(object container, ThreadValidationToken token);
    }
}
