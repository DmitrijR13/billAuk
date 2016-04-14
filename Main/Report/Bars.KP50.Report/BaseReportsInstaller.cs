namespace Bars.KP50.Report
{
    using System;
    using System.IO;

    using Bars.KP50.Report.Base;
    using Bars.QueueCore;

    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;

    using Globals.SOURCE;
    using Globals.SOURCE.Config;
    using Globals.SOURCE.Container;

    /// <summary>Установщик отчетов</summary>
    public abstract class BaseReportsInstaller : BaseJobInstaller, IReportsInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Install(container);
        }

        /// <summary>Зарегистрировать отчеты</summary>
        protected abstract void RegisterReports();

        protected override void Register()
        {
#warning Не забыть в процессе ввода IoC контейнера, сделать регистрацию нормально
            if (!Container.Kernel.HasComponent(typeof(IConfigProvider)))
            {
                Container.Register(Component.For<IConfigProvider>().ImplementedBy<FileConfigProvider>().LifestyleSingleton());
                Container.Resolve<IConfigProvider>().GetConfig();
            }

            if (!Container.Kernel.HasComponent(typeof(ILog)))
            {
                Container.Register(Component.For<ILog>().UsingFactoryMethod(x => NLogLogger.Create("nlog", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"))).LifestyleSingleton());
            }

            if (!Container.Kernel.HasComponent(typeof(IReportProvider)))
            {
                Container.Register(Component.For<IReportProvider>().ImplementedBy<ReportProvider>().LifestyleSingleton());
            }

            RegisterReports();
        }

        /// <summary>Зарегистрировать отчет</summary>
        /// <typeparam name="T">Тип отчета</typeparam>
        protected void Register<T>() where T : class, IBaseReport, IJob, new()
        {
            Container.Resolve<IReportProvider>().RegisterReport(new T());

            RegisterJob<T>();
        }
    }
}