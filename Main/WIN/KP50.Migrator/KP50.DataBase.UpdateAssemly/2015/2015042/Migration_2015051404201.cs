using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015051404201, Migrator.Framework.DataBase.Web)]
    public class Migration_2015051404201 : Migration
    {
        public override void Apply()
        {
            var source_pack_ls = new SchemaQualifiedObjectName() { Name = "source_pack_ls", Schema = CurrentSchema };
            if (!Database.TableExists(source_pack_ls)) return;
            if (Database.ColumnExists(source_pack_ls, "user_ls")) return;
            Database.AddColumn(source_pack_ls, new Column("user_ls", DbType.StringFixedLength.WithSize(100)));
        }
    }
}
