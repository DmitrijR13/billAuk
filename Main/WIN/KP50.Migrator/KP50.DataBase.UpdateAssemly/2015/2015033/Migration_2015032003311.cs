using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003311, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015032003311_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            // Tables:
            var parameters = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };
            var count = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(1) FROM " + parameters.Schema + Database.TableDelimiter + parameters.Name + " WHERE nzp_prm = 2202"));
            if (count == 0)
            {
                Database.Insert(parameters, new
                    {
                        nzp_prm = 2202,
                        name_prm = "Учитывать в расчёте для временно прибывших дату окончания временной регистрации",
                        type_prm = "bool",
                        prm_num = 5
                    });
            }
        }

        public override void Revert()
        {

        }
    }
}
