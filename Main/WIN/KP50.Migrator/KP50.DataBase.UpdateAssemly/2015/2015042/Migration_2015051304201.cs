using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015051304201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015051304201_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };


            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 1047;");
            var count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(prm_name, new string[]
                {
                    "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"
                }, new string[]
                {
                    "1047", "ФИО руководителя ПУС", "char", null, "10", null, null, null
                });
            }

            obj = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 1048;");
            count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(prm_name, new string[]
                {
                    "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_"
                }, new string[]
                {
                    "1048", "Должность руководителя ПУС", "char", null, "10", null, null, null
                });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
