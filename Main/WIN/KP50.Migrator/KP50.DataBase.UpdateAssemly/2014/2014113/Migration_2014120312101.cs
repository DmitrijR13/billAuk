using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014113
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014120312101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014120312101_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, " nzp_res = 9990 and nzp_y in (5,6) ");
                Database.Insert(res_y, new[] { "nzp_res", "nzp_y", "name_y" }, new[] { "9990", "5", "санузел" });
                Database.Insert(res_y, new[] { "nzp_res", "nzp_y", "name_y" }, new[] { "9990", "6", "кладовка" });
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_17 = new SchemaQualifiedObjectName() { Name = "prm_17", Schema = CurrentSchema };
            if (Database.TableExists(prm_17))
            {
                if (Database.ColumnExists(prm_17, "val_prm"))
                {
                    Database.ChangeColumn(prm_17, "val_prm", DbType.String.WithSize(40), true);
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

        }
    }

}
