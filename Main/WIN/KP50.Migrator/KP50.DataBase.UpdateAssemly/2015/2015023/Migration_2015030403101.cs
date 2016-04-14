using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030403101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015030403101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(resolution, "nzp_res = 3018");
            Database.Delete(res_y, "nzp_res = 3018");
            Database.Delete(res_x, "nzp_res = 3018");
            Database.Delete(res_values, "nzp_res = 3018");

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3018", "ТТипОтоплЛС", "таблица Тип отопления для ЛС" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3018", "1", "Центральное" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3018", "2", "Автономная котельная" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3018", "3", "Печное" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3018", "4", "Квартирный котел" }); 
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3018", "1", "Номер" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3018", "1", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3018", "2", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3018", "3", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3018", "4", "1", " " });


            Database.Delete(resolution, "nzp_res = 3028");
            Database.Delete(res_y, "nzp_res = 3028");
            Database.Delete(res_x, "nzp_res = 3028");
            Database.Delete(res_values, "nzp_res = 3028");

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3028", "ТТипГвсЛС", "таблица Тип ГВС для ЛС" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3028", "1", "Центральное" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3028", "2", "Автономная котельная" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3028", "3", "Печное" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3028", "4", "Квартирный газовый нагреватель" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3028", "5", "Квартирный малолитражный котел" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3028", "1", "Номер" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3028", "1", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3028", "2", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3028", "3", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3028", "4", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3028", "5", "1", " " });

            Database.Delete(resolution, "nzp_res = 3019");
            Database.Delete(res_y, "nzp_res = 3019");
            Database.Delete(res_x, "nzp_res = 3019");
            Database.Delete(res_values, "nzp_res = 3019");

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3019", "ТТипДома", "Тип дома" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3019", "1", "МКД" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3019", "2", "Частный" });

            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3019", "1", "Номер" });

            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3019", "1", "1", " " });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3019", "2", "1", " " });

            Database.Delete(prm_name, "nzp_prm in (1240,1244,2030)");

            Database.Insert(prm_name,
                new string[] {"nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"},
                new string[] {"1240", "Тип отопления", "sprav", "3018", "1", null, null, null});
            Database.Insert(prm_name,
                new string[] {"nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"},
                new string[] {"1244", "Тип ГВС", "sprav", "3028", "1", null, null, null});
            Database.Insert(prm_name,
                new string[] {"nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"},
                new string[] { "2030", "Тип дома", "sprav", "3019", "2", null, null, null });
            //
        }
    }
}
