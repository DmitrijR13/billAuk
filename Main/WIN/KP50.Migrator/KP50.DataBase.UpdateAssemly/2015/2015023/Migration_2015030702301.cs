using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015023
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015030702301, MigrateDataBase.CentralBank)]
    public class Migration_2015030702301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName() { Name = "file_oplats", Schema = CurrentSchema };

            if (Database.TableExists(file_oplats))
            {
                if (!Database.ColumnExists(file_oplats, "nzp_pack_ls_real"))
                {
                    Database.AddColumn(file_oplats, new Column("nzp_pack_ls_real", DbType.Int32));
                }

            }

            SchemaQualifiedObjectName file_raspr = new SchemaQualifiedObjectName() { Name = "file_raspr", Schema = CurrentSchema };

            if (Database.TableExists(file_raspr))
            {
                if (!Database.ColumnExists(file_raspr, "nzp_pack_ls_real"))
                {
                    Database.AddColumn(file_raspr, new Column("nzp_pack_ls_real", DbType.Int32));
                }

                if (!Database.ColumnExists(file_raspr, "nzp_pack"))
                {
                    Database.AddColumn(file_raspr, new Column("nzp_pack", DbType.Int32));
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
