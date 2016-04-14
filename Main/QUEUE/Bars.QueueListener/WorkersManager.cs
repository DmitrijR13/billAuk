using NLog;

namespace Bars.QueueListener
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    using Bars.QueueCore;
    using Bars.QueueListener.Domain;
    using Bars.Worker;

    public class WorkersManager : IDisposable
    {
        private AppDomainHelper _domains;
        
        public WorkersManager()
        {
            WorkersPath = ConfigurationManager.AppSettings["workersCachePath"];
            if (!Directory.Exists(WorkersPath))
            {
                throw new DirectoryNotFoundException("Путь сборок не найден. Необходимо указать путь к библиотекам обработчиков.");
            }
            
            this._domains = new AppDomainHelper();
        }

        ~WorkersManager()
        {
            Dispose(false);
        }

        /// <summary>путь к библиотекам обработчиков</summary>
        public string WorkersPath { get; set; }
        
        public void RunWork(JobArguments args, Queue queue)
        {
            var logger = LogManager.GetLogger("nlogger");
            if (string.IsNullOrEmpty(queue.Name))
            {
                throw new ArgumentException("Пустое название очереди", "queue");
            }

            var domain = _domains.CreateDomain(queue.Name, WorkersPath);
            queue.Domain = domain;

            try
            {
                var assembliesToLoad = Directory.GetFiles(WorkersPath, "*.dll");
                domain.LoadAssembles(assembliesToLoad.ToArray());

                domain.Execute(WorkerProxy.Init, WorkersPath);
                domain.Execute(WorkerProxy.Run, args);
            }
            catch (AppDomainUnloadedException ex)
            {
                logger.Info("Сборка выгружена в момент выполнения!");
            }
            catch (Exception ex)
            {
                logger.ErrorException("Ошибка запуска работы! ", ex);
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this._domains != null)
            {
                this._domains.Dispose();
                this._domains = null;
                GC.Collect();
            }
        }

        public bool StopWork(Queue queue)
        {
            try
            {
                if (queue.Domain != null)
                {
                    queue.Domain.Execute(WorkerProxy.Stop, false);
                }
                else
                {
                    throw new Exception("Свойство Domain не задано!");
                }
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetLogger("nlogger");
                logger.ErrorException("Ошибка выгрузки", ex);
            }
            return _domains.UnloadDomain(queue.Name);
        }
    }
}