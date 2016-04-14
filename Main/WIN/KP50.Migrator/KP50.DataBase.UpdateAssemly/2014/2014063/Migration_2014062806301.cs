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
    [Migration(2014062806301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014062806301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName()
            {
                Name = "prm_name",
                Schema = CurrentSchema
            };
            if (!Database.TableExists(prm_name)) return;

            Database.Delete(prm_name, "nzp_prm IN (2081,2082,2083,2084)");

            Database.Insert(prm_name,
                new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
                new[]
                {
                    "2081", "Расход ИПУ по услуге Электроснабжение не должен превышать, Квт", "'float'", "10", "0",
                    "1000000", "7"
                });
            Database.Insert(prm_name,
                new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
                new[]
                {
                    "2082", "Расход ИПУ по услуге по ГВС не должен превышать, м3", "'float'", "10", "0", "1000000",
                    "7"
                });
            Database.Insert(prm_name,
                new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
                new[]
                {
                    "2083", "Расход ИПУ по услуге ХВС не должен превышать, м3", "'float'", "10", "0", "1000000",
                    "7"
                });
            Database.Insert(prm_name,
                new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
                new[]
                {
                    "2084", "Расход ИПУ по услуге по услуге Газ не должен превышать, м3", "'float'", "10", "0",
                    "1000000", "7"
                });
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

        }
    }
}
