namespace Bars.QueueCore
{
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;

    public abstract class BaseJobInstaller : IJobInstaller
    {
        protected IWindsorContainer Container { get; set; }

        public void Install(IWindsorContainer container)
        {
            Container = container;
            Register();
        }

        protected abstract void Register();

        /// <summary>Зарегистрировать работу</summary>
        /// <typeparam name="T">Тип реализующий интерфейс IJob</typeparam>
        /// <param name="jobName">Имя работы</param>
        protected void RegisterJob<T>(string jobName = null) where T : IJob
        {
            if (string.IsNullOrEmpty(jobName))
            {
                jobName = typeof(T).FullName;
            }

            Container.Register(
                   Component
                   .For<IJob>()
                   .ImplementedBy(typeof(T))
                   .Named(jobName)
                   .LifestyleTransient());
        }
    }
}