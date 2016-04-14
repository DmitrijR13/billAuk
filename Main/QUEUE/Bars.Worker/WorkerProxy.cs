namespace Bars.Worker
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;

    using Bars.QueueCore;

    using Castle.MicroKernel.Registration;
    using Castle.Windsor;

    using NLog;

    public static class WorkerProxy
    {
        private static readonly object LockObject = new object();

        public static IWindsorContainer Container { get; set; }

        private static bool IsInit { get; set; }

        public static void Init(string assemblyesDirectory)
        {
            if (!IsInit)
            {
                lock (LockObject)
                {
                    var logger = LogManager.GetLogger("nlogger");
                    logger.Info("Init WorkerProxy");

                    Container = new WindsorContainer();
                    
                    Container.Register(Classes
                        .FromAssemblyInDirectory(new AssemblyFilter(assemblyesDirectory))
                        .BasedOn<IJobInstaller>()
                        .WithServiceBase()
                        .Configure(c => c.Named(c.Implementation.FullName).LifestyleTransient()));

                    Container.Register(Classes
                        .FromAssemblyInDirectory(new AssemblyFilter(assemblyesDirectory))
                        .BasedOn<IWindsorInstaller>()
                        .WithServiceBase()
                        .Configure(c => c.Named(c.Implementation.FullName).LifestyleTransient()));

                    foreach (var regisrar in Container.ResolveAll<IJobInstaller>())
                    {
                        regisrar.Install(Container);
                        Container.Release(regisrar);
                        logger.Info("Install {0}", regisrar.GetType().FullName);
                    }

                    IsInit = true;
                }
            }
        }

        public static void Run(JobArguments args)
        {
            var logger = LogManager.GetLogger("nlogger");
            logger.Info("Run job. Code: {0}, Id: {1}", args.Code, args.JobId);
            var job = Container.Resolve<IJob>(args.Code);
            if (job != null)
            {
                try
                {
                    job.Run(Container, args);
                }
                catch (Exception exc)
                {
                    logger.Error(
                        "Error execute job. Code: {0}, Id: {1}, Name: {2}, Params: {3}",
                        args.Code,
                        args.JobId,
                        args.Name,
                        args.Parameters.AllKeys.Aggregate(string.Empty, (current, key) => current + string.Format("{0}: {1}; ", key, args.Parameters[key])).TrimStart(';').Trim());
                    logger.Error(exc);
                    throw;
                }
            }
            else
            {
                logger.Error("Not found job. Code: {0}, Id: {1}", args.Code, args.JobId);
            }
        }

        public static void Stop(bool init)
        {
            if (!IsInit)
            {
                lock (LockObject)
                {
                    IsInit = init;
                }
            }
        }
    }
}