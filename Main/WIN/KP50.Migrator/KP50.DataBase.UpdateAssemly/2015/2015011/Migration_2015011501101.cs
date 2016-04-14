using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015011501101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015011501101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };
            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            SchemaQualifiedObjectName prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm = 2463");
            Database.Delete(prm_name, "nzp_prm = 253");
            Database.Delete(prm_name, "nzp_prm = 1397");

            Database.Delete(prm_tarifs, "nzp_frm in (1140,40) and nzp_prm=253");
            Database.Delete(prm_frm, "nzp_frm in (1140,40) and nzp_prm=253");

            Database.Delete(resolution, "nzp_res = 3025");
            Database.Delete(res_y, "nzp_res = 3025");
            Database.Delete(res_x, "nzp_res = 3025");
            Database.Delete(res_values, "nzp_res = 3025");

            Database.Insert(prm_frm,
                new string[] { "nzp_frm", "frm_calc", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                new string[] { "40", "999", "1", "  FLD", "253", " ", " ", " ", " " });

            Database.Insert(prm_tarifs,
                new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" },
                new string[] { "9", "40", "253", "1", "-1000" });

            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2463", "Квартирный норматив на 1 ГКал/кв.м для отопления", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "253", "Норматив на 1 ГКал/куб.м горячей воды", "float", null, "5", "0", "1000", "4" });
            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1397", "Тип алгоритма расчета для ПУ от ГКал", "sprav", "3025", "2", null, null, null });

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3025", "ТТипАлгГКал", "Алгоритмы расчета для ПУ от ГКал" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "1", "Расчет ГВС по ГКал" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "2", "Расчет ГВС по ГКал(по ДПУ)" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "3", "Расчет Отопления и ГВС по ГКал(ГВС по нормативу)" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "4", "Расчет Отопления и ГВС по ГКал(пропорция по нормативам)" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3025", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "1", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "2", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "3", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "3", "1", "0" });

        }
    }
}
