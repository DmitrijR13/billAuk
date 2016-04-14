using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014081808402, Migrator.Framework.DataBase.Charge)]
    public class Migration_2014081708402_Charge:Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName del_supplier = new SchemaQualifiedObjectName() { Name = "del_supplier", Schema = CurrentSchema };
             SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CentralKernel };
            if (Database.TableExists(del_supplier))
            {
                if (!Database.ColumnExists(del_supplier, "type_rcl")) Database.AddColumn(del_supplier, new Column("type_rcl", DbType.Int32));

                if (Database.ProviderName == "PostgreSQL")
                {
                    if (!Database.ConstraintExists(del_supplier, "fk_s_typercl_type_rcl"))
                    Database.AddForeignKey("fk_s_typercl_type_rcl", del_supplier, "type_rcl", s_typercl, "type_rcl");
                }
            }
        }
    }
}
