using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015021602101, MigrateDataBase.LocalBank)]
    public class Migration_2015021602101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
         

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kart = new SchemaQualifiedObjectName() { Name = "kart", Schema = CurrentSchema };
            if (Database.TableExists(kart))
            {
                Database.ChangeColumn(kart, "rem_mr", DbType.String.WithSize(80), false);
                Database.ChangeColumn(kart, "rem_op", DbType.String.WithSize(80), false);
                Database.ChangeColumn(kart, "rem_ku", DbType.String.WithSize(80), false);
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
