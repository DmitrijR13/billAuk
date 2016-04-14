namespace Bars.KP50.Queue
{
    using Bars.QueueCore;

    /// <summary>Интерфейс провайдера работ</summary>
    public interface IJobProvider
    {
        /// <summary>Добавить работу на выполнение</summary>
        /// <param name="jobType">Тип работы</param>
        /// <param name="jobArguments">Параметры запуска работы</param>
        /// <param name="queueName">Название очереди</param>
        void AddJob(JobType jobType, JobArguments jobArguments, string queueName);

        /// <summary>
        /// Остановить выполнение работы
        /// </summary>
        /// <param name="jobId">работа</param>
        void StopJob(int jobId);

        /// <summary>
        /// Перезапустить работу
        /// </summary>
        /// <param name="jobId">работа</param>
        void RestartJob(int jobId);
    }
}