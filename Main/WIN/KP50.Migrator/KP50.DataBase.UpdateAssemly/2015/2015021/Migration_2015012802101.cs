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
    [Migration(2015012802101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012802101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel); SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 1046"));

            if (count == 0)
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1046", "Предельная сумма начисления/перерасчета для анализа и проверок", "float", "5", "0", "1000000", "7" });
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_5 = new SchemaQualifiedObjectName() { Name = "prm_5", Schema = CurrentSchema };

            count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_5.Schema + Database.TableDelimiter +
                                           prm_5.Name + " WHERE is_actual <> 100 AND nzp_prm = 1046"));

            if (count == 0)
            {
                Database.Insert(prm_5,
                new[] { "nzp_prm", "nzp", "dat_s", "dat_po", "val_prm", "is_actual" },
                new[] { "1046", "0", "01.01.2014", "01.01.3000", "1000000", "1"});
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
