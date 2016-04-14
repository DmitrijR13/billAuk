using System;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Статус использования соединения
    /// </summary>
    public class DatabaseConnectionStatus
    {
        /// <summary>
        /// Поток, в котором випользовалось соединение
        /// </summary>
        public int ThreadId { get; protected internal set; }

        /// <summary>
        /// Время последней активности
        /// </summary>
        public DateTime UtcActivity { get; protected internal set; }

        /// <summary>
        /// Статус подключения
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; protected internal set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Format(
                "Статус подключения: {0}\n" +
                "Идентификатор использовавшего потока: {1}\n" +
                "Дата последней активности: {2:dd.MM.yyyy HH:mm:ss}\n",
                ConnectionStatus.GetDescription(), ThreadId, UtcActivity.ToLocalTime());
        }
    }
}
