using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015034
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015040703401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015040703401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var parameters = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };
            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT COUNT(1) FROM " + parameters.Schema + Database.TableDelimiter +
                                           parameters.Name + " WHERE nzp_prm = 1984"));
            if (count == 0)
            {

                Database.Insert(parameters, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet" },
                    new string[] { "1984", "Услуга для ЖКУ (ГВС)", "serv", "5", "0" });
            }

            SetSchema(Bank.Data);

            var prm_5 = new SchemaQualifiedObjectName { Name = "prm_5", Schema = CurrentSchema }; count =
                 Convert.ToInt32(
                     Database.ExecuteScalar("SELECT COUNT(1) FROM " + prm_5.Schema + Database.TableDelimiter +
                                            prm_5.Name + " WHERE nzp_prm = 1984 AND is_actual <> 100"));
            if (count == 0)
            {
                Database.Insert(prm_5, new string[] { "nzp", "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual" },
                    new string[] { "0", "1984", "1900-01-01", "3000-01-01", "9", "1" });
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
