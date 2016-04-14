using System;
using System.ComponentModel;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Статус подключения
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Свободно
        /// </summary>
        [Description("Свободно")]
        Free = 0,

        /// <summary>
        /// Используется
        /// </summary>
        [Description("Используется")]
        InUse = 1
    }

    /// <summary>
    /// Represents an open connection to a data source, and is implemented by .NET
    /// Framework data providers that access relational databases.
    /// </summary>
    public partial class DatabaseConnection
    {
        /// <summary>
        /// Статус последнего использования подключения
        /// </summary>
        public DatabaseConnectionStatus Status { get; protected internal set; }

        /// <summary>
        /// Время в которое подключение может быть освобождено
        /// </summary>
        public DateTime? UtcEraseTime { get; protected internal set; }
    }
}
