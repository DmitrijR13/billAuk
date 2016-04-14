using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using STCLINE.KP50.Global;

namespace ReportServer
{
    /// <summary>
    /// Класс пул потоков
    /// </summary>
    public class ThreadPool
    {
        private int allThreads;    //общее количество потоков
        private int highThreads;   //потоки с высоким приоритетом
        private int normalThreads; //потоки со средним приоритетом
        private int lowThreads;    //потоки с низким приоритетом
        private ManualResetEvent blockEvent;
        private bool isBlocking;
        private object blockLock;
        private Dictionary<int, ManualResetEvent> threadsEvent;
        private Thread[] threads;
        private List<ReportParams> reports;
        private ManualResetEvent timeTableEvent;
        private Thread timeTableThread;
        private bool isDisposed;

        /// <summary>
        /// создает пул потоков с указанным количеством потоков
        /// </summary>
        /// <param name="threadNumber">количество потоков</param>
        public ThreadPool(int threadNumber)
        {
            if (threadNumber < 0)
                throw new ArgumentException("threadNumber", "Количество потов должно быть больше нуля");

            this.allThreads = threadNumber;
            this.highThreads = (int)(threadNumber / 2);
            double temp = (this.allThreads - this.highThreads) / 2;
            this.normalThreads = (int)Math.Round(temp);
            this.lowThreads = threadNumber - this.highThreads - this.normalThreads;
            this.blockLock = new object();
            this.blockEvent = new ManualResetEvent(false);
            this.timeTableEvent = new ManualResetEvent(false);
            this.timeTableThread = new Thread(StartTimeTableThread) { Name = "Timetable Thread", IsBackground = true };
            timeTableThread.Start();
            this.threads = new Thread[threadNumber];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(threadNumber);

            for (int i = 0; i < threadNumber; i++)
            {
                threads[i] = new Thread(ThreadWork) { Name = "Pool Thread" + i.ToString(), IsBackground = true };
                threadsEvent.Add(threads[i].ManagedThreadId, new ManualResetEvent(false));
                threads[i].Start();
            }
            this.reports = new List<ReportParams>();
        }

        /// <summary>
        /// прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы
        /// </summary>
        ~ThreadPool()
        {
            Dispose(false);
        }

        /// <summary>
        /// освобождает ресурсы, которые используются пулом потоков
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// освобождает ресурсы, которые используются пулом потоков
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    timeTableThread.Abort();
                    //timeTableEvent.Dispose();
                    timeTableEvent.Close();
                    for (int i = 0; i < allThreads; i++)
                    {
                        threads[i].Abort();
                        //threadsEvent[threads[i].ManagedThreadId].Dispose();
                        threadsEvent[threads[i].ManagedThreadId].Close();
                    }
                }

                isDisposed = true;
            }
        }

        /// <summary>
        /// запуск пула
        /// </summary>
        private void ThreadWork()
        {
            while (true)
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                ReportParams task = SelectReport();
                if (task != null)
                {
                    try
                    {
                        task.Execute(task);
                    }
                    finally
                    {
                        RemoveReport(task);
                        if (isBlocking)
                            blockEvent.Set();
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }
                }
            }
        }

        private void StartTimeTableThread()
        {
            while (true)
            {
                timeTableEvent.WaitOne();
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            //переводим в сигнальное состояние
                            threadsEvent[thread.ManagedThreadId].Set();
                            //break;
                        }
                    }
                }
                timeTableEvent.Reset();
            }
        }

        private ReportParams SelectReport()
        {
            lock (reports)
            {
                if (reports.Count != 0)
                {
                    //отчеты, ожидающие очереди на выполнение
                    List<ReportParams> waitingReports = reports.Where(t => !t.IsRunned).ToList();
                    //отчеты высокого приоритета
                    List<ReportParams> highReports = waitingReports.Where(t => t.priority == ReportPriority.High.GetHashCode()).ToList();
                    //отчеты среднего приоритета
                    List<ReportParams> normalReports = waitingReports.Where(t => t.priority == ReportPriority.Normal.GetHashCode()).ToList();

                    if (highReports.Count() > 0)
                    {
                        ReportParams c = highReports.First();
                        c.isRunned = true;
                        return c;
                    }
                    else
                    {
                        if (normalReports.Count() > 0)
                        {
                            ReportParams c = normalReports.First();
                            c.isRunned = true;
                            return c;
                        }
                        else
                        {
                            var lowTasks = waitingReports.Where(t => t.priority == ReportPriority.Low.GetHashCode()).ToArray();
                            ReportParams c = lowTasks.FirstOrDefault();
                            if (c != null)
                            {
                                c.isRunned = true;
                            }
                            return c;
                        }
                    }
                }
                return null;
            }
        }

        private void AddReport(ReportParams task)
        {
            lock (reports)
            {
                reports.Add(task);
            }
            timeTableEvent.Set();
        }

        private void RemoveReport(ReportParams task)
        {
            lock (reports)
            {
                reports.Remove(task);
            }

            if (reports.Count > 0 && reports.Where(t => !t.IsRunned).Count() > 0)
            {
                timeTableEvent.Set();
            }
        }

        /// <summary>
        /// ставит задачу в очередь
        /// </summary>
        /// <param name="task">задача</param>
        /// <returns>Возвращает значание удалось ли поставить задачу в очередь.</returns>
        public bool Execute(ReportParams report)
        {
            if (report == null)
                throw new ArgumentNullException("Report", "The Report can't be null.");

            lock (blockLock)
            {
                if (isBlocking)
                {
                    return false;
                }
                AddReport(report);
                return true;
            }
        }

        /// <summary>
        /// ставить несколько задачь в очередь.
        /// </summary>
        /// <param name="tasks">массив задач</param>
        /// <returns>возвращает false, если хотя бы одну задачу не удалось запустить</returns>
        public bool ExecuteRange(IEnumerable<ReportParams> tasks)
        {
            bool result = true;
            foreach (var task in tasks)
            {
                if (!Execute(task))
                    result = false;
            }

            return result;
        }

        /// <summary>
        /// останавливает работу пула потоков
        /// ожидает завершения всех задач (запущенных и стоящих в очереди) и уничтожает все ресурсы
        /// </summary>
        public void Stop()
        {
            lock (blockLock)
            {
                isBlocking = true;
            }

            while (reports.Count > 0)
            {
                blockEvent.WaitOne();
                blockEvent.Reset();
            }

            Dispose(true);
        }
    }
}
