using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Группа прав доступа
    /// </summary>
    public class AccessibleGroup
    {
        /// <summary>
        /// Имя группы
        /// </summary>
        public string Name { get; protected internal set; }

        /// <summary>
        /// Объекты группы
        /// </summary>
        public AccessibleObjectsCollection Objects;
    }
}
