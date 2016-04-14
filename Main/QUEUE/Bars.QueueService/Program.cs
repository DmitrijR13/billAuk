namespace Bars.QueueService
{
    using System;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.ServiceProcess;
    using NLog;
    using NLog.Config;

    public static class Program
    {
        public static void Main(string[] args)
        {
            LogManager.Configuration = new XmlLoggingConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
            var logger = LogManager.GetLogger("nlogger");

            if (Environment.UserInteractive)
            {
                var parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                            // для запуска сервиса после установки
                            //var process = new Process
                            //{
                            //    StartInfo = new ProcessStartInfo()
                            //    {
                            //        WindowStyle = ProcessWindowStyle.Hidden,
                            //        FileName = "cmd.exe",
                            //        Arguments = "/C net start Bars.Queueservice"
                            //    }
                            //};
                            //process.Start();
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorException("", ex);
                        }
                        break;
                    case "--uninstall":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorException("", ex);
                        }
                        break;
                    default:
                        logger.Info("Не определен параметр. Используйте --install для установки сервиса и --uninstall для удаления.");
                        break;
                }
            }
            else
            {
                logger.Info("Start service");

                ServiceBase[] servicesToRun = { new QueueService() };

                ServiceBase.Run(servicesToRun);
            }
        }
    }
}