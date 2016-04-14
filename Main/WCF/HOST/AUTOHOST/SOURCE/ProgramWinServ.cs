using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.WinSrv
{
    static class ProgramWinServ
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                var parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new[] {Assembly.GetExecutingAssembly().Location});
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo()
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = "cmd.exe",
                                    Arguments = "/C net start srvhost"
                                }
                            };
                            process.Start();
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteException("", ex);
                        }
                        break;
                    case "--uninstall":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new[] {"/u", Assembly.GetExecutingAssembly().Location});
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteException("", ex);
                        }
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new WinSrv());
            }
        }
    }
}
