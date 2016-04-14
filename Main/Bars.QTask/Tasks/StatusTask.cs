using System;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Объект статуса задачи.
    /// Не содержит реализации
    /// </summary>
    internal class StatusTask : ExecutableTask
    {
        /// <summary>
        /// Точка входа
        /// </summary>
        /// <param name="container">Параметр</param>
        /// <param name="token">Токен отмены задачи</param>
        public override void Execute(object container, ThreadValidationToken token)
        {
            throw new NotSupportedException("Данное действие не поддерживается для этого объекта.");
        }
    }
}
