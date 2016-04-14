using System;
using Bars.KP50.Faktura.Source.BillProvider;
using Bars.QueueCore;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Globals.SOURCE.Container;

namespace Bars.KP50.Faktura.Source
{
    /// <summary>Установщик отчетов</summary>
    public abstract class BaseBillInstaller : IBillInstaller
    {

        protected IWindsorContainer Container { get; set; }

        public void Install(IWindsorContainer container)
        {
            Container = container;
            Register();
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Install(container);
        }

        /// <summary>Зарегистрировать отчеты</summary>
        protected abstract void RegisterBills();

        protected virtual void Register()
        {
            if (!Container.Kernel.HasComponent(typeof(IBillProvider)))
            {
                Container.Register(Component.For<IBillProvider>().ImplementedBy<BillProvider.BillProvider>().LifestyleSingleton());
            }

            RegisterBills();
        }

        /// <summary>Зарегистрировать отчет</summary>
        /// <typeparam name="T">Тип отчета</typeparam>
        protected void Register<T>(string reportCode) where T : class, IBaseBill, new()
        {
            Container.Resolve<IBillProvider>().RegisterBill(new T());
            
            Container.Register(
                   Component
                   .For<IBaseBill>()
                   .ImplementedBy(typeof(T))
                   .Named(reportCode)
                   .LifestyleTransient());

        }
    }
}