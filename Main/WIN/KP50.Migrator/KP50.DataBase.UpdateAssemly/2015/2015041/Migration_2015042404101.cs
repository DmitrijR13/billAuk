using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015042404101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015042404101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName()
            {
                Name = "res_y",
                Schema = CurrentSchema
            };
            //
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 11 Постановления №354 (с учетом Формулы 15 Постановления №354)" }, 
                " nzp_res=3002 and nzp_y=3");
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 11 (-ГВ) Постановления №354 (с учетом Формулы 15 Постановления №354)" }, 
                " nzp_res=3002 and nzp_y=4");
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 11 Пост.№354 и не больше нормы для ИПУ (с учетом Формулы 15 Постановления №354)" }, 
                " nzp_res=3002 and nzp_y=5");

            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 12 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3006 and nzp_y=3");
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 20 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3006 and nzp_y=4");

            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 12 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3007 and nzp_y=3");
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 12 Постановления №354 2х-тарифный (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3007 and nzp_y=4");

            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 13 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3008 and nzp_y=3");
            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 18 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3008 and nzp_y=4");

            Database.Update(res_y, new[] { "name_y" }, new[] { "Формула 12 Постановления №354 (с учетом Формулы 15 Постановления №354)" },
                " nzp_res=3022 and nzp_y=3");
        }
    }
}
