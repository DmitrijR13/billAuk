using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Bars.B4.Utils;

namespace Unloader
{
    public static class ConfigurationReader
    {

        /// <summary>Получить настройки приложения</summary>
        /// <returns>Возвращает экземпляр <see cref="AppConfig"/></returns>
        public static AppConfig GetConfig()
        {
            string userName, userPassword, value = GetDecryptValue("W2");
            Utils.UserLogin(value, out userName, out userPassword);

            var _config = new AppConfig
            {
                UserName = userName,
                UserPassword = userPassword,
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
                }
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

    /// <summary>
    /// Методы шифрования и расшифровки текста
    /// </summary>
    public static class Encryptor
    {
        private static string Pwd = "bu@ovgEwe3rf34tf^r161t11HG3c-3fc3yg/f43fgnhcniu+ghe";

        private static byte[] Slt //значение по-умолчанию
        {
            get { return Convert.FromBase64String("RT45CVdd"); }
        }

        /// <summary>
        /// Шифрование текста
        /// </summary>
        /// <param name="inputText">Текст для шифрования</param>
        /// <param name="password">Пароль шифра</param>
        /// <param name="salt">Случайная последовательность байт</param>
        public static string Encrypt(string inputText, byte[] salt)
        {
            if (salt != null)
                if (salt.Length < 1) salt = Slt;

            // Преобразуем текст в байты
            byte[] inputBytes = Encoding.Unicode.GetBytes(inputText);
            // Соединяем пароль и случайные байты
            // Это повышает защищенность шифрования
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Pwd, salt);

            using (MemoryStream ms = new MemoryStream())
            {
                // Алгоритм шифрования Rijndael
                Rijndael alg = Rijndael.Create();
                // Ключи Key и IV
                alg.Key = pdb.GetBytes(32);
                alg.IV = pdb.GetBytes(16);

                // Создаем поток для шифрования
                // на основе потока в памяти
                using (CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    // Записываем входную строку в поток
                    cs.Write(inputBytes, 0, inputBytes.Length);
                }


                // Результат - зашифрованная строка
                return Convert.ToBase64String(ms.ToArray()).Replace("+", "-").Replace("/", "_");
            }
        }

        /// <summary>
        /// Расшифровка текста
        /// </summary>
        /// <param name="inputText">Текст для расшифровки</param>
        /// <param name="password">Пароль шифра</param>
        /// <param name="salt">Случайная последовательность байт</param>
        public static string Decrypt(string inputText, byte[] salt)
        {
            if (salt != null)
                if (salt.Length < 1) salt = Slt;

            // Конвертируем строку в байты
            byte[] inputBytes = Convert.FromBase64String(inputText.Replace("-", "+").Replace("_", "/"));
            // Соединяем пароль и случайные байты
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Pwd, salt);

            using (MemoryStream ms = new MemoryStream())
            {
                // Алгоритм шифрования Rijndael
                Rijndael alg = Rijndael.Create();
                // Ключи Key и IV
                alg.Key = pdb.GetBytes(32);
                alg.IV = pdb.GetBytes(16);

                // Создаем поток для расшифровки
                // на основе потока в памяти
                using (CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    // расшифровка
                    cs.Write(inputBytes, 0, inputBytes.Length);
                }

                // Результат - расшифрованная строка
                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }
    }
}
