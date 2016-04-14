using System;
using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030602301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015030602301CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);
            //1456|Период отключения расчета пени|||bool||1||||
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 1456"));

            if (count == 0)
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                new[] { "1456", "Период отключения расчета пени", "bool", "1" });
            }


        }

        public override void Revert()
        {
        }
    }
}


