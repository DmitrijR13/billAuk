using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

using KP50.DataBase.Migrator.Exceptions;
using KP50.DataBase.Migrator.Framework;
using KP50.DataBase.Migrator.Framework.Logging;
using KP50.DataBase.Migrator.Utils;

namespace KP50.DataBase.Migrator.Loader
{
    using DataBase = KP50.DataBase.Migrator.Framework.DataBase;

    /// <summary>
    /// Класс для работы с миграциями в сборке
    /// </summary>
    public class MigrationAssembly
    {
        /// <summary>
        /// Список загруженных типов миграций
        /// </summary>
        private readonly ReadOnlyCollection<MigrationInfo> migrationsTypes;

        /// <summary>
        /// Returns registered migration <see cref="System.Type">types</see>.
        /// </summary>
        public ReadOnlyCollection<MigrationInfo> MigrationsTypes
        {
            get { return migrationsTypes; }
        }

        /// <summary>
        /// Ключ миграций для данной сборки
        /// </summary>
        private readonly string key;

        /// <summary>
        /// Ключ миграций для данной сборки
        /// </summary>
        public string Key
        {
            get { return key; }
        }

        /// <summary>
        /// Максимальная доступная версия
        /// </summary>
        private readonly long lastVersion;

        /// <summary>
        /// Максимальная доступная версия
        /// </summary>
        public long LastVersion
        {
            get { return lastVersion; }
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="asm">Сборка с миграциями</param>
        public MigrationAssembly(Assembly asm)
        {
            Require.IsNotNull(asm, "Не задана сборка с миграциями");

            key = GetAssemblyKey(asm);

            var mt = GetMigrationInfoList(asm);
            var versions = mt.Select(info => info.Version).ToArray();

            foreach (DataBase database in Enum.GetValues(typeof(DataBase)))
            {
                var cache = mt.Where(db => db.DB == database).Select(info => info.Version).ToArray();
                CheckForDuplicatedVersion(cache);
            }

            migrationsTypes = new ReadOnlyCollection<MigrationInfo>(mt);

            lastVersion = versions.IsEmpty() ? 0 : versions.Max();
        }

        public static MigrationAssembly Load(Assembly asm)
        {
            return new MigrationAssembly(asm);
        }

        /// <summary>
        /// Получение ключа миграций для заданной сборки
        /// </summary>
        private static string GetAssemblyKey(Assembly assembly)
        {
            var asmAttribute = assembly.GetCustomAttribute<MigrationAssemblyAttribute>();

            string assemblyKey = asmAttribute == null
              ? string.Empty
              : asmAttribute.Key ?? string.Empty;

            MigratorLogManager.Log.Info("Migration key: {0}", assemblyKey);
            return assemblyKey;
        }

        /// <summary>
        /// Collect migrations in one <c>Assembly</c>.
        /// </summary>
        /// <param name="asm">The <c>Assembly</c> to browse.</param>
        private static List<MigrationInfo> GetMigrationInfoList(Assembly asm)
        {
            var migrations = new List<MigrationInfo>();

            foreach (Type type in asm.GetExportedTypes())
            {
                var attribute = type.GetCustomAttribute<MigrationAttribute>();

                if (attribute != null
                  && typeof(IMigration).IsAssignableFrom(type)
                  && !attribute.Ignore)
                {
                    var mi = new MigrationInfo(type);
                    migrations.Add(mi);
                }
            }

            migrations.Sort(new MigrationInfoComparer());

            // пишем в лог список загруженных миграций
            StringBuilder logMessageBuilder = new StringBuilder("Loaded migrations:").AppendLine();

            foreach (MigrationInfo mi in migrations)
            {
                string msg = string.Format("{0} {1}", mi.Version.ToString().PadLeft(5), StringUtils.ToHumanName(mi.Type.Name));
                logMessageBuilder.AppendLine(msg);
            }

            MigratorLogManager.Log.Info(logMessageBuilder.ToString());


            return migrations;
        }

        /// <summary>
        /// Check for duplicated version in migrations.
        /// </summary>
        /// <exception cref="CheckForDuplicatedVersion">CheckForDuplicatedVersion</exception>
        public static void CheckForDuplicatedVersion(IEnumerable<long> migrationsTypes)
        {
            IEnumerable<long> list = migrationsTypes
              .GroupBy(v => v)
              .Where(x => x.Count() > 1)
              .Select(x => x.Key)
              .ToList();

            if (list.Any())
            {
                throw new DuplicatedVersionException(list.ToArray());
            }
        }

        public MigrationInfo GetMigrationInfo(long version, DataBase DB)
        {
            var targetMigrationInfo = migrationsTypes
              .Where(info => info.Version == version && info.DB.HasFlag(DB))
              .ToList();

            Require.That(targetMigrationInfo.Any(), "Не найдена миграция версии {0}", version);

            return targetMigrationInfo.First();
        }

        /// <summary>
        /// Создать миграцию по номеру версии
        /// </summary>
        /// <param name="migrationInfo">Информация о миграции</param>
        /// <param name="provider">Провайдер СУБД для установки в качестве текущего провайдера миграции</param>
        public IMigration InstantiateMigration(MigrationInfo migrationInfo, ITransformationProvider provider)
        {
            Require.IsNotNull(provider, "Не задан провайдер СУБД");
            Require.IsNotNull(migrationInfo.Type, "Не задан класс миграции");

            var migration = (IMigration)Activator.CreateInstance(migrationInfo.Type);
            migration.Database = provider;
            return migration;
        }
    }
}
