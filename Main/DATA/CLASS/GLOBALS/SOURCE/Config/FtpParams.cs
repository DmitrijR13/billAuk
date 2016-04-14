namespace Globals.SOURCE.Config
{
    using System.Net;

    /// <summary>Настройки FTP</summary>
    public class FtpParams
    {
        /// <summary>Использовать FTP</summary>
        public bool UseFtp { get; set; }

        /// <summary>Адрес FTP</summary>
        public string Address { get; set; }

        public NetworkCredential Credentials { get; set; }

        /// <summary>Имя пользователя</summary>
        public string UserName { get; set; }

        /// <summary>Пароль пользователя</summary>
        public string UserPassword { get; set; }

        /// <summary>Использовать прокси</summary>
        public bool UseProxy { get; set; }

        /// <summary>Адрес прокси</summary>
        public string ProxyAddress { get; set; }

        /// <summary>Имя пользователя для прокси</summary>
        public string ProxyUserName { get; set; }

        /// <summary>Пароль пользователя для прокси</summary>
        public string ProxyUserPassword { get; set; }

        /// <summary>Домен для прокси</summary>
        public string ProxyDomain { get; set; }
    }
}