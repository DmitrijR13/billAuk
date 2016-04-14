using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2014._2014082
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080508201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014080508201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters_arx = new SchemaQualifiedObjectName() { Name = "counters_arx", Schema = CurrentSchema };
            if (Database.TableExists(counters_arx))
            {
                Database.ChangeColumn(counters_arx, "val_old", DbType.String.WithSize(60), false);
                Database.ChangeColumn(counters_arx, "val_new", DbType.String.WithSize(60), false);
            }
        }
    }
}
