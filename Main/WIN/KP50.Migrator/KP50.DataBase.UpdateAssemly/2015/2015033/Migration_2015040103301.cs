using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015040103301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015040103301_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, "nzp_res = 9998 and nzp_y = 5");
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "9998", "5", "Норматив - стандартный вид" });
            }
        }

        public override void Revert()
        {

        }
    }
}
