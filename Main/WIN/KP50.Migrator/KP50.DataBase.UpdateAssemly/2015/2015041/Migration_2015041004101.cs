using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015041
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015041004101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015041004101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                if (!Database.ColumnExists(prm_name, "is_day_uchet_enable"))
                {
                    Database.AddColumn(prm_name, new Column("is_day_uchet_enable", new ColumnType(DbType.Int32), ColumnProperty.None, 0));
                }
                Database.Update(prm_name, new string[] { "is_day_uchet_enable" }, new string[] { "1" },
                    " nzp_prm in (4,6,133,5,131)");
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

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
