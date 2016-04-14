using System.ComponentModel;
namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Действие над процессом
    /// </summary>
    public enum TaskAction : int
    {
        /// <summary>
        /// Игнорировать параметр
        /// </summary>
        [Description("Оставить без изменений")]
        None = 0,

        /// <summary>
        /// Приостановить
        /// </summary>
        [Description("Приостановить")]
        Suspend = 200,

        /// <summary>
        /// Возобновить
        /// </summary>
        [Description("Возобновить")]
        Resume = 205,

        /// <summary>
        /// Отменить
        /// </summary>
        [Description("Отменить")]
        Cancel = 210,
    }

    /// <summary>
    /// Статусы выполнения задачи
    /// </summary>
    public enum TaskState : int
    {
        /// <summary>
        /// Во время обработки возникло системное исключение
        /// </summary>
        [Description("Во время обработки возникло системное исключение")]
        Aborted = 320,

        /// <summary>
        /// Отменено по требованию пользователя
        /// </summary>
        [Description("Отменено по требованию пользователя")]
        Cancelled = 310,

        /// <summary>
        /// Выполнено
        /// </summary>
        [Description("Выполнено")]
        Executed = 300,

        /// <summary>
        /// Запрошена отмена
        /// </summary>
        [Description("Запрошена отмена")]
        CancellationRequired = 210,

        /// <summary>
        /// Запрошено возобновление
        /// </summary>
        [Description("Запрошено возобновление")]
        ResumeRequired = 205,

        /// <summary>
        /// Запрошена приостановка обработки
        /// </summary>
        [Description("Запрошена приостановка обработки")]
        SuspendRequired = 200,

        /// <summary>
        /// Ожидает свободный поток для возобновления
        /// </summary>
        [Description("Ожидает свободный поток для возобновления")]
        WaitingForFreeThread = 115,

        /// <summary>
        /// Приостановлено по требованию пользователя
        /// </summary>
        [Description("Приостановлено по требованию пользователя")]
        Suspended = 110,

        /// <summary>
        /// Выполняется
        /// </summary>
        [Description("Выполняется")]
        Executing = 105,

        /// <summary>
        /// Поставлено в очередь на обработку
        /// </summary>
        [Description("Поставлено в очередь на обработку")]
        Queued = 100,

        /// <summary>
        /// Новое задание
        /// </summary>
        [Description("Новое задание")]
        New = 400,
    }
}
