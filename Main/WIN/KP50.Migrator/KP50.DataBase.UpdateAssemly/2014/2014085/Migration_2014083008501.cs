using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014085
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014083008501, MigrateDataBase.CentralBank)]
    public class Migration_2014083008501_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };

            if (Database.TableExists(s_payer))
            {
                if (Database.ColumnExists(s_payer, "npayer"))
                {
                    Database.ChangeColumn(s_payer, "npayer", DbType.String.WithSize(100), true);
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
