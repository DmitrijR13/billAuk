using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305015, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_20140305015_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            Database.Update(prm_name, new string[] { "old_field" }, new string[] { "0" });
            Database.Update(prm_name, new string[] { "old_field" }, new string[] { "1" }, "nzp_prm = 974");
        }
    }
}
