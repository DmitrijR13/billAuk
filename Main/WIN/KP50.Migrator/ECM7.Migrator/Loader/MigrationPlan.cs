using KP50.DataBase.Migrator.Exceptions;
using KP50.DataBase.Migrator.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Utils;

namespace KP50.DataBase.Migrator.Loader
{
    using DataBase = KP50.DataBase.Migrator.Framework.DataBase;
    using System.Data;
    using KP50.DataBase.Migrator.Compatibility;
    using KP50.DataBase.Migrator.Framework.Logging;
    using System.Threading;

    public sealed class MigrationBank
    {
        public string Name { get; private set; }

        public DataBase Bank { get; private set; }

        public MigrationBank(string name, DataBase bank)
        {
            Name = name;
            Bank = bank;
        }
    }

    public class MigrationPlan
    {
        private readonly long Version;
        private readonly Dictionary<MigrationBank, IList<long>> dicMigrationPlans;
        private readonly Dictionary<string, long> dicLastUpdate;

        public IDictionary<long, IList<MigrationBank>> Plan { get; private set; }

        public int Count { get { return Plan.Sum(x => x.Value.Count); } }

        public long LastAppliedVersion(string schema)
        {
            return dicLastUpdate.Where(x => x.Key == schema).Select(x => x.Value).First();
        }

        public void SetLastAppliedVersion(string schema, long version)
        {
            dicLastUpdate.Remove(schema);
            dicLastUpdate.Add(schema, version);
        }

        public MigrationPlan(ITransformationProvider Provider, MigrationAssembly migrationAssembly, string Key, string CentralPrefix, long Version, bool IgnoreLocalBank = false)
        {
            if (!Provider.TableExists(Provider.SCHEMA_INFO_TABLE))
                Provider.AddTable(
                  Provider.SCHEMA_INFO_TABLE,
                  new Column("Version", DbType.Int64, ColumnProperty.NotNull),
                  new Column("AssemblyKey", DbType.String.WithSize(20), ColumnProperty.NotNull, "''"),
                  new Column("Prefix", DbType.String.WithSize(20), ColumnProperty.NotNull, "''"));
            else
            {
                if (!Provider.ColumnExists(Provider.SCHEMA_INFO_TABLE, "AssemblyKey"))
                    if (Provider.ColumnExists(Provider.SCHEMA_INFO_TABLE, "Key")) UpdateSchemaInfo.Update2To4(Provider);
                    else UpdateSchemaInfo.Update1To4(Provider);
                if (!Provider.ColumnExists(Provider.SCHEMA_INFO_TABLE, "UpdateDate"))
                    UpdateSchemaInfo.Update3To4(Provider);
            }

            this.Version = Version < 0 ? migrationAssembly.LastVersion : Version;
            dicMigrationPlans = new Dictionary<MigrationBank, IList<long>>();
            dicLastUpdate = new Dictionary<string, long>();
            #region Bild migration plans for each bank.
            if (!string.IsNullOrWhiteSpace(CentralPrefix)) Provider.SetSchema(string.Format("{0}_kernel", CentralPrefix));
            IList<long> availableMigrations;
            IList<long> appliedMigrations;
            if (CentralPrefix == null)
            {
                availableMigrations = migrationAssembly.MigrationsTypes
                  .Where(mInfo => mInfo.DB.HasFlag(DataBase.Web))
                  .Select(mInfo => mInfo.Version).ToList();
                appliedMigrations = Provider.GetAppliedMigrations(Provider.Connection.Database, Key).Where(mInfo => mInfo <= this.Version).ToList();
                IList<long> migrationPlan = BuildMigrationPlan(this.Version, appliedMigrations, availableMigrations, Provider.Connection.Database);
                if (migrationPlan.HasElements()) dicMigrationPlans.Add(new MigrationBank(Provider.Connection.Database, DataBase.Web), migrationPlan);
            }
            else
            {
                foreach (DataBase database in Enum.GetValues(typeof(DataBase)))
                {
                    if (database.HasFlag(DataBase.LocalBank) && IgnoreLocalBank) continue;
                    availableMigrations = migrationAssembly.MigrationsTypes
                      .Where(mInfo => mInfo.DB.HasFlag(database))
                      .Select(mInfo => mInfo.Version).ToList();
                    foreach (string schema in Provider.GetBasesList(database, CentralPrefix))
                    {
                        appliedMigrations = Provider.GetAppliedMigrations(schema, Key).Where(mInfo => mInfo <= this.Version).ToList();
                        IList<long> migrationPlan = BuildMigrationPlan(this.Version, appliedMigrations, availableMigrations, schema);
                        if (migrationPlan.HasElements()) dicMigrationPlans.Add(new MigrationBank(schema, database), migrationPlan);
                    }
                }
            }
            #endregion

            Plan = new Dictionary<long, IList<MigrationBank>>();
            #region Build plan per version
            IList<long> avalibleVersions = new List<long>();
            foreach (IList<long> lst in dicMigrationPlans.Values) avalibleVersions = avalibleVersions.Union(lst).ToList();
            avalibleVersions = avalibleVersions.OrderBy(x => x).Distinct().ToList();
            IList<MigrationBank> migrateBank = new List<MigrationBank>();
            foreach (long version in avalibleVersions)
                this.Plan.Add(version, dicMigrationPlans.Where(x => x.Value.Where(ver => ver == version).HasElements()).Select(x => x.Key).ToList());
            #endregion
        }

        /// <summary>
        /// Получить список версий для выполнения
        /// </summary>
        /// <param name="target">Версия назначения</param>
        /// <param name="appliedMigrations">Список версий выполненных миграций</param>
        /// <param name="availableMigrations">Список версий доступных миграций</param>
        private List<long> BuildMigrationPlan(long target, IList<long> appliedMigrations, IList<long> availableMigrations, string schema)
        {
            long startVersion = appliedMigrations.IsEmpty() ? 0 : appliedMigrations.Max();
            try { dicLastUpdate.Add(schema, startVersion); }
            catch (Exception ex) { throw new Exception(string.Format("Не удалось добавить банк \"{0}\" в словарь. Возможно, ссылка на банк дублируется в таблице s_baselist.", schema), ex); }

            var set = new HashSet<long>(appliedMigrations);

            // проверки
            var list = availableMigrations.Where(x => x < startVersion && !set.Contains(x)).ToList();
            if (!list.IsEmpty())
            {
                MigratorLogManager.Log.Warn(
                    string.Format("Доступны невыполненные миграции, версия которых меньше текущей версии БД:\n{0}", 
                    string.Join(Environment.NewLine, list.Distinct().ToArray())));

                throw new VersionException(
                  "Доступны невыполненные миграции, версия которых меньше текущей версии БД", list.Distinct().ToArray());
            }

            set.UnionWith(availableMigrations);

            var versions = target < startVersion
                    ? set.Where(n => n <= startVersion && n > target).OrderByDescending(x => x).ToList()
                    : set.Where(n => n > startVersion && n <= target).OrderBy(x => x).ToList();

            return versions;
        }
    }
}
