using Bars.QTask.Queue;
using System;
using System.Threading;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Токен валидации жизненного цикла задачи
    /// </summary>
    public class ThreadValidationToken
    {
        /// <summary>
        /// Токен отмены
        /// </summary>
        private readonly CancellationToken _token;

        /// <summary>
        /// Исполняемая задача
        /// </summary>
        private readonly ExecutableTask _task = null;

        /// <summary>
        /// Токен валидации жизненного цикла задачи
        /// </summary>
        /// <param name="taskIdentifier">Идентификатор задачи</param>
        /// <param name="token">Токен отмены</param>
        protected internal ThreadValidationToken(int taskIdentifier, CancellationToken token)
        {
            _task = QueueService.Instance.GetTask(taskIdentifier);
            _token = token;
        }

        /// <summary>
        /// Валидация
        /// </summary>
        public void Validate()
        {
            do
            {
                _token.ThrowIfCancellationRequested();
                switch (_task.State)
                {
                    case TaskState.CancellationRequired:
                        throw new OperationCanceledException("Операция прервана пользователем.");

                    case TaskState.SuspendRequired:
                        _task._state = TaskState.Suspended;
                        QueueService.Instance.SetState(_task.Identifier, _task._state.ToString("D"));
                        QueueService.Instance.Scheduler.NotifyTaskSuspended();
                        break;

                    case TaskState.ResumeRequired:
                        _task._state = TaskState.WaitingForFreeThread;
                        QueueService.Instance.SetState(_task.Identifier, _task._state.ToString("D"));
                        QueueService.Instance.Scheduler.NotifyTaskResumed();
                        _task.State = TaskState.Executing;
                        break;
                }
                if (_task.State == TaskState.Suspended) Thread.Sleep(5000);
            } while (_task.State == TaskState.Suspended);
        }
    }
}
