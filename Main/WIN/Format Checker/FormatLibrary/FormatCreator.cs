using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FormatLibrary
{
    public class FormatCreator
    {
        protected FormatCreator()
        {
            LoadAllDLL();
            formatList = new Stack<int>();
        }
        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public string programVersion { get; set; }

        protected sealed class SingletonCreator
        {
            private static readonly FormatCreator instance = new FormatCreator();
            public static FormatCreator Instance { get { return instance; } }
        }
        /// <summary>
        /// Экземпляр объекта Проверщика форматов
        /// </summary>
        public static FormatCreator Instance
        {
            get { return SingletonCreator.Instance; }
        }
        /// <summary>
        /// Событие отображение прогресса операций
        /// </summary>
        public event ProgressEventHandler mainProgress;

        public ManualResetEvent ManualResetEvent;
        /// <summary>
        /// Событие остановки потока
        /// </summary>
        public static event StopEventHandler stopThread;
        /// <summary>
        /// Событие отправки сообщения
        /// </summary>
        public static event SendEventHandler sendMessage;
        /// <summary>
        /// Список загруженных сборок
        /// </summary>
        private List<AssembleAttribute> listClasses;
        /// <summary>
        /// Стек индексов выполняемых потоков
        /// </summary>
        public Stack<int> formatList { get; set; }

        /// <summary>
        /// Функция добавления события по остановке потока 
        /// </summary>
        /// <param name="formatID">Идентификатор</param>
        /// <param name="is_alive">Показатель того запущен или остановлен поток</param>
        protected void StopThread(int formatID, bool is_alive)
        {
            if (stopThread != null)
                stopThread(this, new StopArgs(formatID, is_alive));
        }
        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="formatID">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        public static void SendMessage(int formatID, string Message, Statuses result)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, formatID));
        }

        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="formatID">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        /// <param name="link">Ссылка на файл</param>
        public static void SendMessage(int formatID, string Message, Statuses result, string link)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, formatID, link));
        }


        /// <summary>
        /// Подключение сборок
        /// </summary>
        public void LoadAllDLL()
        {
            var binFolder = AppDomain.CurrentDomain.BaseDirectory;
            listClasses = new List<AssembleAttribute>();
            var listInclude = new[] { "FORMAT" };
            var folder = new DirectoryInfo(binFolder);

            var libraries =
                folder.GetFiles("*.dll")
                    .Where(x => listInclude.Any(y => x.Name.ToUpper().StartsWith(y)))
                    .Select(x => AssemblyName.GetAssemblyName(x.FullName));

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName()).ToArray();

            foreach (var asmName in libraries.Where(asmName => loadedAssemblies.All(x => x.FullName != asmName.FullName) && libraries.Where(x => x.Name == asmName.Name).Max(x => x.Version) == asmName.Version))
            {
                Assembly.LoadFrom(asmName.CodeBase.Split('/').Last());
            }
            foreach (
                var assembly in
                    AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.ToUpper().Contains("FORMAT")))
            {
                listClasses.AddRange(
                    assembly.GetTypes()
                        .Where(x => !x.IsInterface && !x.IsAbstract && typeof(IFormat).IsAssignableFrom(x))
                        .Select(x =>
                            new AssembleAttribute
                            {
                                RegistrationName = x.FullName,
                                Version =
                                    x.GetCustomAttributes(typeof(AssembleAttribute), true)
                                        .Cast<AssembleAttribute>()
                                        .Single()
                                        .Version,
                                FormatName =
                                    x.GetCustomAttributes(typeof(AssembleAttribute), true)
                                        .Cast<AssembleAttribute>()
                                        .Single()
                                        .FormatName,
                                type = x
                            }).ToList());
            }
        }

        /// <summary>
        /// Асинхронный запуск потоков
        /// </summary>
        /// <param name="method">Исполняемый Метод</param>
        /// <param name="type">Тип формата</param>
        /// <param name="formatID">Идентификатор формата</param>
        /// <param name="FileName">Наименование файла</param>
        /// <param name="Path">Путь к файлу</param>
        protected static void RunAsynchronously(Func<Type, int, string, string, bool, Returns> method, Type type, int formatID, string FileName, string Path, bool WithEndSymbol)
        {
            ThreadPool.QueueUserWorkItem(_ => method(type, formatID, FileName, Path, WithEndSymbol));
        }

        /// <summary>
        /// Функция запуска операции
        /// </summary>
        /// <param name="type">Тип формата</param>
        /// <param name="formatID">Идентификатор</param>
        /// <param name="FileName">Имя файла</param>
        /// <param name="Path">Путь к файлу</param>
        /// <returns>Результат выполнения</returns>
        protected Returns StartProcess(Type type, int formatID, string FileName, string Path, bool WithEndSymbol)
        {
            SendMessage(formatID, "", Statuses.Execute);
            var obj = new Creator(type, formatID, FileName, Path);
            if (obj.Checker == null || obj.Loader == null || obj.ProtocolCreator == null)
            {
                SendMessage(formatID, "Не все зависимости определены", Statuses.Error);
                return new Returns();
            }
            obj.events.WaitOne();
            obj.Checker.Progress += onProgress;
            obj.ProtocolCreator.Progress += onProgress;
            obj.Loader.Progress += onProgress;
            obj.Checker.programVersion = programVersion;
            obj.Loader.WithEndSymbol = WithEndSymbol;
            var param = new object();
            var ret = obj.Loader.LoadData(ref param);
            GC.Collect();
            try
            {
                obj.Checker.err = obj.Loader.err;
                obj.events.WaitOne();
                if (ret.result)
                {
                    ret = obj.Checker.CheckData(ref param);
                    GC.Collect();
                    obj.events.WaitOne();
                    if (!ret.result)
                    {
                        SendMessage(formatID, "Не правильный формат файла загрузки.Ошибка:" + ret.resultMessage,
                            Statuses.Error);
                    }
                    ret = obj.ProtocolCreator.CreateProtocol(ref param);
                    GC.Collect();
                    obj.events.WaitOne();
                    if (!ret.result)
                    {
                        SendMessage(formatID, "Не правильный формат файла загрузки.Ошибка:" + ret.resultMessage,
                            Statuses.Error);
                    }
                    else
                    {
                        SendMessage(formatID, ret.resultMessage, Statuses.Finished, param.ToString());
                    }
                }
                else
                    SendMessage(formatID, "Не правильный формат файла загрузки.Ошибка:" + ret.resultMessage,
                        Statuses.Error);
            }
            catch (Exception ex)
            {
                SendMessage(formatID, "Не правильный формат файла загрузки.Ошибка:" + ex.Message,
                        Statuses.Error);
            }
            return ret;
        }

        /// <summary>
        /// Постановка в очередь очередной загрузки
        /// </summary>
        /// <param name="attribute">Атрибуты формата</param>
        /// <returns>UnloadID-идентификатор</returns>
        public int Start(Request attribute)
        {
            programVersion = attribute.programVersion;
            Func<Type, int, string, string, bool, Returns> method = StartProcess;
            formatList.Push(formatList.Count + 1);
            RunAsynchronously(method, attribute.type, formatList.Count, attribute.FileName, attribute.Path, attribute.WithEndSymbol);
            return formatList.Count;
        }

        protected void onProgress(object sender, ProgressArgs arg)
        {
            SetProgress(arg.progress, arg.formatID);
        }

        protected virtual void SetProgress(decimal progress, int formatID)
        {
            if (mainProgress != null)
                mainProgress(this, new ProgressArgs(progress, formatID));
        }

        /// <summary>
        /// Запуск/остановка потока
        /// </summary>
        /// <param name="formatID">Идентификатор</param>
        /// <param name="is_alive">Показаль того запущен или остановлен поток</param>
        public void StopResume(int formatID, bool is_alive)
        {
            StopThread(formatID, is_alive);
        }

        /// <summary>
        /// Получения списка Наименований,Версии подключенных форматов
        /// </summary>
        /// <returns>Список форматов</returns>
        public List<AssembleAttribute> GetAllFormattedNames()
        {
            return listClasses.Any() ? listClasses.Select(x => new AssembleAttribute
            {
                Version = x.Version,
                FormatName = x.FormatName + ", версия:" + x.Version,
                type = x.type,
                RegistrationName = x.RegistrationName,
            }).Distinct().OrderBy(x => x.FormatName).ThenBy(x => x.Version).ToList() : null;
        }

        public void CreateMetaData(Type FormatType)
        {
            new Thread(new FormatMetaData(FormatType).Run).Start();
        }
    }
}
