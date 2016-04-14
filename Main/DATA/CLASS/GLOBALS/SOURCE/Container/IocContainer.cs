namespace Globals.SOURCE.Container
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Castle.Core.Internal;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;

    using STCLINE.KP50.Global;

    public static class IocContainer
    {
        private static readonly object LockObject = new object();

        public static bool IsInit { get; private set; }

        public static IWindsorContainer Current { get; private set; }

        public static void SetContainer(IWindsorContainer current)
        {
            Current = current;
        }

        public static void InitContainer(bool console = true)
        {
            if (IsInit || !Monitor.TryEnter(LockObject))
            {
                return;
            }

            try
            {
                Current = new WindsorContainer();

                LoadBinary(console);
                
                IsInit = true;
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Не удалось инициализировать IoC", exc);
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }

        /// <summary>Инициализоровать логгер</summary>
        /// <param name="configurationFileName">Путь к конфигурационному файлу</param>
        public static void InitLogger(string configurationFileName)
        {
            Current.Register(Component.For<ILog>().UsingFactoryMethod(x => NLogLogger.Create("nlog", configurationFileName)).LifestyleSingleton());
        }

        /// <summary>Инициализоровать логгер</summary>
        /// <param name="logger">Логгер</param>
        public static void InitLogger(ILog logger)
        {
            Current.Register(Component.For<ILog>().UsingFactoryMethod(x => logger).LifestyleSingleton());
        }

        public static void LoadReports()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.ToUpper().Contains("REPORT")))
            {
                Current.Register(Classes.FromAssembly(assembly).BasedOn<IReportsInstaller>().WithServiceBase());
            }

            Current.ResolveAll<IReportsInstaller>().ForEach(x => x.Install(Current, null));
        }

        /// <summary>
        /// Загрузка rkfccjd счет-квитанций
        /// </summary>
        public static void LoadBills()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.ToUpper().Contains("FAKTURA")))
            {
                Current.Register(Classes.FromAssembly(assembly).BasedOn<IBillInstaller>().WithServiceBase());
            }

            Current.ResolveAll<IBillInstaller>().ForEach(x => x.Install(Current, null));
        }


        public static void DeinitializeContainer()
        {
            if (Current != null)
            {
                Current.Dispose();
                Current = null;
            }
        }

        private static void LoadBinary(bool console)
        {
            var binFolder = console ? AppDomain.CurrentDomain.BaseDirectory : AppDomain.CurrentDomain.RelativeSearchPath;
            if (Directory.Exists(binFolder))
            {
                var listInclude = new[] { "BARS.", "STCLINE.","REPORT.","FAKTURA." };

                var folder = new DirectoryInfo(binFolder);

                var libraries =
                    folder.GetFiles("*.dll")
                        .Where(x => listInclude.Any(y => x.Name.ToUpper().StartsWith(y)))
                        .Select(x => AssemblyName.GetAssemblyName(x.FullName));

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName()).ToArray();

                foreach (var asmName in libraries)
                {
                    if (loadedAssemblies.Any(x => x.FullName == asmName.FullName))
                    {
                        continue;
                    }

                    Assembly.Load(asmName.FullName);
                }
            }
        }
    }
}