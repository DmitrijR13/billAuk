using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015055
{
    [Migration(2015061905501, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015061905501 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var sPayer = new SchemaQualifiedObjectName() {Name = "s_payer", Schema = CurrentSchema};
            if (Database.TableExists(sPayer))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + sPayer.Name + " where nzp_payer = 79997")) == 0)
                {
                    Database.Insert(sPayer, new string[] { "nzp_payer", "payer", "npayer" },
                    new string[] { "79997", "Диспетчерская служба", "Диспетчерская служба" });
                }
            }
        }
    }
}
