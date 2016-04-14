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
    [Migration(2014122312403, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014122312403_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data 
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            if (Database.TableExists(prm_10))
            {
                Database.Update(prm_10, new string[] {"nzp"}, new string[] {"0"}, " nzp is null");
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
