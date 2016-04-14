using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using AutoMapper;
using FormatLibrary;

namespace Loader
{
    public class Instance : ILoader
    {
        public Instance()
        {
            var AbsolutePath = Path.Combine(Path.GetTempPath(), "Loader");
            Directory.CreateDirectory(AbsolutePath);
            var dict = new Dictionary<string, string>
            {
               { Path.Combine(AbsolutePath, "gkh_del.bat"),Properties.Resources.gkh_del},
               { Path.Combine(AbsolutePath, "gkh_must_calc.bat"),Properties.Resources.gkh_must_calc},
               { Path.Combine(AbsolutePath, "gkh_pack_ls.bat"),Properties.Resources.gkh_pack_ls},
               { Path.Combine(AbsolutePath, "gkh_triggers_data_off.bat"),Properties.Resources.gkh_triggers_data_off},
               { Path.Combine(AbsolutePath, "gkh_triggers_data_on.bat"),Properties.Resources.gkh_triggers_data_on},
               { Path.Combine(AbsolutePath, "gkh_upd.bat"),Properties.Resources.gkh_upd},
               { Path.Combine(AbsolutePath, "part_payment.bat"),Properties.Resources.part_payment}
            };
            foreach (var d in dict)
            {
                if (File.Exists(d.Key))
                {
                    File.Delete(d.Key);
                }
                using (var file = File.Create(d.Key))
                {
                    using (var writer = new StreamWriter(file))
                    {
                        writer.Write(d.Value);
                        writer.Flush();
                    }
                    file.Close();
                }
            }
            if (File.Exists(Path.Combine(AbsolutePath, "ISOto1251.exe")))
            {
                File.Delete(Path.Combine(AbsolutePath, "ISOto1251.exe"));
            }
            File.WriteAllBytes(Path.Combine(AbsolutePath, "ISOto1251.exe"), Properties.Resources.ISOto1251);
        }

        protected sealed class SingletonCreator
        {
            private static readonly Instance instance = new Instance();
            public static Instance Instance { get { return instance; } }
        }

