using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015055
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015060505502, MigrateDataBase.CentralBank)]
    public class Migration_2015060505502_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };
            if (Database.TableExists(file_gilec))
            {
                if (!Database.ColumnExists(file_gilec, "doc_name"))
                {
                    Database.AddColumn(file_gilec, new Column("doc_name", DbType.String.WithSize(30)));
                }
            }

            SchemaQualifiedObjectName file_doc = new SchemaQualifiedObjectName() { Name = "file_doc", Schema = CurrentSchema };
            if (!Database.TableExists(file_doc))
            {
                Database.AddTable(file_doc,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("uniq_doc_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("num_ls", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_gil", DbType.Int32, ColumnProperty.None),
                    new Column("urlic_id", DbType.Int32, ColumnProperty.None),
                    new Column("fam", DbType.String.WithSize(30), ColumnProperty.NotNull),
                    new Column("ima", DbType.String.WithSize(30), ColumnProperty.NotNull),
                    new Column("otch", DbType.String.WithSize(30), ColumnProperty.None),
                    new Column("birth_date", DbType.Date, ColumnProperty.NotNull),
                    new Column("doc_sobstv_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull)
                    );
            }

            SchemaQualifiedObjectName file_group = new SchemaQualifiedObjectName() { Name = "file_group", Schema = CurrentSchema };
            if (!Database.TableExists(file_group))
            {
                Database.AddTable(file_group,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("group_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("group_name", DbType.String.WithSize(80), ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.Date, ColumnProperty.NotNull),
                    new Column("table_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull)
                    );
            }

            SchemaQualifiedObjectName file_norm_table = new SchemaQualifiedObjectName() { Name = "file_norm_table", Schema = CurrentSchema };
            if (!Database.TableExists(file_norm_table))
            {
                Database.AddTable(file_norm_table,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("table_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("norm_name", DbType.String.WithSize(80), ColumnProperty.NotNull),
                    new Column("norm_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("st_name", DbType.String.WithSize(80), ColumnProperty.NotNull),
                    new Column("st_code", DbType.Int32, ColumnProperty.NotNull),
                    new Column("znach", DbType.Decimal.WithSize(12, 2), ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull)
                    );
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
