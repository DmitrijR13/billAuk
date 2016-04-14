using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014083
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080808302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014080808302_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_3 = new SchemaQualifiedObjectName() { Name = "prm_3", Schema = CurrentSchema };
            if (Database.TableExists(prm_3))
            {
                Database.ChangeColumn(prm_3, "val_prm", DbType.String.WithSize(128), false);
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);

        }
    }
}
