using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003307, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015032003307_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            // Tables:
            var parameters = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };

            var count = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(1) FROM " + parameters.Schema + Database.TableDelimiter + parameters.Name + " WHERE nzp_prm = 2200"));
            if (count == 0)
            {
                Database.Insert(parameters, new
                    {
                        nzp_prm = 2200,
                        name_prm = "Дата окончания учёта жёстких расходов по ЛС",
                        type_prm = "date",
                        prm_num = 10
                    });
            }
        }

        public override void Revert()
        {

        }
    }
}
