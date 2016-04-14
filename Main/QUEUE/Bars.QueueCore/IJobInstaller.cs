namespace Bars.QueueCore
{
    using Castle.Windsor;

    /// <summary>Интерфейс для регистрации работы</summary>
    public interface IJobInstaller
    {
        /// <summary>Метод в котором должны быть описаны все регистрации работ</summary>
        /// <param name="container">IWindsorContainer</param>
        void Install(IWindsorContainer container);
    }
}