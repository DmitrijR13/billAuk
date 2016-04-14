namespace Globals.SOURCE.Config
{
    using STCLINE.KP50.Global;

    /// <summary>Настройки приложения</summary>
    public class AppConfig
    {
        /// <summary>Имя системного пользователя</summary>
        public string UserName { get; set; }

        /// <summary>Пароль системного пользователя</summary>
        public string UserPassword { get; set; }

        /// <summary>Таймаут повторного входа в минутах</summary>
        public int UsersTimeout { get; set; }

        /// <summary>Отчеты выполняются удаленно (отдельным сервисов)</summary>
        public bool RemoteExecuteReport { get; set; }

        /// <summary>Адрес сервера очередей</summary>
        public string QueueHost { get; set; }

        /// <summary>Пути к отчетам, счетам и т.д.</summary>
        public Directories Directories { get; set; }

        /// <summary>Параметры wcf для хоста</summary>
        public WCFParamsType AddressWcfWeb { get; set; }

        /// <summary>Параметры wcf для web</summary>
        public WCFParamsType AddressWcfHost { get; set; }

        /// <summary>Основные настройки БД</summary>
        public DbParams MainDbParams { get; set; }

        /// <summary>Настройки БД хост</summary>
        public DbParams HostDbParams { get; set; }

        /// <summary>Настройки БД web</summary>
        public DbParams WebDbParams { get; set; }

        /// <summary>Настройки FTP</summary>
        public FtpParams FtpParams { get; set; }
        
        /// <summary> Логгировать ли все события/запросы в хосте </summary>
        public bool FullLogging { get; set; }
    }
}