using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111811202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014111811202_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName()
            {
                Name = "prm_name",
                Schema = CurrentSchema
            };

            if (Database.TableExists(prm_name))
            {
                //1395|Количество незарегистврированных проживающих|float|1|0|1000000|7|
                int count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_prm) FROM {0:NAME} WHERE nzp_prm = 1395 ", prm_name)));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                        new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                        new string[] { "1395", "Количество не зарегистрированных проживающих", "float", "1", "0", "1000000", "7" });
                }
                //1396|Не зарегистрированные граждане являются потребителями ЖКУ|||bool||11||||
                count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_prm) FROM {0:NAME} WHERE nzp_prm = 1396 ", prm_name)));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                        new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "1396", "Не зарегистрированные граждане являются потребителями ЖКУ", "bool", "11" });
                }
            }
        }
    }
}
