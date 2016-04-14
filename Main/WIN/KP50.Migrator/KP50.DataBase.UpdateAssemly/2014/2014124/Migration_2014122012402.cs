using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014122012402, MigrateDataBase.CentralBank)]
    public class Migration_2014122012402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName checkchmon = new SchemaQualifiedObjectName() { Name = "checkchmon", Schema = CurrentSchema };
            if (Database.TableExists(checkchmon))
            {
                if (Database.ColumnExists(checkchmon, "dat_check"))
                    Database.ChangeColumn(checkchmon, "dat_check", new ColumnType(DbType.DateTime), false);
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
