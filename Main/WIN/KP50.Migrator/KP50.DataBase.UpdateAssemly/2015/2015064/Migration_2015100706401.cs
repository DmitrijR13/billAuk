using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(2015100706401, Migrator.Framework.DataBase.LocalBank | Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015100706401:Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Kernel);
             SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName {Name = "supplier", Schema = CurrentSchema};
             if (!Database.TableExists(supplier))
             {
                 return;
             }
             if (Database.ColumnExists(supplier, "nzp_payer_podr"))
             {
                 return;
             }
             Database.AddColumn(supplier, new Column("nzp_payer_podr", DbType.Int32));
         }
    }
}
