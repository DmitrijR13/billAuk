using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014123
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014121512301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014121512301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
                Database.Update(s_group, new string[] { "ngroup" }, new string[] { "П-изм.пар-ров ЛС после расчета" }, " nzp_group = 12");

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
