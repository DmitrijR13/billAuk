using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015021602102, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015021602102 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(resolution, "nzp_res = 3009");
            Database.Delete(res_y, "nzp_res = 3009");
            Database.Delete(res_x, "nzp_res = 3009");
            Database.Delete(res_values, "nzp_res = 3009");

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3009", "ТВариантОДН", "Вариант расчета ОДН" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3009", "1", "Стандарт" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3009", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3009", "1", "1", "0" });

            Database.Delete(prm_name, "nzp_prm in (1991,1994)");

            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1991", "ОДН- Запрещен перерасчет", "bool", null, "5", null, null, null });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1994", "ОДН- Вариант расчета", "sprav", "3009", "5", null, null, null });
            //
        }
    }
}
