using System;
using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015050804202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015050804202_CentralBank : Migration
    {
        public override void Apply()
        {
        
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName();
            prm_name.Schema = CurrentSchema;
            prm_name.Name = "prm_name";
            //1456|Период отключения расчета пени|||bool||1||||
            var count = Convert.ToInt32(
                          Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                                 prm_name.Name + " WHERE nzp_prm = 1456"));

            if (count == 0)
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                new[] { "1456", "Период отключения расчета пени", "bool", "1", "1", "1" });
            }
            else
            {
                Database.Update(
                      prm_name,
                      new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                      new[] { "1456", "Период отключения расчета пени", "bool", "1", "1", "1" }, "nzp_prm=1456");
            }


        }

        public override void Revert()
        {
        }
    }
}


