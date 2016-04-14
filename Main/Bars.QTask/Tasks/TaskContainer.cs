using Bars.QTask.Extensions;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Контейнер экспортированных типов
    /// </summary>
    internal class TaskContainer : IDisposable
    {
        /// <summary>
        /// MEF-Контейнер
        /// </summary>
        private readonly CompositionContainer _container = null;

        /// <summary>
        /// Экпортированные типы и метаданные
        /// </summary>
        private readonly IEnumerable<KeyValuePair<Type, IExecutableTaskMetadata>> _exportedTypes = null;

        /// <summary>
        /// Идентификаторы загруженных типов
        /// </summary>
        protected internal int[] SupportedTasks { get; private set; }

        /// <summary>
        /// Создает экземпляр запрошенного типа
        /// </summary>
        /// <param name="type">Тип задачи</param>
        protected internal ExecutableTask CreateInstance(TaskType type)
        {
            var objectMetadata = _exportedTypes.SingleOrDefault(x => x.Value.TaskType == type);
            if (objectMetadata.Value != null)
            {
                var objectInstance = (ExecutableTask)objectMetadata.Key.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance, null,
                    Type.EmptyTypes, new ParameterModifier[] { }).Invoke(null);
                objectInstance.TaskType = objectMetadata.Value.TaskType;
                objectInstance.Description = objectMetadata.Value.Description;
                return objectInstance;
            }
            return null;
        }

        /// <summary>
        /// Контейнер экспортированных типов
        /// </summary>
        internal TaskContainer()
        {
            var assemblys = AppDomain.CurrentDomain.GetAssemblies();
            var catalog = new AggregateCatalog(assemblys.Select(x => new AssemblyCatalog(x)));
            _container = new CompositionContainer(catalog);
            var types = new List<KeyValuePair<Type, IExecutableTaskMetadata>>(_container.GetExportTypesWithMetadata<IExecutableTask, IExecutableTaskMetadata>());
            var duplicated = new List<KeyValuePair<Type, IExecutableTaskMetadata>>(
                types.GroupBy(x => x.Value.TaskType).
                    Where(x => x.Count() > 1).
                    SelectMany(group => group));
            if (duplicated.Any())
            {
                MonitorLog.WriteLog("Обнаружены дублирующиеся реализации задач.", MonitorLog.typelog.Warn, true);
                foreach (var type in duplicated)
                {
                    var message = string.Format("Type: {0}\nContract: {1}\nDocument name: {2}",
                        type.Key, type.Value.TaskType, type.Value.Description);
                    MonitorLog.WriteLog(message, MonitorLog.typelog.Warn, true);
                    types.Remove(type);
                }
            }
            if (!types.Any()) throw new EntryPointNotFoundException("Не найдено ни одной реализации.");
            _exportedTypes = types.AsReadOnly();
            SupportedTasks = _exportedTypes.Select(x => (int)x.Value.TaskType).ToArray();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
