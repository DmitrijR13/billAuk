using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014122612402, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014122612402_CentralBank : Migration
    {


        public override void Apply()
        {

            #region Добавление параметров
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName();
            prm_name.Schema = CurrentSchema;
            prm_name.Name = "prm_name";

            //добавление в список параметров
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm in (1556,1557,1558,1559)");
                Database.Insert(
                    prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "1556", "Повышающий коэффициент по услуге отопления", "float", "2", "0", "1000000", "7" });
                Database.Insert(
                   prm_name,
                   new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                   new[] { "1557", "Повышающий коэффициент по услуге хол.вода", "float", "2", "0", "1000000", "7" });
                Database.Insert(
                   prm_name,
                   new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                   new[] { "1558", "Повышающий коэффициент по услуге гор.вода", "float", "2", "0", "1000000", "7" });
                Database.Insert(
                   prm_name,
                   new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                   new[] { "1559", "Повышающий коэффициент по услуге электроснабжение", "float", "2", "0", "1000000", "7" });
            }

            #endregion

        }

    }
}