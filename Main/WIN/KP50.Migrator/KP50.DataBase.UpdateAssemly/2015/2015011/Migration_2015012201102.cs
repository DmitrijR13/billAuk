using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015012201102, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012201102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm=1214"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1214", "Пост.344 ХВ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2" },
                    " nzp_prm=1214");
            }
            else
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1214", "Пост.344 ХВ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2", null, null, null });
            }

            count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm=1215"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1215", "Пост.344 ГВ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2" },
                    " nzp_prm=1215");
            }
            else
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1215", "Пост.344 ГВ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2", null, null, null });
            }

            count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm=1216"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1216", "Пост.344 ЭЭ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2" },
                    " nzp_prm=1216");
            }
            else
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1216", "Пост.344 ЭЭ -Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2", null, null, null });
            }

            count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm=1217"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1217", "Пост.344 ГАЗ-Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2" },
                    " nzp_prm=1217");
            }
            else
            {
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1217", "Пост.344 ГАЗ-Разрешить превышение расхода ОДН над нормативом на ОДН", "bool", "2", null, null, null });
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

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
