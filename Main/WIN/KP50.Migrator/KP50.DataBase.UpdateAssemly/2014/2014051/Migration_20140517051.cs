using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140517051, MigrateDataBase.LocalBank)]
    public class Migration_20140517051_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel
            
            SetSchema(Bank.Data);
            // TODO: Upgrade LocalPref_Data
            SchemaQualifiedObjectName prm_3 = new SchemaQualifiedObjectName() { Name = "prm_3", Schema = CurrentSchema };
            if (Database.ColumnExists(prm_3, "val_prm"))
            {
                Database.ChangeColumn(prm_3, "val_prm", DbType.String.WithSize(150), false);
            }

            SchemaQualifiedObjectName prm_9 = new SchemaQualifiedObjectName() { Name = "prm_9", Schema = CurrentSchema };
            if (Database.ColumnExists(prm_9, "val_prm"))
            {
                Database.ChangeColumn(prm_9, "val_prm", DbType.String.WithSize(255), false);
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }
}
