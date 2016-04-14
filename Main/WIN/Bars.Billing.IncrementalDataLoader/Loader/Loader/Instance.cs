using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Bars.Billing.IncrementalDataLoader.Formats;
using Bars.Billing.IncrementalDataLoader.Utils;
using Bars.Billing.IncrementalDataLoader.Utils.ArchiveUtils;
using Bars.Billing.IncrementalDataLoader.Utils.DbUtils;

namespace Bars.Billing.IncrementalDataLoader.Loader
{
    public class Instance : ILoader
    {
        public Instance()
        {
            var absolutePath = Path.Combine(Path.GetTempPath(), "Loader");
            Directory.CreateDirectory(absolutePath);
            var dict = new Dictionary<string, string>
            {
               { Path.Combine(absolutePath, "gkh_del.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_del},
               { Path.Combine(absolutePath, "gkh_must_calc.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_must_calc},
               { Path.Combine(absolutePath, "gkh_pack_ls.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_pack_ls},
               { Path.Combine(absolutePath, "gkh_triggers_data_off.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_triggers_data_off},
               { Path.Combine(absolutePath, "gkh_triggers_data_on.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_triggers_data_on},
               { Path.Combine(absolutePath, "gkh_upd.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.gkh_upd},
               { Path.Combine(absolutePath, "part_payment.bat"),global::Bars.Billing.IncrementalDataLoader.Properties.Resources.part_payment}
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
            if (File.Exists(Path.Combine(absolutePath, "ISOto1251.exe")))
            {
                File.Delete(Path.Combine(absolutePath, "ISOto1251.exe"));
            }
            File.WriteAllBytes(Path.Combine(absolutePath, "ISOto1251.exe"), global::Bars.Billing.IncrementalDataLoader.Properties.Resources.ISOto1251);
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
        public List<AssemblyAttribute> GetFormats()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => (typeof(Format)).IsAssignableFrom(p) && !p.IsAbstract).Select(x =>
                new AssemblyAttribute
                {
                    FormatName = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().FormatName + ", Версия: " +
                    x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().Version,
                    Version = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().Version,
                    type = x,
                    RegistrationName = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().RegistrationName
                }).ToList();
        }

        public List<AssemblyAtr> GetFormatsForWeb()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => (typeof(Format)).IsAssignableFrom(p) && !p.IsAbstract).Select(x =>
                new AssemblyAtr
                {
                    FormatName = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().FormatName + ", Версия: " +
                    x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().Version,
                    Version = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().Version,
                    type = x,
                    RegistrationName = x.GetCustomAttributes(typeof(AssemblyAttribute), true).Cast<AssemblyAttribute>().First().RegistrationName
                }).ToList();
        }


        /// <summary>
        /// Асинхронный запуск потоков
        /// </summary>
        /// <param name="method">Исполняемый Метод</param>
        /// <param name="format"></param>
        protected static Task RunAsynchronously(Func<Format, Returns> method, Format format)
        {
            return Task.Factory.StartNew(() => method(format));
        }

        /// <summary>
        /// Постановка в очередь очередной загрузки
        /// </summary>
        /// <param name="request"></param>
        /// <returns>UnloadID-идентификатор</returns>
        public Returns Start(Request request)
        {
            request.type = GetFormats().FirstOrDefault(x => x.RegistrationName == request.RegistrationName).type;
            if (request.type == null)
                return new Returns(false, "Не определен формат");
            var format = Mapper.DynamicMap(request, typeof(Request), request.type) as Format;
            format.Progress += OnProgress;
            Func<Format, Returns> method = Add;
            RunAsynchronously(method, format);
            return new Returns();
        }

        public Returns Start(Request request, ConfigurationParams prms)
        {
            LogUtils.WriteLog(String.Format("Старт загрузки файла '{0}'", request.FileName),
               ELogType.Info);
            LogUtils.WriteLog(String.Format("Старт загрузки файла '{0}' в '{1}'", request.FileName,prms.connectionString),
               ELogType.Debug);
            
            request.type = GetFormats().FirstOrDefault(x => x.RegistrationName == request.RegistrationName).type;
            if (request.type == null)
            {
                LogUtils.WriteLog(
                    String.Format("Не определен формат файла '{0}'. Значение формата из файла: '{1}'. Загрузка не будет произведена!",
                        request.FileName, request.RegistrationName),
                    ELogType.Info);
                return new Returns(false, "Не определен формат");
            }
            var format = Mapper.DynamicMap(request, typeof(Request), request.type) as Format;
            format.ConnString = prms.connectionString;
            format.PsqlPath = prms.psqlPath;
            format.Server = prms.server;
            format.Database = prms.database;
            format.Port = prms.port;
            format.User = prms.user;
            format.Password = prms.password;
            format.Progress += OnProgress;
            Func<Format, Returns> method = Add;
            
            RunAsynchronously(method, format).Wait();
            LogUtils.WriteLog(String.Format("Завершена загрузка файла '{0}'", request.FileName),
               ELogType.Info);
            LogUtils.WriteLog(String.Format("Завершена загрузка файла '{0}' в '{1}'", request.FileName, prms.connectionString),
               ELogType.Debug);

            return new Returns();
        }

        public void StartOperation(List<ConfigurationParams> confParams, List<OtherParams> parms)
        {

            if (parms[0].EnableLogger)
            {
                LogUtils.EnableLogger(parms[0].EnableLogger);
                LogUtils.WriteLog(
                    String.Format("Запущена инкрементальная загрузка файлов (кол-во файлов: {0} шт.)", parms.Count),
                    ELogType.Info);
            }

            var newParms = parms.Where(x => x.OriginalName.ToLower().Contains("kernel")).ToList();
            newParms.AddRange(parms.Where(x => x.OriginalName.ToLower().Contains("data")).ToList());
            newParms.AddRange(parms.Where(x => x.OriginalName.ToLower().Contains("charge")).ToList());
            newParms.AddRange(parms.Where(x => x.OriginalName.ToLower().Contains("fin")).ToList());

            foreach (var req in newParms)
            {
                //устанавливаем активность логгера
                LogUtils.EnableLogger(req.EnableLogger);

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
            if (parms[0].EnableLogger)
            {
                LogUtils.EnableLogger(parms[0].EnableLogger);
                LogUtils.WriteLog(
                    String.Format("Запущена загрузка файлов в ПГУ (кол-во файлов: {0} шт.)", parms.Count),
                    ELogType.Info);
            }

            foreach (var req in parms)
            {
                //устанавливаем активность логгера 
                LogUtils.EnableLogger(req.EnableLogger);

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
                req.ConnString = configs[0];
                req.PsqlPath = configs[1];
                req.Server = configs[2];
                req.Database = configs[3];
                req.Port = configs[4];
                req.User = configs[5];
                req.Password = configs[6];
            }
            req.Initialize();
            return new Returns();
        }

        /// <summary>d
        /// Функция добавления события по остановке потока 
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="isAlive">Показатель того запущен или остановлен поток</param>
        static void StopThread(int nzp_load, bool isAlive)
        {
            if (stopThread != null)
                stopThread(null, new StopArgs(nzp_load, isAlive));
        }
        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        public static void SendMessage(int nzp_load, string message, Statuses result)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(message, result, nzp_load));
        }

        /// <summary>
        /// Функция добавления события для отправки сообщения клиенту
        /// </summary>
        /// <param name="nzp_load">Идентификатор</param>
        /// <param name="message">Сообщение</param>
        /// <param name="result">Результат выполнения операции</param>
        /// <param name="link">Ссылка на файл</param>
        public static void SendMessage(int nzp_load, string message, Statuses result, string link)
        {
            if (sendMessage != null)
                sendMessage(null, new SendArgs(message, result, nzp_load, link));
        }

        static void OnProgress(object sender, ProgressArgs arg)
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
        /// <param name="isAlive">Показаль того запущен или остановлен поток</param>
        public static void StopResume(int nzp_load, bool isAlive)
        {
            StopThread(nzp_load, isAlive);
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
