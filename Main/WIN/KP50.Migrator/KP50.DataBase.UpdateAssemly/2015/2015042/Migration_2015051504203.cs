using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015051504203, Migrator.Framework.DataBase.Fin)]
    public class Migration_2015051504203 : Migration
    {
        public override void Apply()
        {
            var pack_ls = new SchemaQualifiedObjectName { Schema = CurrentSchema, Name = "pack_ls" };
            if (!Database.TableExists(pack_ls)) return;
            if (Database.ColumnExists(pack_ls, "old_num_ls")) return;
            Database.AddColumn(pack_ls, new Column("old_num_ls", new ColumnType(DbType.StringFixedLength, 100)));
        }
    }
}
