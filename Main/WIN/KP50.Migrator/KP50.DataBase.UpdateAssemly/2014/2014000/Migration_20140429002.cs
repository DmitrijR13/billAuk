using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(20140425044, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_20140429002_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName prm_3 = new SchemaQualifiedObjectName() { Name = "prm_3", Schema = CurrentSchema };

            if (Database.TableExists(prm_3))
            {
                // Columns:
                if (Database.ColumnExists(prm_3, "val_prm"))
                {
                    Database.ChangeColumn(prm_3, "val_prm", DbType.String.WithSize(40), true);
                }

            }
        }
    }
}
