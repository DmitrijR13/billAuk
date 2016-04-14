using System.ComponentModel;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Приоритет задачи
    /// </summary>
    public enum TaskPriority : int
    {
        /// <summary>
        /// По умолчанию
        /// </summary>
        [Description("По умолчанию")]
        Default = 0,

        /// <summary>
        /// Низкий
        /// </summary>
        [Description("Низкий")]
        Low = 10,

        /// <summary>
        /// Средний
        /// </summary>
        [Description("Средний")]
        Normal = 20,

        /// <summary>
        /// Высокий
        /// </summary>
        [Description("Высокий")]
        High = 30
    }
}
