using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using FormatLibrary.Annotations;

namespace FormatLibrary
{

    /// <summary>
    /// Возвращаемый тип
    /// </summary>
    public class Returns
    {
        public Returns(bool result = true, string resultMessage = "")
        {
            this.result = result;
            this.resultMessage = resultMessage;
        }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string resultMessage { get; set; }
        /// <summary>
        /// Рузультат
        /// </summary>
        public bool result { get; set; }
    }

    /// <summary>
    /// Общий интерфейс для класса загрузки
    /// </summary>
    public interface IFormat
    {
    }
    /// <summary>
    /// Класс параметров события обновления Прогресса
    /// </summary>
    public class ProgressArgs : EventArgs
    {

        public ProgressArgs(decimal progress, int formatID)
        {
            this.progress += progress;
            this.formatID = formatID;
            if (this.progress > 1) this.progress = 1;
        }

        public ProgressArgs(decimal progress, int formatID, string link)
        {
            this.progress += progress;
            this.formatID = formatID;
            this.link = link;
            if (this.progress > 1) this.progress = 1;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; private set; }
        /// <summary>
        /// Текущий прогресс
        /// </summary>
        public decimal progress { get; private set; }

        public string link { get; set; }
    }
    /// <summary>
    /// Класс параметров события остановки потока
    /// </summary>
    public class StopArgs : EventArgs
    {
        public StopArgs(int formatID, bool is_alive)
        {
            this.formatID = formatID;
            this.is_alive = is_alive;
        }
        public bool is_alive { get; set; }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; private set; }
    }
    /// <summary>
    /// Класс события отправки сообщения клиентской программе
    /// </summary>
    public class SendArgs : EventArgs
    {

        public SendArgs(string Message, Statuses result, int formatID)
        {
            this.Message = Message;
            this.formatID = formatID;
            this.result = result;
        }
        public SendArgs(string Message, Statuses result, int formatID, string link)
        {
            this.Message = Message;
            this.formatID = formatID;
            this.result = result;
            this.link = link;
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; private set; }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// Результат
        /// </summary>
        public Statuses result { get; set; }
        public string link { get; set; }
    }

    public delegate void ProgressEventHandler(object sender, ProgressArgs e);
    public delegate void StopEventHandler(object sender, StopArgs e);
    public delegate void SendEventHandler(object sender, SendArgs e);
    /// <summary>
    /// Класс создания формата 
    /// </summary>
    public class Creator
    {
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; set; }
        /// <summary>
        /// Экземпляр класса Проверки данных
        /// </summary>
        public IFormatChecker Checker { get; private set; }
        /// <summary>
        /// Экземпляр класса Загрузки данных
        /// </summary>
        public IFormatLoader Loader { get; private set; }
        /// <summary>
        /// Экземпляр класса Формирования протокола
        /// </summary>
        public IFormatProtocolCreator ProtocolCreator { get; private set; }
        /// <summary>
        /// Текущий поток
        /// </summary>
        private Thread thread { get; set; }

        public ManualResetEvent events = new ManualResetEvent(true);

        /// <summary>
        /// Конструктор, инициализирубщий структуру 
        /// </summary>
        /// <param name="formType"></param>
        /// <param name="formatID"></param>
        /// <param name="FileName"></param>
        /// <param name="Path"></param>
        public Creator(Type formType, int formatID, string FileName, string Path)
        {
            this.formatID = formatID;
            thread = Thread.CurrentThread;
            thread.IsBackground = true;
            FormatCreator.stopThread += onStop;
            events.Set();
            var list = (formType).GetNestedTypes(BindingFlags.Public | BindingFlags.Instance).ToList();
            list.ForEach(x =>
            {
                if (typeof(IFormatChecker).IsAssignableFrom(x))
                {
                    Checker = (IFormatChecker)Create<IFormatChecker>(x);
                    Checker.formatID = formatID;
                    Checker.Path = Path;
                    Checker.FileName = FileName;

                }
                if (typeof(IFormatLoader).IsAssignableFrom(x))
                {
                    Loader = (IFormatLoader)Create<IFormatLoader>(x);
                    Loader.formatID = formatID;
                    Loader.Path = Path;
                    Loader.FileName = FileName;
                }
                if (typeof(IFormatProtocolCreator).IsAssignableFrom(x))
                {
                    ProtocolCreator = (IFormatProtocolCreator)Create<IFormatProtocolCreator>(x);
                    ProtocolCreator.formatID = formatID;
                    ProtocolCreator.Path = Path;
                    ProtocolCreator.FileName = FileName;
                }
            });
        }

        public Creator(int formatID)
        {
            this.formatID = formatID;
            thread = Thread.CurrentThread;
            FormatCreator.stopThread += onStop;
        }


        /// <summary>
        /// Обработчик события остановки потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void onStop(object sender, StopArgs args)
        {
            if (args.formatID != formatID) return;
            if (args.is_alive)
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Stopped);
                events.Reset();
            }
            else
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Execute);
                events.Set();
            }
        }

        /// <summary>
        /// Фунуция создания экземпляра объекта 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        protected IFormatBase Create<T>(Type type) where T : IFormatBase
        {
            return (T)Activator.CreateInstance(type);
        }
    }
    /// <summary>
    /// Базовый интерфейс для всех форматов
    /// </summary>
    public interface IFormatBase
    {
        event ProgressEventHandler Progress;
        Returns Start(ref object dt);
        int formatID { get; set; }
        string programVersion { get; set; }
    }

    /// <summary>
    /// Абстрактный класс Проверки данных
    /// </summary>
    public abstract class IFormatChecker : Instrumentary, IFormatBase
    {
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; set; }
        /// <summary>
        /// Текущий поток
        /// </summary>
        private Thread thread { [UsedImplicitly] get; set; }
        /// <summary>
        /// Функция проверки данных
        /// </summary>
        /// <param name="dt">Параметр передаваемый из/в класс</param>
        /// <returns></returns>
        public abstract Returns CheckData(ref object dt);
        /// <summary>
        /// Запуск операции Проверки данных
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public Returns Start(ref object dt)
        {
            thread = Thread.CurrentThread;
            FormatCreator.stopThread += onStop;
            return CheckData(ref dt);
        }

        public abstract Dictionary<string, string> GetTemplatesHead();
        public abstract Dictionary<string, List<Template>> FillTemplates();
        /// <summary>
        /// Событие отображающее текущий прогресс
        /// </summary>
        public event ProgressEventHandler Progress;
        /// <summary>
        /// Запуск события отображения прогресса
        /// </summary>
        /// <param name="progress"></param>
        protected virtual void SetProgress(decimal progress)
        {
            if (Progress != null)
                Progress(this, new ProgressArgs(progress, formatID));
        }
        public ManualResetEvent events = new ManualResetEvent(true);
        /// <summary>
        /// Обработчик события остановки потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void onStop(object sender, StopArgs args)
        {
            if (args.formatID != formatID) return;
            if (args.is_alive)
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Stopped);
                events.Reset();
            }
            else
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Execute);
                events.Set();
            }
        }
        public List<string> err = new List<string>();
        public string programVersion { get; set; }
    }

    public abstract class IFormatLoader : Instrumentary, IFormatBase
    {
        public bool WithEndSymbol { get; set; }

        /// <summary>
        /// Функция загрузки
        /// </summary>
        /// <param name="dt">Передаваемый объект</param>
        /// <returns>Возвращаемый результат</returns>
        public virtual Returns LoadData(ref object dt)
        {
            Returns ret;
            SetProgress(0.05m);
            FileName = GetFilesIfWorkWithArchive();
            var list = new Dictionary<string, List<string[]>>();
            var strings = GetAllFileRows(Path + "\\" + FileName, out ret);
            if (!ret.result)
            {
                return ret;
            }
            var curList = new List<string[]>();
            var currentSection = 1;
            try
            {
                var i = 0;
                var ii=0;
                string val = null;
                strings.ToList().ForEach(str =>
                {
                    var section = 0;
                    try
                    {
                        val = str;
                        i++;
                        if (str.Trim().Length != 0)
                        {
                            var vals = str.Split(new[] { '|' }, StringSplitOptions.None);
                            section = Convert.ToInt32(vals[0].Trim());
                            if (section != currentSection)
                            {
                                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
                                curList = new List<string[]>();
                                currentSection = section;
                            }
                            if (section < 6)
                                ii++;
                            curList.Add(WithEndSymbol ? vals.Take(vals.Length - 1).ToArray() : vals.ToArray());
                        }
                    }
                    catch (Exception ex)
                    {
                        curList.Add(new[] { section.ToString(), val });
                        err.Add("Ошибка,неверный формат строки, строка " + i);
                        err.Add("Ошибка:" + ex.Message);
                        err.Add("Значение:" + val);
                    }

                });
                Template.sectionLength = ii;
                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            dt = list;
            //dt служит в качестве передаваемого объекта, который содержит список, элементы которого загружены из файла
            SetProgress(0.1m);
            return new Returns();
        }
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; set; }
        /// <summary>
        /// Текущий поток
        /// </summary>
        private Thread thread { [UsedImplicitly] get; set; }

        /// <summary>
        /// Запуск операции Загрузки данных
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public Returns Start(ref object dt)
        {
            thread = Thread.CurrentThread;
            FormatCreator.stopThread += onStop;
            return LoadData(ref dt);
        }
        public ManualResetEvent events = new ManualResetEvent(true);
        /// <summary>
        /// Обработчик события остановки потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void onStop(object sender, StopArgs args)
        {
            if (args.formatID != formatID) return;
            if (args.is_alive)
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Stopped);
                events.Reset();
            }
            else
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Execute);
                events.Set();
            }
        }

        /// <summary>
        /// Событие отображающее текущий прогресс
        /// </summary>
        public event ProgressEventHandler Progress;
        /// <summary>
        /// Запуск события отображения прогресса
        /// </summary>
        /// <param name="progress"></param>
        protected virtual void SetProgress(decimal progress)
        {
            if (Progress != null)
                Progress(this, new ProgressArgs(progress, formatID));
        }

        public string programVersion { get; set; }
        public List<string> err = new List<string>();
    }

    public abstract class IFormatProtocolCreator : Instrumentary, IFormatBase
    {
        /// <summary>
        /// ID загрузки
        /// </summary>
        public int formatID { get; set; }
        /// <summary>
        /// Текущий поток
        /// </summary>
        private Thread thread { [UsedImplicitly] get; set; }
        /// <summary>
        /// Функция формирования протокола
        /// </summary>
        /// <param name="dt">Параметр передаваемый из/в класс</param>
        /// <returns></returns>
        public abstract Returns CreateProtocol(ref object dt);
        /// <summary>
        /// Запуск операции Формирования протокола
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public Returns Start(ref object dt)
        {
            thread = Thread.CurrentThread;
            FormatCreator.stopThread += onStop;
            return CreateProtocol(ref dt);
        }
        public ManualResetEvent events = new ManualResetEvent(true);
        /// <summary>
        /// Обработчик события остановки потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void onStop(object sender, StopArgs args)
        {
            if (args.formatID != formatID) return;
            if (args.is_alive)
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Stopped);
                events.Reset();
            }
            else
            {
                FormatCreator.SendMessage(formatID, "", Statuses.Execute);
                events.Set();
            }
        }

        /// <summary>
        /// Событие отображающее текущий прогресс
        /// </summary>
        public event ProgressEventHandler Progress;
        /// <summary>
        /// Запуск события отображения прогресса
        /// </summary>
        /// <param name="progress"></param>
        protected virtual void SetProgress(decimal progress)
        {
            if (Progress != null)
                Progress(this, new ProgressArgs(progress, formatID));
        }

        public string programVersion { get; set; }
        public List<string> err = new List<string>();
    }
}
