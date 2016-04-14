using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014082708501, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014082708501_Kernel : Migration
    {
        public override void Apply()
        {
            //SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Update(prm_name, new string[] { "nzp_res" }, new string[] { "3022" }, "nzp_prm=2062");
            }

            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            if (Database.TableExists(resolution))
            {
                Database.Delete(resolution, "nzp_res=3022");
                Database.Insert(resolution, new string[] { "nzp_res", "name_short", "name_res" }, new string[] { "3022", "ТТипОДНГАЗ", "Тип распределения ОДН для Газа" });
            }
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Update(res_y, new string[] { "name_y" }, new string[] { "Формула 12 Постановления №354" }, "nzp_res=3006 and nzp_y=3");
                Database.Update(res_y, new string[] { "name_y" }, new string[] { "Формула 12 Постановления №354" }, "nzp_res=3007 and nzp_y=3");
                Database.Update(res_y, new string[] { "name_y" }, new string[] { "Формула 12 Постановления №354 2х-тарифный" }, "nzp_res=3007 and nzp_y=4");
                Database.Update(res_y, new string[] { "name_y" }, new string[] { "Формула 13 Постановления №354" }, "nzp_res=3008 and nzp_y=3");
                //
                Database.Delete(res_y, "nzp_res=3022");
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3022", "1", "Формула 9 Постановления №307" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3022", "2", "Формула 6 Постановления №307" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3022", "3", "Формула 12 Постановления №354" });
            }
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            if (Database.TableExists(res_x))
            {
                Database.Delete(res_x, "nzp_res=3022");
                Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3022", "1", "Тип распределения ОДН" });
            }
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };
            if (Database.TableExists(res_values))
            {
                Database.Delete(res_values, "nzp_res=3022");
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3022", "1", "1", "0" });
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3022", "2", "1", "0" });
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3022", "3", "1", "0" });
            }
            SchemaQualifiedObjectName s_type_alg = new SchemaQualifiedObjectName() { Name = "s_type_alg", Schema = CurrentSchema };
            if (Database.TableExists(s_type_alg))
            {
                Database.Delete(s_type_alg, "nzp_type_alg=23");
                Database.Insert(s_type_alg, new string[] { "nzp_type_alg", "name_type", "name_small", "name_short" },
                    new string[] { "23", "Расчет по Постановлению №354-формула 13 (отопление) пропорционально площади лицевых счетов.", "Пост.№354-формула 13 (отопление)", "П354ф13ПлЛСОт" });
            }
        }
        public override void Revert()
        {
            //
        }
    }
}