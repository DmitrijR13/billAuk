using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014062706301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014062706301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            Database.Update(prm_name, new string[] { "is_day_uchet" }, new string[] { "1" }, "nzp_prm in (51, 4, 5, 6, 131)");
        }
    }
}
