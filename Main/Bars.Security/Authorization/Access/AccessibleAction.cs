using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Доступные действия над объектом
    /// </summary>
    [Flags]
    public enum AccessibleAction : byte
    {
        /// <summary>
        /// Нет доступных действий
        /// </summary>
        None = 0,

        /// <summary>
        /// Чтение
        /// </summary>
        Read = 1,

        /// <summary>
        /// Запись
        /// </summary>
        Write = 1 << 1,

        /// <summary>
        /// Исполнение
        /// </summary>
        Execute = 1 << 2
    }
}
