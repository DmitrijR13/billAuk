using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015040603401, Migrator.Framework.DataBase.Web)]
    public class Migration_2015040603401 : Migration
    {
        public override void Apply()
        {
            var anl2014_dom = new SchemaQualifiedObjectName() { Name = "anl2014_dom", Schema = CurrentSchema };
            if (!Database.TableExists(anl2014_dom)) return;
            if (!Database.ColumnExists(anl2014_dom, "ndom")) return;
            Database.ChangeColumn(anl2014_dom, "ndom", new ColumnType(DbType.StringFixedLength, 1000), false);
        }
    }
}
