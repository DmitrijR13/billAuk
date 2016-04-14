using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.Migrator.Compatibility
{
    public class UpdateSchemaInfo
    {
        /// <summary>
        /// Расширяет таблицу SchemaInfo полем AssemblyKey.
        /// Использутеся при переходе от старой версии
        ///
        /// Должно быть удалено в будущем
        /// </summary>
        /// <param name="provider"></param>
        public static void Update1To4(ITransformationProvider provider)
        {
            provider.AddTable(
              "SchemaTmp",
              new Column("Version", DbType.Int64, ColumnProperty.NotNull),
              new Column("AssemblyKey", DbType.String.WithSize(200), ColumnProperty.NotNull, "''"),
              new Column("Prefix", DbType.String.WithSize(20), ColumnProperty.NotNull, "''"));

            string sql = provider.FormatSql(
              "INSERT INTO {0:NAME} ({1:NAME}) SELECT {1:NAME} FROM {2:NAME}",
                "SchemaTmp", "Version", "SchemaInfo");

            provider.ExecuteNonQuery(sql);

            provider.RemoveTable("SchemaInfo");
            provider.RenameTable("SchemaTmp", "SchemaInfo");

            if (!provider.ColumnExists("SchemaInfo", "UpdateDate"))
                provider.ExecuteNonQuery(
                    provider.FormatSql("ALTER TABLE {0:NAME} ADD COLUMN {1:NAME} {2} DEFAULT {3} NOT NULL",
                        provider.SCHEMA_INFO_TABLE,
                        "UpdateDate",
                        provider.ProviderName == "PostgreSQL" ? "TIMESTAMP" : "DATETIME",
                        provider.ProviderName == "PostgreSQL" ? "NOW()" : "CURRENT"));
        }

        public static void Update2To4(ITransformationProvider provider)
        {
            provider.RenameColumn("SchemaInfo", "Key", "AssemblyKey");
            provider.AddColumn("SchemaInfo", new Column("Prefix", DbType.String.WithSize(20), ColumnProperty.NotNull, "''"));

            if (!provider.ColumnExists("SchemaInfo", "UpdateDate"))
                provider.ExecuteNonQuery(
                    provider.FormatSql("ALTER TABLE {0:NAME} ADD COLUMN {1:NAME} {2} DEFAULT {3} NOT NULL",
                        provider.SCHEMA_INFO_TABLE,
                        "UpdateDate",
                        provider.ProviderName == "PostgreSQL" ? "TIMESTAMP" : "DATETIME",
                        provider.ProviderName == "PostgreSQL" ? "NOW()" : "CURRENT"));
        }

        public static void Update3To4(ITransformationProvider provider)
        {
            if (!provider.ColumnExists("SchemaInfo", "UpdateDate"))
                provider.ExecuteNonQuery(
                    provider.FormatSql("ALTER TABLE {0:NAME} ADD COLUMN {1:NAME} {2} DEFAULT {3} NOT NULL",
                        provider.SCHEMA_INFO_TABLE,
                        "UpdateDate",
                        provider.ProviderName == "PostgreSQL" ? "TIMESTAMP" : "DATETIME",
                        provider.ProviderName == "PostgreSQL" ? "NOW()" : "CURRENT"));
        }
    }
}
