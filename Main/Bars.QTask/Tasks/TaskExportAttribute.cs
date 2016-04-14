using System;
using System.ComponentModel.Composition;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Аттрибут экспорта реализации задачи
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TaskExportAttribute : ExportAttribute, IExecutableTaskMetadata
    {
        /// <summary>
        /// Тип зпдпчи
        /// </summary>
        public TaskType TaskType { get; private set; }

        /// <summary>
        /// Имя задачи
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Аттрибут экспорта реализации задачи
        /// </summary>
        /// <param name="type">Тип зпдпчи</param>
        /// <param name="name">Имя задачи</param>
        public TaskExportAttribute(TaskType type) :
            base(typeof(IExecutableTask))
        {
            this.TaskType = type;
            Description = type.GetDescription();
        }
    }
}
