using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015032
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015031103201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015031103201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 1457"));

            if (count == 0)
            {
                Database.Insert(prm_name,
                    new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
                    new[] { "1457", "Расход ПУ по умолчанию для всех услуг не должен превышать", "float", "10", "1", "1000000","7"});


                SetSchema(Bank.Data);
                SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName()
                {
                    Name = "prm_10",
                    Schema = CurrentSchema
                };

                count =
                    Convert.ToInt32(
                        Database.ExecuteScalar("SELECT count(*) FROM " + prm_10.Schema + Database.TableDelimiter +
                                               prm_10.Name + " WHERE is_actual <> 100 AND nzp_prm = 1457"));

                if (count == 0)
                {
                    Database.Insert(prm_10,
                        new[] {"nzp_prm", "nzp", "dat_s", "dat_po", "val_prm", "is_actual"},
                        new[] {"1457", "0", "01.01.2014", "01.01.3000", "300000", "1"});
                }
            }
        }

        public override void Revert()
        {
        }
    }
    
}
