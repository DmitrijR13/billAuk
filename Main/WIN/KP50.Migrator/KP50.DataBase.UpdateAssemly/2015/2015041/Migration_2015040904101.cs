using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015040904101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015040904101_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;
            SetSchema(Bank.Kernel);

            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для бани" }, "nzp_prm = 441");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для газа" }, "nzp_prm = 137");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для гор.воды" }, "nzp_prm = 136");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для канализации" }, "nzp_prm = 139");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС(литров) для питьевой воды" }, "nzp_prm = 706");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для ночное эл/эн" }, "nzp_prm = 380");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для Освещение МОП" }, "nzp_prm = 226");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для отопления" }, "nzp_prm = 138");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для Эл/Эн лифтов (квт/час)" }, "nzp_prm = 388");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "ООбъем потребления ЛС для полива" }, "nzp_prm = 412");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для хол.воды" }, "nzp_prm = 135");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для ХП эл/эн" }, "nzp_prm = 472");
                Database.Update(prm_name, new string[] { "name_prm" }, new string[] { "Объем потребления ЛС для электроэнергии" }, "nzp_prm = 22");
            }

        }

        public override void Revert()
        {
        }
    }


}
