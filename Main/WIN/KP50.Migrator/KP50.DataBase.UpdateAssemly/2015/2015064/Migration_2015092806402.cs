using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015092806402, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015092806402:Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var simple_load = new SchemaQualifiedObjectName{Name="simple_load", Schema=CurrentSchema};
            if (!Database.TableExists(simple_load)) return;
            if (Database.ColumnExists(simple_load, "is_actual")) return;
            Database.AddColumn(simple_load, new Column("is_actual", DbType.Int32, ColumnProperty.None, 1));
        }
    }

    [Migration(2015092806402, Migrator.Framework.DataBase.Web)]
    public class Migration_2015092806402_Web : Migration
    {
        public override void Apply()
        {
            var source_pack = new SchemaQualifiedObjectName { Name = "source_pack", Schema = CurrentSchema };
            if (!Database.TableExists(source_pack)) return;
            if (Database.ColumnExists(source_pack, "is_actual")) return;
            Database.AddColumn(source_pack, new Column("is_actual", DbType.Int32, ColumnProperty.None, 1));
        }
    }
}
