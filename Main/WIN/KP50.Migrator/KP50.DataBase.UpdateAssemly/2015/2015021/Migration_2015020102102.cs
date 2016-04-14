using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015020102102, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015020102102 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm = 2095");

            Database.Delete(resolution, "nzp_res = 3025");
            Database.Delete(res_y, "nzp_res = 3025");
            Database.Delete(res_x, "nzp_res = 3025");
            Database.Delete(res_values, "nzp_res = 3025");

            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2095", "ОДН-Учет отрицательного расхода в услуге на ОДН", "bool", null, "2", null, null, null });

            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3025", "ТТипАлгГКал", "Алгоритмы расчета для ПУ от ГКал" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "1", "Расчет ГВС по ГКал" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "2", "Расчет ГВС по ГКал(по ДПУ)" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "3", "Расчет Отопления и ГВС по ГКал(ГВС по нормативу)" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "4", "Расчет Отопления и ГВС по ГКал(пропорция по нормативам)" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3025", "5", "Расчет ОДН ГВС по ГКал - Пост354 (расход ЛС по норме в ГКал)" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3025", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "1", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "2", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "3", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "4", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3025", "5", "1", "0" });

            // г..но для сахи ...
            Database.Delete(prm_name, "nzp_prm in (2096,2097,2098,2099,2100,2101)");

            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2096", "ЭОТ-отопление(справочно)           ", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2097", "ЭОТ-х/водоснабжение(справочно)     ", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2098", "ЭОТ-г/водоснабжение(справочно)     ", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2099", "ЭОТ-водоотведение(справочно)       ", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2100", "ЭОТ-теплоноситель(справочно)       ", "float", null, "1", "0", "1000000", "7" });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2101", "ЭОТ-теплоэнергия для ГВС(справочно)", "float", null, "1", "0", "1000000", "7" });
            //
        }
    }
}
