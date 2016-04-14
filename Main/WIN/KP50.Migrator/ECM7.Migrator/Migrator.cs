using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;

using KP50.DataBase.Migrator.Exceptions;
using KP50.DataBase.Migrator.Framework;
using KP50.DataBase.Migrator.Framework.Logging;
using KP50.DataBase.Migrator.Loader;
using KP50.DataBase.Migrator.Providers;
using KP50.DataBase.Migrator.Utils;
using System.Collections;

namespace KP50.DataBase.Migrator
{
    using DataBase = KP50.DataBase.Migrator.Framework.DataBase;
    
    /// <summary>
    /// Migrations mediator.
    /// </summary>
    public class Migrator : IDisposable
    {
        public delegate void MigratorEventHandler(string msg);

        public event MigratorEventHandler onMigrate;

        /// <summary>
        /// Провайдер
        /// </summary>
        private readonly ITransformationProvider provider;
        private readonly string CentralPrefix;

        public ITransformationProvider Provider
        {
            get { return provider; }
        }

        /// <summary>
        /// Загрузчик информации о миграциях
        /// </summary>
        private readonly MigrationAssembly migrationAssembly;

        /// <summary>
        /// Ключ для фильтрации миграций
        /// </summary>
        private string Key
        {
            get { return migrationAssembly.Key; }
        }

        #region constructors

