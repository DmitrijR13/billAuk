using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014120512103, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014120512103_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, " nzp_res = 3002 and nzp_y in (4,5) ");
                Database.Insert(res_y, new[] { "nzp_res", "nzp_y", "name_y" }, new[] { "3002", "4", "Формула 11 (-ГВ) Постановления №354" });
                Database.Insert(res_y, new[] { "nzp_res", "nzp_y", "name_y" }, new[] { "3002", "5", "Формула 11 Пост.№354 и не больше нормы для ИПУ" });
            }

            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };
            if (Database.TableExists(res_values))
            {
                Database.Delete(res_values, " nzp_res = 3002 and nzp_y in (4,5) ");
                Database.Insert(res_values, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3002", "4", "1", "0" });
                Database.Insert(res_values, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3002", "5", "1", "0" });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

        }
    }

}
