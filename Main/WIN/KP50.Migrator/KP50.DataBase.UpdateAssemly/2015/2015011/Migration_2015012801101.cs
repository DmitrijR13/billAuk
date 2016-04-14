using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015012801101, MigrateDataBase.CentralBank)]
    public class Migration_2015012801101_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            if (Database.TableExists(files_imported))
            {
                if (!Database.ColumnExists(files_imported, "nzp_exc_rep_load"))
                {
                    Database.AddColumn(files_imported, new Column("nzp_exc_rep_load", new ColumnType(DbType.Int32)));
                }
            }
        }

        public override void Revert() {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}