using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014073
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014072107302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014072107302_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (Database.TableExists(kvar))
            {
                Database.ChangeColumn(kvar, "nkvar_n", DbType.String.WithSize(10), false);
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data
        }
    }
}
