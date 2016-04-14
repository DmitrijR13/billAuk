using System;
using NLog;

namespace Bars.Billing.IncrementalDataLoader.Utils
{
    /// <summary>
    /// Класс утилит для работы с логгером
    /// </summary>
    public static class LogUtils
    {
        /// <summary>
        /// Экземпляр логгера
        /// </summary>
        private static readonly Logger MyLogger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Активность лога
        /// </summary>
        private static bool _loggerEnabled = false;

        /// <summary>
        /// Запись в лог-файл
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="type">Тип сообщения</param>
        public static void WriteLog(string message, ELogType type)
        {
            //если логгер выключен, то выходим
            if(!_loggerEnabled) return;

            //если логгер включен, то пишем в лог-файл
            string messageFormat = string.Format("Bars.Billing.IncrementalDataLoader: {0}\n", message);

            switch (type)
            {
                case ELogType.Error:
                    MyLogger.Error(messageFormat);
                    break;
                case ELogType.Warn:
                    MyLogger.Warn(messageFormat);
                    break;
                case ELogType.Info:
                    MyLogger.Info(messageFormat);
                    break;
                case ELogType.Debug:
                    MyLogger.Debug(messageFormat);
                    break;
                case ELogType.Fatal:
                    MyLogger.Fatal(messageFormat);
                    break;
            }
        }

        /// <summary>
        /// Переключатель активности лога
        /// </summary>
        /// <param name="activ">Устанавливаемое состояние активности лога (true:включен, false: выключен)</param>
        public static void EnableLogger(bool activ)
        {
            _loggerEnabled = activ;
        }
    }
}
