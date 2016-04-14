using System.ComponentModel;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Тип задачи к обработке
    /// </summary>
    public enum TaskType : int
    {
        /// <summary>
        /// Тестовая задача
        /// </summary>
        [Description("Тестовая задача")]
        SampleTask = 1,
    }
}
