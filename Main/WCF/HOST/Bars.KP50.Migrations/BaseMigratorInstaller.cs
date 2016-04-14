using System;
using System.Collections.Generic;
using System.Text;

namespace Bars.KP50.Migration
{

    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;
    using ECM7.Migrator.Providers.Informix;
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
            if (!Container.Kernel.HasComponent(typeof(IMigratorProvider)))
            {
                Container.Register(Component.For<IMigratorProvider>().ImplementedBy<InformixTransformationProvider>().LifestyleSingleton());
            }

            //RegisterMigrator();
        }

        public void Register<T>() where T : class, IBaseMigrator, new()
        {
            Container.Resolve<IMigratorProvider>().RegisterMigrator(new T());

            //RegisterJob<T>();
        }
    }
}
