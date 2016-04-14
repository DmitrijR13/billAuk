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
    [Migration(2015013002101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015013002101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName sobstw = new SchemaQualifiedObjectName() { Name = "sobstw", Schema = CurrentSchema };
            if (Database.TableExists(sobstw))
            {
                if (!Database.ColumnExists(sobstw, "tel"))
                {
                    Database.AddColumn(sobstw, new Column("tel", DbType.String.WithSize(20)));
                }
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
