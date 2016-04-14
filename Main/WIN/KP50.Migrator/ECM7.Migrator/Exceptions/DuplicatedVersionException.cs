using System.Collections.Generic;
using KP50.DataBase.Migrator.Utils;

namespace KP50.DataBase.Migrator.Exceptions
{
    /// <summary>
    /// Exception thrown when a migration number is not unique.
    /// </summary>
    public class DuplicatedVersionException : VersionException
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="versions">Дублирующиеся версии</param>
        public DuplicatedVersionException(params long[] versions)
            : base(string.Format("Migration version #{0} is duplicated", versions.ToCommaSeparatedString()), versions)
        {
        }
    }
}
