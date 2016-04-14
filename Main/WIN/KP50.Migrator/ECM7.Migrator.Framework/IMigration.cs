namespace KP50.DataBase.Migrator.Framework
{
    /// <summary>
    /// Интерфейс миграции
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// Название
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Represents the database.
        /// <see cref="ITransformationProvider"></see>.
        /// </summary>
        /// <seealso cref="ITransformationProvider">Migration.Framework.ITransformationProvider</seealso>
        ITransformationProvider Database { get; set; }

        string CentralPrefix { get; set; }

        string CurrentPrefix { get; set; }

        string CurrentSchema { get; set; }

        string CentralKernel { get; set; }

        string CentralData { get; set; }

        void SetSchema(Bank bank);

        /// <summary>
        /// Defines tranformations to port the database to the current version.
        /// </summary>
        void Apply();

        /// <summary>
        /// Defines transformations to revert things done in <c>Apply</c>.
        /// </summary>
        void Revert();
    }
}
