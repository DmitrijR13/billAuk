using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015101206401, Migrator.Framework.DataBase.LocalBank | Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015101206401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName { Name = "supplier", Schema = CurrentSchema };
            if (!Database.TableExists(supplier))
            {
                return;
            }
            if (Database.ColumnExists(supplier, "fn_dogovor_bank_lnk_id"))
            {
                return;
            }
            Database.AddColumn(supplier, new Column("fn_dogovor_bank_lnk_id", DbType.Int32));
        }
    }
}