        public static Instance LoaderInstance
        {
            get { return SingletonCreator.Instance; }
        }

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
        public List<AssembleAttribute> GetFormats()
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
                    RegistrationName = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().RegistrationName
                }).ToList();
        }

        public List<AssembleAtr> GetFormatsForWeb()
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
                    RegistrationName = x.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().First().RegistrationName
                }).ToList();
        }


        /// <summary>
        /// Асинхронный запуск потоков
        /// </summary>
        /// <param name="method">Исполняемый Метод</param>
        /// <param name="format"></param>
        protected static void RunAsynchronously(Func<Format, Returns> method, Format format)
        {
            ThreadPool.QueueUserWorkItem(_ => method(format));
        }

        /// <summary>
        /// Постановка в очередь очередной загрузки
        /// </summary>
        /// <param name="attribute">Атрибуты формата</param>
        /// <returns>UnloadID-идентификатор</returns>
        public Returns Start(Request req)
        {
            req.type = GetFormats().FirstOrDefault(x => x.RegistrationName == req.RegistrationName).type;
            if (req.type == null)
                return new Returns(false, "Не определен формат");
            var format = Mapper.DynamicMap(req, typeof(Request), req.type) as Format;
            format.Progress += onProgress;
            Func<Format, Returns> method = Add;
            RunAsynchronously(method, format);
            return new Returns();
        }

        public Returns Start(Request req, ConfigurationParams prms)
        {
            req.type = GetFormats().FirstOrDefault(x => x.RegistrationName == req.RegistrationName).type;
            if (req.type == null)
                return new Returns(false, "Не определен формат");
            var format = Mapper.DynamicMap(req, typeof(Request), req.type) as Format;
            format.connString = prms.connectionString;
            format.psqlPath = prms.psqlPath;
            format.server = prms.server;
            format.database = prms.database;
            format.port = prms.port;
            format.user = prms.user;
            format.password = prms.password;
            format.Progress += onProgress;
            Func<Format, Returns> method = Add;
            RunAsynchronously(method, format);
            return new Returns();
        }

        public void StartOperation(List<ConfigurationParams> confParams, List<OtherParams> parms)
        {
            var newParms = parms.Where(x => x.OriginalName.Contains("kernel")).ToList();
            newParms.AddRange(parms.Where(x => x.OriginalName.Contains("data")).ToList());
            newParms.AddRange(parms.Where(x => x.OriginalName.Contains("charge") || x.FileName.Contains("fin")).ToList());
            foreach (var req in newParms)
            {
                ConfigurationParams localConn;
                try
                {
                    localConn = SelectConf(req, confParams);
                }
                catch
                {
                    localConn = confParams[0];
                }
                var request = Insert(new Request
                {
                    StatusID = Statuses.Added,
                    Status = "Добавлена",
                    nzp_user = req.nzp_user,
                    Path = req.Path,
                    FileName = req.FileName,
                    RegistrationName = req.RegistrationName,
                    date_charge = new DateTime(),
                    GisFileId = req.GisFileId
                }, localConn);
                Start(request, localConn);
            }
        }

        public void StartOperationPGU(ConfigurationParams confParams, List<OtherParams> parms)
        {
            foreach (var req in parms)
            {
                var request = Insert(new Request
                {
                    StatusID = Statuses.Added,
                    Status = "Добавлена",
                    nzp_user = req.nzp_user,
                    Path = req.Path,
                    FileName = req.FileName,
                    RegistrationName = req.RegistrationName,
                    date_charge = new DateTime(),
                    GisFileId = req.GisFileId
                }, confParams);
                Start(request, confParams);
            }
        }

        protected ConfigurationParams SelectConf(OtherParams req, List<ConfigurationParams> confParams)
        {
            ConfigurationParams localConn = null;
            try
            {
                var AbsolutePath = Path.Combine(Path.GetTempPath(), "Loader");
                var parentDir = string.IsNullOrEmpty(AbsolutePath)
                    ? (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.FullName
                    : AbsolutePath;
                var newPath = Directory.CreateDirectory(string.Format("{0}\\Temp", parentDir));
                if (File.Exists(newPath.FullName + "\\" + req.FileName))
                    File.Delete(newPath.FullName + "\\" + req.FileName);
                File.Copy(Path.Combine(req.Path, req.FileName), newPath.FullName + "\\" + req.FileName);
                Archive.GetInstance(newPath.FullName + "\\" + req.FileName)
                    .Decompress(newPath.FullName + "\\" + req.FileName, newPath.FullName);
                var headerfileLines = File.ReadAllLines(newPath.FullName + "\\_main.txt", Encoding.GetEncoding(1251));
                {
                    var firstLine = headerfileLines[0].Split('|');
                    var schema_code = firstLine[6];
                    foreach (
                        var con in
                            confParams.Where(con => new Db(con.connectionString).SelectSchema(schema_code) != null))
                    {
                        localConn = con;
                    }
                }
                if (!Directory.Exists(newPath.FullName)) return localConn;
                var downloadedMessageInfo = new DirectoryInfo(newPath.FullName);

                foreach (var file in downloadedMessageInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (var dir in downloadedMessageInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                Directory.Delete(newPath.FullName);
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return localConn;
        }

        protected Returns Add(Format req)
        {
            var configs = ConfigurateReader.GetConfig().Where(x => x != null).ToList();
            if (configs.Any())
            {
                req.connString = configs[0];
                req.psqlPath = configs[1];
                req.server = configs[2];
                req.database = configs[3];
                req.port = configs[4];
                req.user = configs[5];
                req.password = configs[6];
            }
            req.Initialize();
            return new Returns();
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

        public Returns Delete(int nzp_load, ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).Delete(nzp_load);
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

        public List<Request> GetImportValues(ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).GetImportValues();
        }

        public List<Request> UpdateImportValue(List<Request> reqList, ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).UpdateImportValue(reqList);
        }

        public Request Insert(Request req, ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).Insert(req);
        }

        public User CheckUser(string login, string password, ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).CheckUser(login, password);
        }

        public List<string> GetSchemas(ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).GetSchemas();
        }

        public User SaveUser(User user, ConfigurationParams config = null)
        {
            var configs = config == null ? ConfigurateReader.GetConfig()[0] : config.connectionString;
            var connString = configs;
            return new Db(connString).SaveUser(user);
        }

    }
}
