using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015022502201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015022502201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 1448"));

            if (count == 0)
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1448", "Количество месяцев учета среднего после выхода из строя ПУ", "int", "10", "1", "100", "3" });
            }
        }

        public override void Revert()
        {
        }
    }

}
