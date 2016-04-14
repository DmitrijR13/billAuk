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
    [Migration(2015062405501, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015062405501_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var kvar_arx = new SchemaQualifiedObjectName() { Name = "kvar_arx", Schema = CurrentSchema };
            if (!Database.TableExists(kvar_arx)) return;
            if (Database.ColumnExists(kvar_arx, "pkod"))
                Database.ChangeColumn(kvar_arx, "pkod", DbType.Decimal.WithSize(13, 0), true);
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

        }
    }

}
