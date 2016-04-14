namespace Bars.QueueCore
{
    using Castle.Windsor;

    /// <summary>Интерфейс для определения работы</summary>
    public interface IJob
    {
        /// <summary>Получить текущее состояние</summary>
        /// <returns>Состояние работы</returns>
        JobState GetState();

        /// <summary>Запустить работу</summary>
        /// <param name="container">IoC контейнер</param>
        /// <param name="jobArguments">Параметры запуска работы</param>
        void Run(IWindsorContainer container, JobArguments jobArguments);
    }
}
