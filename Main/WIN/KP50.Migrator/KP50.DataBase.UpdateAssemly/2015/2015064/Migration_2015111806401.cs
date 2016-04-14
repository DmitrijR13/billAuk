using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015111806401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015111806401:Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName tula_file_reestr= new SchemaQualifiedObjectName{Name = "tula_file_reestr", Schema = CurrentSchema};
            if (!Database.TableExists(tula_file_reestr)) return;
            if (Database.IndexExists("tula_file_reestr_pkod_transaction_id_idx", tula_file_reestr)) return;
            Database.AddIndex("tula_file_reestr_pkod_transaction_id_idx", false,tula_file_reestr, "pkod", "transaction_id");
        }
    }
}
