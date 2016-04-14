using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using AutoMapper;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Npgsql;

namespace Unloader
{
    public class UnloadInstanse : IInstanse
    {
        public WindsorContainer container;
        protected AppConfig configurations;
        public UnloadInstanse()
        {
            container = new WindsorContainer();
            unloadList = new Stack<int>();
            configurations = ConfigurationReader.GetConfig();
            Register();
        }
        /// <summary>
        /// Стек индексов выполняемых потоков
        /// </summary>
        public Stack<int> unloadList { get; set; }
        public event ProgressEventHandler mainProgress;
        /// <summary>
        /// Событие остановки потока
        /// </summary>
        public static event StopEventHandler stopThread;
        /// <summary>
        /// Событие отправки сообщения
        /// </summary>
        public static event SendEventHandler sendMessage;
        protected sealed class SingletonCreator
        {
            private static readonly UnloadInstanse instance = new UnloadInstanse();
            public static UnloadInstanse Instance { get { return instance; } }
        }
        /// <summary>
        /// Экземпляр объекта Проверщика форматов
        /// </summary>
        public static UnloadInstanse Instance
        {
            get { return SingletonCreator.Instance; }
        }

        protected void Register()
        {
            var type = typeof(Unload);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(x => type.IsAssignableFrom(x) && !x.IsAbstract).ToList();
            types.ForEach(x =>
                container.Register(
                    Component
                        .For<Unload>()
                        .ImplementedBy(x)
                        .Named(x.GetCustomAttributes(typeof(AssembleAttribute), true)
                            .Cast<AssembleAttribute>()
                            .Single().RegistrationName)
                        .LifestyleTransient())
            );
        }

        protected Unload Resolve(string name)
        {
            return container.Resolve<Unload>(name);
        }

        protected List<Unload> ResolveAll()
        {
            return (container.ResolveAll(typeof(Unload))).OfType<Unload>().ToList();
        }

        public List<AssembleAttribute> GetAllFormats()
        {
            var list = ResolveAll();
            return (from x in list
                    let assembleAttribute = x.GetType().GetCustomAttribute(typeof(AssembleAttribute), true) as AssembleAttribute
                    where assembleAttribute != null
                    select new AssembleAttribute
                        {
                            RegistrationName = assembleAttribute.RegistrationName,
                            FormatName = assembleAttribute.FormatName + ", Версия:" + assembleAttribute.Version,
                            Version = assembleAttribute.Version
                        }).ToList();
        }

        public int Run(Request request)
        {
            Func<int, Request, Returns> method = StartProcess;
            unloadList.Push(unloadList.Count + 1);
            RunAsynchronously(method, unloadList.Count, request);
            return unloadList.Count;
        }

        /// <summary>
        /// Функция добавления события по остановке потока 
        /// </summary>
        /// <param name="unloadID">Идентификатор</param>
        /// <param name="is_alive">Показатель того запущен или остановлен поток</param>
        protected void StopThread(int unloadID, bool is_alive)
        {
            if (stopThread != null)
                stopThread(this, new StopArgs(unloadID, is_alive));
        }
        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="unloadID">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        public static void SendMessage(int unloadID, string Message, Statuses result)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, unloadID));
        }

        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="unloadID">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        /// <param name="link">Ссылка на файл</param>
        public static void SendMessage(int unloadID, string Message, Statuses result, string link)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, unloadID, link));
        }
        protected void onProgress(object sender, ProgressArgs arg)
        {
            SetProgress(arg.progress, arg.unloadID);
        }

        protected virtual void SetProgress(decimal progress, int unloadID)
        {
            if (mainProgress != null)
                mainProgress(this, new ProgressArgs(progress, unloadID));
        }
        /// <summary>
        /// Запуск/остановка потока
        /// </summary>
        /// <param name="unloadID">Идентификатор</param>
        /// <param name="is_alive">Показаль того запущен или остановлен поток</param>
        public void StopResume(int unloadID, bool is_alive)
        {
            StopThread(unloadID, is_alive);
        }

        public List<string> GetDatabases()
        {
            return new DB().GetAllDb(configurations.HostDbParams.ConnectionString);
        }
        public List<string> GetShemas(string db)
        {
            return new DB().GetAllSchema(configurations.HostDbParams.ConnectionString, db);
        }

        public Points GetPoints(string db)
        {
            return new DB().LoadPoints(configurations.HostDbParams.ConnectionString, db);
        }

        public string GetConnectionString(string db)
        {
            return new DB().CreateConnectionString(configurations.HostDbParams.ConnectionString, db);
        }

        protected Returns StartProcess(int unloadID, Request request)
        {
            SendMessage(unloadID, "", Statuses.Execute);
            var obj = Resolve(request.RegistrationName);
            if (obj == null)
            {
                SendMessage(unloadID, "Не определен формат", Statuses.Error);
                return new Returns();
            }
            obj = Mapper.DynamicMap(request, typeof(Request), obj.GetType()) as Unload;
            obj.connectionString = GetConnectionString(request.db);
            obj.events.WaitOne();
            obj.Progress += onProgress;
            var param = new object();
            var ret = obj.Run(ref param);
            obj.events.WaitOne();
            if (ret.result)
            {
                SendMessage(unloadID, ret.resultMessage, Statuses.Finished, param.ToString());
            }
            else SendMessage(unloadID, "Не правильный формат выгрузки.Ошибка:" + ret.resultMessage, Statuses.Error);
            return ret;
        }

        /// <summary>
        /// Асинхронный запуск потоков
        /// </summary>
        /// <param name="method">Исполняемый Метод</param>
        /// <param name="unloadID">Идентификатор формата</param>
        /// <param name="request"></param>
        protected static void RunAsynchronously(Func<int, Request, Returns> method, int unloadID, Request request)
        {
            ThreadPool.QueueUserWorkItem(_ => method(unloadID, request));
        }

    }
}
