using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015051804202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015051804202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm IN (2102, 2103, 2104, 2105)");
                Database.Insert(prm_name,
                    new string[] {"nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"},
                    new string[] { "2102", "ОДН ПОЛИВ-Тип распределения", "sprav", "3119", "2", null, null, null });

                Database.Insert(prm_name,
                   new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                   new string[] { "2103", "ОДН ПОЛИВ-НЕ начислять по Пост.307 если К<1", "bool", null,"2", null, null, null });

                Database.Insert(prm_name,
                   new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                   new string[] { "2104", "ОДН ПОЛИВ-Вид распределения", "sprav", "3003", "2", null, null, null });

                Database.Insert(prm_name,
                   new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                   new string[] { "2105", "ОДН ПОЛИВ-Показатель распределения", "sprav", "3004", "2", null, null, null });
            }

            var resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            if (Database.TableExists(resolution))
            {
                Database.Delete(resolution, "nzp_res = 3119");
                Database.Insert(resolution,
                    new string[] {"nzp_res", "name_short", "name_res", "is_readonly"},
                    new string[] {"3119", "ТТипОДНПолив", "Тип распределения ОДН для Полив", "1"});
            }
            var res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, "nzp_res in (3119) and nzp_y in (1)");

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3119", "1", "Формула 6 Постановления №307" });
            }

            var res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            if (Database.TableExists(res_x))
            {
                Database.Delete(res_x, "nzp_res=3119");
                Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3119", "1", "Тип распределения ОДН" });
            }

            var res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };
            if (Database.TableExists(res_values))
            {
                Database.Delete(res_values, "nzp_res=3022");
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3119", "1", "1", "0" });
            }

            var s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            if (Database.TableExists(s_reg_prm))
            {
                Database.Delete(s_reg_prm, "nzp_reg=3 and nzp_serv = 200 and nzp_prm in (2102,2103,2104,2105)");
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2102", "200", "34", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2103", "200", "35", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2104", "200", "36", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2105", "200", "37", "1" });
            }
        }

        public override void Revert()
        {

        }
    }
}
