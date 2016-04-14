using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bars.QTask.Queue
{
    /// <summary>
    /// Планировщик задач
    /// </summary>
    internal class QueueScheduler : TaskScheduler
    {
        /// <summary>
        /// Указывает обрабатывает ли текущий поток рабочий элемент.
        /// </summary>
        [ThreadStatic]
        private static bool _isProcessingItems;

        /// <summary>
        /// Список задач к исполнению
        /// </summary>
        private readonly LinkedList<Task> _tasks = null;

        /// <summary>
        /// Семафор для обработки задач
        /// </summary>
        private readonly Semaphore _semaphore = null;

        /// <summary>
        /// Максимальное число потоков, разрешенное данным планировщиком
        /// </summary>
        private int _maxDegreeOfParallelism;

        /// <summary>
        /// Количество задач, которое в данный  момент обрабатывает планировщик.
        /// </summary>
        private int _delegatesQueuedOrRunning;

        /// <summary>
        /// Замороженные задачи
        /// </summary>
        private int _suspendedTasks;

        /// <summary>
        /// Возвращает максимальное число потоков для этого планировщика.
        /// </summary>
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        /// <summary>
        /// Количество свободных потоков
        /// </summary>
        public int FreeThreads { get { return _maxDegreeOfParallelism + _suspendedTasks - _delegatesQueuedOrRunning; } }

        /// <summary>
        /// Gets an enumerable of the tasks currently scheduled on this scheduler. 
        /// </summary>
        /// <returns></returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }

        /// <summary>
        /// Поставить в очередь планировщика задачу
        /// </summary>
        /// <param name="task">Задача</param>
        protected sealed override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (FreeThreads > 0)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        /// <summary>
        /// Попытка удалить ранее запланированную задачу из планировщика.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        protected override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        /// <summary>
        /// Попытка выполнить задачу в текущем потоке
        /// </summary>
        /// <param name="task"></param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns></returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!_isProcessingItems) return false;
            if (taskWasPreviouslyQueued)
                return TryDequeue(task) ? TryExecuteTask(task) : false;
            return TryExecuteTask(task);
        }

        /// <summary>
        /// Извещение о наличии задач к обработке
        /// </summary>
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                _semaphore.WaitOne();
                _isProcessingItems = true;
                try
                {
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            if (!_tasks.Any())
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        TryExecuteTask(item);
                    }
                }
                finally
                {
                    _semaphore.Release();
                    _isProcessingItems = false;
                }
            }, null);
        }

        /// <summary>
        /// Информирует пул о возобновлении работы и ожидает свободного потока
        /// </summary>
        protected internal void NotifyTaskResumed()
        {
            lock (_tasks) _suspendedTasks--;
            _semaphore.WaitOne();
        }

        /// <summary>
        /// Информирует пул о приостановке работы потока
        /// </summary>
        protected internal void NotifyTaskSuspended()
        {
            lock (_tasks)
            {
                _semaphore.Release();
                _suspendedTasks++;
            }
        }

        /// <summary>
        /// Информирует пул об окончании работы
        /// </summary>
        protected internal void NotifyStoppedService()
        {
            _maxDegreeOfParallelism = 0;
        }

        /// <summary>
        /// Информирует пул о снятии задач
        /// </summary>
        /// <param name="dequeue"></param>
        protected internal void NotifyTasksDequeued(params Task[] tasks)
        {
            lock (_tasks)
            {
                foreach (var task in tasks)
                    _tasks.Remove(task);
            }
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        protected internal void Release()
        {
            _semaphore.Close();
            _semaphore.Dispose();
        }

        /// <summary>
        /// Планировщик задач
        /// </summary>
        /// <param name="degreeOfParallelism">Максимальное число потоков планировщика</param>
        protected internal QueueScheduler(int degreeOfParallelism = 10)
        {
            _maxDegreeOfParallelism = degreeOfParallelism;
            _semaphore = new Semaphore(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
            _tasks = new LinkedList<Task>();
        }
    }
}
