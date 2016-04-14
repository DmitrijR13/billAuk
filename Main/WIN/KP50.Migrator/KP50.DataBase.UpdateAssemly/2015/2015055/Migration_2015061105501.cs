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
    [Migration(2015061105501, MigrateDataBase.CentralBank)]
    public class Migration_2015061105501_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_doc = new SchemaQualifiedObjectName() { Name = "file_doc", Schema = CurrentSchema };
            if (Database.TableExists(file_doc))
            {
                if (Database.ColumnExists(file_doc, "uniq_doc_code"))
                {
                    Database.ChangeColumn(file_doc, "uniq_doc_code", DbType.String.WithSize(20), true);
                }

                if (Database.ColumnExists(file_doc, "num_ls"))
                {
                    Database.ChangeColumn(file_doc, "num_ls", DbType.String.WithSize(20), true);
                }
            }

            SchemaQualifiedObjectName file_norm_table = new SchemaQualifiedObjectName() { Name = "file_norm_table", Schema = CurrentSchema };
            if (Database.TableExists(file_doc))
            {
                if (Database.ColumnExists(file_norm_table, "norm_name"))
                {
                    Database.RenameColumn(file_norm_table, "norm_name", "str_name");
                }

                if (Database.ColumnExists(file_norm_table, "norm_code"))
                {
                    Database.RenameColumn(file_norm_table, "norm_code", "str_code");
                }

                if (Database.ColumnExists(file_norm_table, "st_name"))
                {
                    Database.RenameColumn(file_norm_table, "st_name", "col_name");
                }

                if (Database.ColumnExists(file_norm_table, "st_code"))
                {
                    Database.RenameColumn(file_norm_table, "st_code", "col_code");
                }
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
