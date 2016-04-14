using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015023
{

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015030202302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015030202302_Kernel : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Update(prm_name, new[] { "name_prm", "is_day_uchet" }, new[] { "Второе жилье для ЛС - Количество не зарегистрированных проживающих", "1" }, "nzp_prm=1395");
                Database.Update(prm_name, new[] { "name_prm" }, new[] { "Второе жилье для ЛС - Не зарегистрированные граждане являются потребителями ЖКУ" }, "nzp_prm=1396");
                Database.Update(prm_name, new[] { "name_prm" }, new[] { "Резерв - не использовать" }, "nzp_prm=1374");
            }

            SetSchema(Bank.Data);
            var prm_1 = new SchemaQualifiedObjectName { Name = "prm_1", Schema = CurrentSchema };
            if (Database.TableExists(prm_1))
            {
                Database.Update(prm_1, new[] { "is_actual" }, new[] { "100" }, "nzp_prm=1374 and is_actual<>100");
            }
        }

        public override void Revert()
        {
            // TODO: Downgrade Web

        }
    }
}
