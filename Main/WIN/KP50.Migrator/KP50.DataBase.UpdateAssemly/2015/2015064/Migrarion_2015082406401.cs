using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015082406401, Migrator.Framework.DataBase.CentralBank)]
    public class Migrarion_2015082406401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var sPayer = new SchemaQualifiedObjectName() {Name = "s_payer", Schema = CurrentSchema};
            if (!Database.TableExists(sPayer)) return;
            if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + sPayer.Name + " where nzp_payer = 79997")) == 0)
            {
                Database.Insert(sPayer, new string[] {"nzp_payer", "payer", "npayer"},
                    new string[] {"79997", "Диспетчерская служба", "Диспетчерская служба"});
            }
        }
    }
}

