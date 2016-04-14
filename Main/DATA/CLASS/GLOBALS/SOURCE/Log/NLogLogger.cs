namespace Globals.SOURCE
{
    using System;

    using NLog;
    using NLog.Config;

    public class NLogLogger : ILog
    {
        private readonly Logger _logger;

        /// <summary>Конструктор</summary>
        /// <param name="logger">Объект лога</param>
        public NLogLogger(Logger logger)
        {
            _logger = logger;
        }

        /// <summary>Создает экземпляр NLog-логгера</summary>
        /// <param name="name">Имя логгера</param>
        /// <param name="configurationFileName">Путь к конфигурационному файлу</param>
        /// <returns>Реализация контракта ILog</returns>
        public static ILog Create(string name, string configurationFileName)
        {
            LogManager.Configuration = new XmlLoggingConfiguration(configurationFileName);
            return new NLogLogger(LogManager.GetLogger(name));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "критическая ошибка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "критическая ошибка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="e">Объект исключения</param>
        public void Fatal(string message, Exception e)
        {
            _logger.FatalException(message, e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "критическая ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void FatalFormat(string format, params object[] args)
        {
            _logger.Fatal(string.Format(format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "критическая ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void FatalFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.Fatal(string.Format(formatProvider, format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "критическая ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void FatalFormat(Exception e, string format, params object[] args)
        {
            _logger.FatalException(string.Format(format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "критическая ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void FatalFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.FatalException(string.Format(formatProvider, format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "ошибка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void Error(string message)
        {
            _logger.Error(message);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "ошибка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="e">Объект исключения</param>
        public void Error(string message, Exception e)
        {
            _logger.ErrorException(message, e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void ErrorFormat(string format, params object[] args)
        {
            _logger.Error(string.Format(format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.Error(string.Format(formatProvider, format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void ErrorFormat(Exception e, string format, params object[] args)
        {
            _logger.ErrorException(string.Format(format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "ошибка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void ErrorFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.ErrorException(string.Format(formatProvider, format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "предупреждение"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void Warning(string message)
        {
            _logger.Warn(message);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "предупреждение"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="e">Объект исключения</param>
        public void Warning(string message, Exception e)
        {
            _logger.WarnException(message, e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "предупреждение" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void WarningFormat(string format, params object[] args)
        {
            _logger.Warn(string.Format(format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "предупреждение" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void WarningFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.Warn(string.Format(formatProvider, format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "предупреждение" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void WarningFormat(Exception e, string format, params object[] args)
        {
            _logger.WarnException(string.Format(format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "предупреждение" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void WarningFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.WarnException(string.Format(formatProvider, format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "информация"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void Info(string message)
        {
            _logger.Info(message);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "информация"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="e">Объект исключения</param>
        public void Info(string message, Exception e)
        {
            _logger.InfoException(message, e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "информация" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void InfoFormat(string format, params object[] args)
        {
            _logger.Info(string.Format(format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "информация" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void InfoFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.Info(string.Format(formatProvider, format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "информация" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void InfoFormat(Exception e, string format, params object[] args)
        {
            _logger.InfoException(string.Format(format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "информация" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void InfoFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.InfoException(string.Format(formatProvider, format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "отладка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "отладка"
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="e">Объект исключения</param>
        public void Debug(string message, Exception e)
        {
            _logger.DebugException(message, e);
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "отладка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void DebugFormat(string format, params object[] args)
        {
            _logger.Debug(string.Format(format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение с типом события "отладка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void DebugFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.Debug(string.Format(formatProvider, format, args));
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "отладка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void DebugFormat(Exception e, string format, params object[] args)
        {
            _logger.DebugException(string.Format(format, args), e);
        }

        /// <summary>
        /// Добавляет в лог сообщение и ошибку с типом события "отладка" 
        /// с заранее определенным форматом
        /// </summary>
        /// <param name="e">Объект исключения</param>
        /// <param name="formatProvider">Контроллер формата</param>
        /// <param name="format">Формат сообщения</param>
        /// <param name="args">Необходимые аргументы для сообщения</param>
        public void DebugFormat(Exception e, IFormatProvider formatProvider, string format, params object[] args)
        {
            _logger.DebugException(string.Format(formatProvider, format, args), e);
        }
    }
}