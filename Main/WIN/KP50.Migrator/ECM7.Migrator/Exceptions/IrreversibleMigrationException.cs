using System;

namespace KP50.DataBase.Migrator.Exceptions
{
    /// <summary>
    /// Exception thrown in a migration <c>Revert()</c> method
    /// when changes can't be undone.
    /// </summary>
    public class IrreversibleMigrationException : Exception
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public IrreversibleMigrationException()
            : base("Irreversible migration")
        {
        }
    }
}
