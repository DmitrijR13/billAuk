using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace Loader
{
    public static class Instanse
    {
        public static string connString { get; set; }
        public static string psqlPath { get; set; }
        public static string database { get; set; }
        public static string port { get; set; }
        public static string server { get; set; }
        public static string password { get; set; }
        public static string user { get; set; }
        /// <summary>
        /// Событие остановки потока
        /// </summary>
        public static event StopEventHandler stopThread;
        /// <summary>
        /// Событие отправки сообщения
        /// </summary>
        public static event SendEventHandler sendMessage;
        /// <summary>
        /// Событие отображение прогресса операций
        /// </summary>
        public static event ProgressEventHandler mainProgress;
        public static List<AssembleAttribute> GetFormats()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => (typeof(Format)).IsAssignableFrom(p) && !p.IsAbstract).Select(x =>
                new AssembleAttribute
                {
                    FormatName = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().FormatName + ", Версия: " +
                    x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().Version,
                    Version = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().Version,
                    type = x,
                    RegistrationName = x.FullName
                }).ToList();
        }

        public static List<AssembleAtr> GetFormatsForWeb()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => (typeof(Format)).IsAssignableFrom(p) && !p.IsAbstract).Select(x =>
                new AssembleAtr
                {
                    FormatName = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().FormatName + ", Версия: " +
                    x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().Version,
                    Version = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().Version,
                    type = x,
                    RegistrationName = x.FullName
                }).ToList();
        }

        public static ListQueue<Format> Queue = new ListQueue<Format>();
        public static void Run()
        {
            var thread = new Thread(Creator);
            var configs = ConfigurateReader.GetConfig();
            connString = configs[0];
            psqlPath = configs[1];
            server = configs[2];
            database = configs[3];
            port = configs[4];
            user = configs[5];
            password = configs[6];
            thread.Start();
        }

        public static void Creator()
        {
            while (true)
            {
                try
                {
                    if (Queue.Any())
                    {
                        if (Queue.Peek().state == Statuses.InQueue)
                            Queue.Peek().Initialize();
                        if (Queue.Peek().state == Statuses.Finished || Queue.Peek().state == Statuses.Error)
                            Queue.Dequeue();
                    }
                    Thread.Sleep(500);
                }
                catch
                {
                }
            }
        }

        public static void Add(Request req)
        {
            var format = Mapper.DynamicMap(req, typeof(Request), req.type) as Format;
            format.Progress += onProgress;
            format.connectionString = connString;
            Add(format);
        }

        static void Add(Format format)
        {
            Queue.Enqueue(format);
        }

        public static void Delete(int nzp_load)
        {
            var elem = Queue.FirstOrDefault(x => x.nzp_load == nzp_load);
            DeleteInDB(nzp_load);
            if (elem != null)
                Queue.Remove(elem);
        }

        /// <summary>d
        /// Функция добавления события по остановке потока 
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="is_alive">Показатель того запущен или остановлен поток</param>
        static void StopThread(int nzp_load, bool is_alive)
        {
            if (stopThread != null)
                stopThread(null, new StopArgs(nzp_load, is_alive));
        }
        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        public static void SendMessage(int nzp_load, string Message, Statuses result)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, nzp_load));
        }

        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="Message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        /// <param name="link">Ссылка на файл</param>
        public static void SendMessage(int nzp_load, string Message, Statuses result, string link)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(Message, result, nzp_load, link));
        }

        static void onProgress(object sender, ProgressArgs arg)
        {
            SetProgress(arg.progress, arg.nzp_load);
        }

        static void SetProgress(decimal progress, int nzp_load)
        {
            if (mainProgress != null)
                mainProgress(null, new ProgressArgs(progress, nzp_load));
        }

        /// <summary>
        /// Запуск/остановка потока
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="is_alive">Показаль того запущен или остановлен поток</param>
        public static void StopResume(int nzp_load, bool is_alive)
        {
            StopThread(nzp_load, is_alive);
        }

        public static List<Request> GetImportValues()
        {
            return new Db(connString).GetImportValues();
        }

        public static List<Request> UpdateImportValue(List<Request> reqList)
        {
            return new Db(connString).UpdateImportValue(reqList);
        }

        public static Request Insert(Request req)
        {
            return new Db(connString).Insert(req);
        }

        public static void DeleteInDB(int nzp_load)
        {
            new Db(connString).Delete(nzp_load);
        }

        public static User CheckUser(string login, string password)
        {
            return new Db(connString).CheckUser(login, password);
        }

        public static List<string> GetSchemas()
        {
            return new Db(connString).GetSchemas();
        }

        public static User SaveUser(User user)
        {
            return new Db(connString).SaveUser(user);
        }
    }
}
