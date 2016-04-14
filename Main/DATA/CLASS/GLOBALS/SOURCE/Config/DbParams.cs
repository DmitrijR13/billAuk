namespace Globals.SOURCE.Config
{
    /// <summary>Настройки БД</summary>
    public class DbParams
    {
        /// <summary>Строка подключения</summary>
        public string ConnectionString { get; set; }

        /// <summary>Имя главной схемы/базы</summary>
        public string MainSchemaName { get; set; }

        /// <summary>Время ожидания снятия блокировки с таблиц</summary>
        public int DbWaitingTimeout { get; set; }
    }
}