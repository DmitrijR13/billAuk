using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014111
{
    [Migration(2014111011101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014111011101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                int count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_prm) FROM {0:NAME} WHERE nzp_prm = 1377 ", prm_name)));
                if (count == 0)
                {
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "1377", "Не отражать временно-прибывших в ЕПД", "bool", "1" });
                }
                else
                {
                    Database.Update(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "1377", "Не отражать временно-прибывших в ЕПД", "bool", "1" }, "nzp_prm = 1377");
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

        }
    }
}
