using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.KP50.Migration
{
    using Castle.Windsor;
    using Castle.MicroKernel.SubSystems.Configuration;

    using Globals.SOURCE.Container;

    public abstract class BaseMigratorInstaller : IMigratorInstaller
    {
        protected IWindsorContainer Container { get; set; }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Install(container);
        }

        public void Install(IWindsorContainer container)
        {
            Container = container;
            Register();
        }

        protected virtual void Register()
        {
            if (!Container.Kernel.HasComponent(typeof (IReportProvider)))
            {
                Container.Register(Component.For<IReportProvider>().ImplementedBy<ReportProvider>().LifestyleSingleton());
            }

            RegisterMigrator();
        }

        protected void Register<T>() where T : class, IBaseReport, IJob, new()
        {
            Container.Resolve<IReportProvider>().RegisterReport(new T());

            RegisterJob<T>();
        }
    }
}
