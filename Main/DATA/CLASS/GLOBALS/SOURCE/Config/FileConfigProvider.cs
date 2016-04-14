namespace Globals.SOURCE.Config
{
    using System.Configuration;
    using System.Net;

    using Bars.KP50.Utils;

    using STCLINE.KP50.Global;

    /// <summary>Провайдер кофигураций. Получает параметры из app.config</summary>
    public class FileConfigProvider : IConfigProvider
    {
        /// <summary>Конфигурация</summary>
        private AppConfig _config;
        
        public static IConfigProvider Init()
        {
            return new FileConfigProvider();
        }

        /// <summary>Получить настройки приложения</summary>
        /// <returns>Возвращает экземпляр <see cref="AppConfig"/></returns>
        public AppConfig GetConfig()
        {
            if (_config != null)
            {
                return _config;
            }

            var ftpParams = string.IsNullOrEmpty(ConfigurationManager.AppSettings["FtpHostAddress"])
            ? new FtpParams()
            : new FtpParams
            {
                UseFtp = true,
                Address = GetDecryptValue("FtpHostAddress"),
                UserName = GetDecryptValue("FtpUserName"),
                UserPassword = GetDecryptValue("FtpUserPassword"),
                UseProxy = GetDecryptValue("FtpUseProxy").ToStr().ToUpper().Trim() == "YES"
            };

            if (ftpParams.UseFtp)
            {
                ftpParams.Credentials = new NetworkCredential(ftpParams.UserName, ftpParams.UserPassword);
            }
            
            string userName, userPassword, value = GetDecryptValue("W2");
            Utils.UserLogin(value, out userName, out userPassword);
            
            _config = new AppConfig
            {
                UserName = userName,
                UserPassword = userPassword,
                FtpParams = ftpParams,
                RemoteExecuteReport = GetDecryptValue("RemoteExecuteReport", false),
                QueueHost = GetDecryptValue("QueueHost"),
                WebDbParams = new DbParams
                {
                    ConnectionString = GetDecryptValue("W1"),
                    DbWaitingTimeout = GetDecryptValue("DbWaitingTimeout", 5)
                },
                HostDbParams = new DbParams
                {
                    ConnectionString = GetDecryptValue("W4"),
                    MainSchemaName = GetDecryptValue("W10"),
                    DbWaitingTimeout = GetDecryptValue("DbWaitingTimeout", 5)
                },
                AddressWcfHost = new WCFParamsType
                {
                    Adres = GetDecryptValue("W3"),
                    BrokerAdres = GetDecryptValue("W5"),
                },
                Directories = new Directories { FilesDir = GetDecryptValue("W6") },
                FullLogging = 
                !string.IsNullOrEmpty(ConfigurationManager.AppSettings["W11"]) 
                && GetDecryptValue("W11").ToStr().ToUpper().Trim() == "YES"
            };

            _config.MainDbParams = _config.HostDbParams;

            return _config;
        }

        /// <summary>Получить значение</summary>
        /// <param name="configName">Имя ключа</param>
        /// <param name="defaultValue">Значение по умолчанию (пустая строка)</param>
        /// <returns>Значение</returns>
        private static string GetDecryptValue(string configName, string defaultValue = "")
        {
            return GetDecryptValue<string>(configName, defaultValue);
        }

        /// <summary>Получить значение</summary>
        /// <typeparam name="T">Тип значения</typeparam>
        /// <param name="configName">Имя ключа</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        /// <returns>Значение</returns>
        private static T GetDecryptValue<T>(string configName, T defaultValue)
        {
            var value = ConfigurationManager.AppSettings[configName];
            return value == null ? defaultValue : Encryptor.Decrypt(value, null).To<T>();
        }
    }
}