        /// <summary>
        /// Инициализация
        /// </summary>
        public Migrator(string providerTypeName, IDbConnection connection, Assembly asm, string CentralPrefix)
            : this(ProviderFactory.Create(providerTypeName, connection), asm, CentralPrefix)
        {
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        public Migrator(string providerTypeName, string connectionString, Assembly asm, string CentralPrefix)
            : this(ProviderFactory.Create(providerTypeName, connectionString), asm, CentralPrefix)
        {
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        public Migrator(ITransformationProvider provider, Assembly asm, string CentralPrefix)
        {
            Require.IsNotNull(provider, "Не задан провайдер трансформации");
            this.provider = provider;

            Require.IsNotNull(asm, "Не задана сборка с миграциями");
            migrationAssembly = new MigrationAssembly(asm);

            // Require.IsNotNullOrEmpty(CentralPrefix, "Не задан префикс центрального банка");
            this.CentralPrefix = CentralPrefix;
        }

        #endregion

        /// <summary>
        /// Returns registered migration <see cref="System.Type">types</see>.
        /// </summary>
        public ReadOnlyCollection<MigrationInfo> AvailableMigrations
        {
            get { return migrationAssembly.MigrationsTypes; }
        }

        /// <summary>
        /// Returns the current migrations applied to the database.
        /// </summary>
        public IList<long> GetAppliedMigrations()
        {
            return provider.GetAppliedMigrations(CentralPrefix, Key);
        }

        /// <summary>
        /// Migrate the database to a specific version.
        /// Runs all migration between the actual version and the
        /// specified version.
        /// If <c>version</c> is greater then the current version,
        /// the <c>Apply()</c> method will be invoked.
        /// If <c>version</c> lower then the current version,
        /// the <c>Revert()</c> method of previous migration will be invoked.
        /// If <c>dryrun</c> is set, don't write any changes to the database.
        /// </summary>
        /// <param name="databaseVersion">The version that must became the current one</param>
        public int Migrate(string CentralPrefix, long databaseVersion = -1, bool CheckUpdates = false, bool IgnoreLocalBank = false)
        {
            databaseVersion = databaseVersion < 0 ? migrationAssembly.LastVersion : databaseVersion;
            provider.SCHEMA_INFO_TABLE.Schema = string.IsNullOrWhiteSpace(CentralPrefix) ? Provider.Connection.Database : string.Format("{0}_kernel", CentralPrefix);
            long targetVersion = databaseVersion < 0 ? migrationAssembly.LastVersion : databaseVersion;

            MigrationPlan plan = new MigrationPlan(provider, migrationAssembly, Key, CentralPrefix, databaseVersion, IgnoreLocalBank);
            if (CheckUpdates) return plan.Count;

            List<long> ExecutedVer = new List<long>();
            // Downgrade banks
            foreach (long currentExecutedVersion in plan.Plan.Where(x => x.Key > databaseVersion).OrderByDescending(y => y.Key).Select(y => y.Key))
            {
                foreach (MigrationBank schema in plan.Plan.Where(x => x.Key == currentExecutedVersion).Select(y => y.Value).First())
                {
                    long currentDatabaseVersion = plan.LastAppliedVersion(schema.Name);
                    //MigratorLogManager.Log.Started(currentDatabaseVersion, targetVersion);
                    //ExecuteMigration(currentExecutedVersion, currentDatabaseVersion, schema.Bank, CentralPrefix, schema.Name);
                    plan.SetLastAppliedVersion(schema.Name, currentExecutedVersion);
                    ExecutedVer.Add(currentExecutedVersion);
                }
            }
            ExecutedVer.ForEach(x => plan.Plan.Remove(x));
            ExecutedVer.Clear();

            // Upgrade banks
            foreach (long currentExecutedVersion in plan.Plan.Keys)
            {
                foreach (MigrationBank schema in plan.Plan.Where(x => x.Key == currentExecutedVersion).Select(y => y.Value).First())
                {
                    long currentDatabaseVersion = plan.LastAppliedVersion(schema.Name);
                    MigratorLogManager.Log.Started(currentDatabaseVersion, targetVersion);
                    ExecuteMigration(currentExecutedVersion, currentDatabaseVersion, schema.Bank, CentralPrefix, schema.Name);
                    plan.SetLastAppliedVersion(schema.Name, currentExecutedVersion);
                }
            }
            return 0;
        }

        public void ExecuteMigration(long targetVersion, long currentDatabaseVersion, DataBase database, string CentralPrefix, string DataBase)
        {
            var migrationInfo = migrationAssembly.GetMigrationInfo(targetVersion, database);
            IMigration migration = migrationAssembly.InstantiateMigration(migrationInfo, provider);
            switch (database)
            {
                case Framework.DataBase.CentralBank:
                    migration.CurrentPrefix = CentralPrefix;
                    break;
                case Framework.DataBase.LocalBank:
                    migration.CurrentPrefix = DataBase.Replace("_kernel", "");
                    break;
                case Framework.DataBase.Charge:
                    migration.CurrentPrefix = DataBase.Substring(0,DataBase.IndexOf("_charge"));
                    break;
                case Framework.DataBase.Fin:
                    migration.CurrentPrefix = DataBase.Substring(0, DataBase.IndexOf("_fin"));
                    break;
                default:
                    migration.CurrentPrefix = null;
                    break;
            }

            if (CentralPrefix != null)
            {
                migration.CentralPrefix = CentralPrefix;
                migration.CentralKernel = CentralPrefix + "_kernel";
                migration.CentralData = CentralPrefix + "_data";
            }
            migration.CurrentSchema = DataBase;

            try
            {
                if (!migrationInfo.WithoutTransaction && provider.ProviderName != "Informix")
                {
                    provider.BeginTransaction();
                }

                if (targetVersion <= currentDatabaseVersion)
                {
                    MigratorLogManager.Log.MigrateDown(targetVersion, migration.Name);
                    if (onMigrate != null) onMigrate(string.Format("Removing {0}: {1}. Bank: {2}.\n", targetVersion, migration.Name, DataBase));
                    provider.SetSchema(DataBase);
                    migration.Revert();
                    provider.SetSchema(migration.CentralKernel);
                    provider.MigrationUnApplied(targetVersion, Key, DataBase);
                }
                else
                {
                    MigratorLogManager.Log.MigrateUp(targetVersion, migration.Name);
                    if (onMigrate != null) onMigrate(string.Format("Applying {0}: {1}. Bank: {2}.\n", targetVersion, migration.Name, DataBase));
                    provider.SetSchema(DataBase);
                    migration.Apply();
                    if (migration.CentralKernel != null && migration.CurrentPrefix != null) provider.SetSchema(migration.CentralKernel);
                    provider.MigrationApplied(targetVersion, Key, DataBase);
                }

                if (!migrationInfo.WithoutTransaction && provider.ProviderName != "Informix")
                {
                    provider.Commit();
                }
            }
            catch (Exception ex)
            {
                MigratorLogManager.Log.Exception(targetVersion, migration.Name, ex);

                if (!migrationInfo.WithoutTransaction)
                {
                    // при ошибке откатываем изменения
                    provider.Rollback();
                    MigratorLogManager.Log.RollingBack(currentDatabaseVersion);
                }

                throw;
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            provider.Dispose();
        }

        #endregion
    }
}
